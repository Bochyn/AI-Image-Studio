using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Enums;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services.Jobs;

public sealed class RefineJobHandler : IJobHandler
{
    public JobType JobType => JobType.Refine;

    public async Task<Guid> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var request = JobRequestJson.Deserialize<RefineRequest>(context.Job.RequestJson);

        var parentGeneration = await context.Db.Generations.FindAsync([request.ParentGenerationId], cancellationToken)
            ?? throw new InvalidOperationException("Parent generation not found");

        context.Progress.Report(context.Job, 10, "Preparing input...");
        var imageData = await context.Storage.ReadFileAsync(parentGeneration.FilePath!, cancellationToken);

        var hasGeminiKey = await context.SecretStorage.HasSecretAsync(SecretKeyNames.GeminiApiKey);
        var hasFalKey = await context.SecretStorage.HasSecretAsync(SecretKeyNames.FalApiKey);

        Generation generation;

        if (hasGeminiKey)
        {
            context.Progress.Report(context.Job, 20, "Refining with Gemini Nano Banana...");

            var geminiResult = await context.GeminiClient.EditImageAsync(
                request.Prompt,
                imageData,
                null,
                new GeminiImageConfig(Model: GeminiModels.NanoBanana),
                cancellationToken);

            context.Progress.Report(context.Job, 90, "Saving result...");
            generation = await context.Results.SaveGeminiResultAsync(
                context.Job.ProjectId,
                parentGeneration.SourceCaptureId,
                request.ParentGenerationId,
                JobType.Refine,
                request.Prompt,
                geminiResult,
                GeminiModels.NanoBanana,
                context.Db,
                context.Storage,
                cancellationToken);
        }
        else if (hasFalKey)
        {
            var imageUrl = await context.FalClient.UploadImageAsync(
                imageData, $"{parentGeneration.Id}.png", cancellationToken);

            var falInput = new Dictionary<string, object>
            {
                ["prompt"] = request.Prompt,
                ["image_urls"] = new[] { imageUrl },
                ["num_images"] = 1
            };

            context.Progress.Report(context.Job, 20, "Refining with fal.ai...");
            context.Job.ProviderModelId = FalModels.NanoBananaEdit;
            var queueResponse = await context.FalClient.SubmitAsync(
                FalModels.NanoBananaEdit, falInput, cancellationToken);
            context.Job.FalRequestId = queueResponse.RequestId;

            var refineResult = await context.FalPoller.PollAsync(
                context.Job,
                context.FalClient,
                FalModels.NanoBananaEdit,
                queueResponse.RequestId,
                cancellationToken: cancellationToken);

            context.Progress.Report(context.Job, 90, "Saving result...");
            generation = await context.Results.SaveFalResultAsync(
                context.Job.ProjectId,
                parentGeneration.SourceCaptureId,
                request.ParentGenerationId,
                JobType.Refine,
                request.Prompt,
                refineResult,
                context.Db,
                context.Storage,
                cancellationToken);
        }
        else
        {
            throw new InvalidOperationException(
                "No API key configured. Please set a Gemini or fal.ai API key in Settings.");
        }

        return generation.Id;
    }
}
