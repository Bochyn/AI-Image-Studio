using System.Text.Json;
using Microsoft.Extensions.Logging;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Enums;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services.Jobs;

public sealed class GenerateJobHandler : IJobHandler
{
    private readonly ILogger<GenerateJobHandler> _logger;

    public GenerateJobHandler(ILogger<GenerateJobHandler> logger)
    {
        _logger = logger;
    }

    public JobType JobType => JobType.Generate;

    public async Task<Guid> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var request = JobRequestJson.Deserialize<GenerateRequest>(context.Job.RequestJson);
        var parametersJson = JsonSerializer.Serialize(new
        {
            aspectRatio = request.AspectRatio,
            resolution = request.Resolution,
            numImages = request.NumImages,
            outputFormat = request.OutputFormat,
            quality = request.Quality,
            inputFidelity = request.InputFidelity
        });

        context.Progress.Report(context.Job, 10, "Preparing input...");

        var sourceImageData = await SourceImageLoader.LoadSourceImageAsync(
            request, context.Db, context.Storage, cancellationToken);
        var referenceImages = await SourceImageLoader.LoadReferenceImagesAsync(
            request, context.Db, context.Storage, cancellationToken);

        List<MaskImageData>? maskImages = null;
        byte[]? overlayImage = null;
        var promptToSend = request.Prompt;

        if (request.MaskPayload != null)
        {
            overlayImage = Convert.FromBase64String(request.MaskPayload.OverlayImageBase64);
            promptToSend = PromptBuilder.BuildOverlayPrompt(request.MaskPayload, request.Prompt);
            _logger.LogInformation(
                "Inpainting with colored overlay (2-image), {LayerCount} layer(s)",
                request.MaskPayload.Layers.Count);
        }
        else if (request.MaskLayers is { Count: > 0 })
        {
            maskImages = request.MaskLayers
                .Select(ml => new MaskImageData(Convert.FromBase64String(ml.MaskImageBase64), ml.Instruction))
                .ToList();
            promptToSend = PromptBuilder.BuildMaskPrompt(request.MaskLayers, request.Prompt);
            _logger.LogInformation("Inpainting with {MaskCount} mask(s)", maskImages.Count);
        }

        var hasGeminiKey = await context.SecretStorage.HasSecretAsync(SecretKeyNames.GeminiApiKey);
        var hasFalKey = await context.SecretStorage.HasSecretAsync(SecretKeyNames.FalApiKey);
        var selectedModel = request.Model ?? GeminiModels.NanoBanana;
        var isGeminiModel = selectedModel.StartsWith("gemini-", StringComparison.Ordinal);
        var usesFalProvider = FalModels.IsFalRouted(selectedModel);

        Generation generation;

        if (isGeminiModel && hasGeminiKey)
        {
            context.Progress.Report(context.Job, 20, $"Generating with {selectedModel}...");

            var config = new GeminiImageConfig(
                Model: selectedModel,
                OutputFormat: request.OutputFormat?.ToLowerInvariant() ?? "png",
                AspectRatio: request.AspectRatio ?? "1:1",
                ImageSize: request.Resolution ?? "1K");

            using var progressCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var progressTask = GeminiProgressSimulator.RunAsync(
                context.Job, context.Progress, 20, 85, progressCts.Token);

            GeminiImageResult geminiResult;
            try
            {
                geminiResult = await context.GeminiClient.GenerateImageAsync(
                    promptToSend,
                    sourceImageData,
                    overlayImage,
                    referenceImages,
                    config,
                    overlayImage != null ? null : maskImages,
                    cancellationToken);
            }
            finally
            {
                progressCts.Cancel();
                try { await progressTask; } catch (OperationCanceledException) { }
            }

            context.Progress.Report(context.Job, 90, "Saving result...");
            generation = await context.Results.SaveGeminiResultAsync(
                context.Job.ProjectId,
                request.SourceCaptureId,
                null,
                JobType.Generate,
                request.Prompt,
                geminiResult,
                selectedModel,
                context.Db,
                context.Storage,
                cancellationToken,
                parametersJson);
        }
        else if (usesFalProvider && hasFalKey)
        {
            context.Progress.Report(context.Job, 20, $"Generating with {selectedModel}...");

            string? sourceImageUrl = null;
            if (sourceImageData != null)
            {
                sourceImageUrl = await context.FalClient.UploadImageAsync(
                    sourceImageData, $"{request.SourceCaptureId}.png", cancellationToken);
            }

            List<string>? referenceImageUrls = null;
            if (referenceImages is { Count: > 0 })
            {
                referenceImageUrls = new List<string>();
                for (var i = 0; i < referenceImages.Count; i++)
                {
                    var refUrl = await context.FalClient.UploadImageAsync(
                        referenceImages[i], $"ref_{i}.png", cancellationToken);
                    referenceImageUrls.Add(refUrl);
                }
            }

            var falInput = FalInputBuilder.Build(
                selectedModel, request, promptToSend, sourceImageUrl, referenceImageUrls);

            context.Job.ProviderModelId = selectedModel;
            var queueResponse = await context.FalClient.SubmitAsync(selectedModel, falInput, cancellationToken);
            context.Job.FalRequestId = queueResponse.RequestId;
            context.Db.Jobs.Update(context.Job);
            await context.Db.SaveChangesAsync(cancellationToken);

            var result = await context.FalPoller.PollAsync(
                context.Job,
                context.FalClient,
                selectedModel,
                queueResponse.RequestId,
                queueResponse.StatusUrl,
                queueResponse.ResponseUrl,
                cancellationToken);

            context.Progress.Report(context.Job, 90, "Saving result...");
            generation = await context.Results.SaveFalResultAsync(
                context.Job.ProjectId,
                request.SourceCaptureId,
                null,
                JobType.Generate,
                request.Prompt,
                result,
                context.Db,
                context.Storage,
                cancellationToken,
                parametersJson,
                selectedModel);
        }
        else
        {
            throw new InvalidOperationException(
                "No API key configured. Please set a Gemini or fal.ai API key in Settings.");
        }

        return generation.Id;
    }
}
