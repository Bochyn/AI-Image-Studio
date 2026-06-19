using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Enums;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services.Jobs;

public sealed class MultiAngleJobHandler : IJobHandler
{
    public JobType JobType => JobType.MultiAngle;

    public async Task<Guid> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var request = JobRequestJson.Deserialize<MultiAngleRequest>(context.Job.RequestJson);

        string sourceFilePath;
        Guid? sourceCaptureId;
        Guid? sourceGenerationId;
        Guid sourceId;

        if (request.SourceGenerationId.HasValue)
        {
            var sourceGeneration = await context.Db.Generations.FindAsync(
                [request.SourceGenerationId.Value], cancellationToken)
                ?? throw new InvalidOperationException("Source generation not found");
            sourceFilePath = sourceGeneration.FilePath!;
            sourceCaptureId = sourceGeneration.SourceCaptureId;
            sourceGenerationId = sourceGeneration.Id;
            sourceId = sourceGeneration.Id;
        }
        else if (request.SourceCaptureId.HasValue)
        {
            var sourceCapture = await context.Db.Captures.FindAsync(
                [request.SourceCaptureId.Value], cancellationToken)
                ?? throw new InvalidOperationException("Source capture not found");
            sourceFilePath = sourceCapture.FilePath;
            sourceCaptureId = sourceCapture.Id;
            sourceGenerationId = null;
            sourceId = sourceCapture.Id;
        }
        else
        {
            throw new InvalidOperationException(
                "Either SourceGenerationId or SourceCaptureId must be provided");
        }

        context.Progress.Report(context.Job, 10, "Preparing input...");

        var imageData = await context.Storage.ReadFileAsync(sourceFilePath, cancellationToken);
        var imageUrl = await context.FalClient.UploadImageAsync(imageData, $"{sourceId}.png", cancellationToken);

        var falInput = new Dictionary<string, object>
        {
            ["image_urls"] = new[] { imageUrl },
            ["horizontal_angle"] = request.HorizontalAngle,
            ["vertical_angle"] = request.VerticalAngle,
            ["zoom"] = request.Zoom,
            ["lora_scale"] = request.LoraScale,
            ["num_images"] = request.NumImages
        };

        context.Progress.Report(context.Job, 20, "Submitting to fal.ai...");
        context.Job.ProviderModelId = FalModels.QwenMultipleAngles;
        var queueResponse = await context.FalClient.SubmitAsync(
            FalModels.QwenMultipleAngles, falInput, cancellationToken);
        context.Job.FalRequestId = queueResponse.RequestId;

        var result = await context.FalPoller.PollAsync(
            context.Job,
            context.FalClient,
            FalModels.QwenMultipleAngles,
            queueResponse.RequestId,
            cancellationToken: cancellationToken);

        context.Progress.Report(context.Job, 90, "Saving result...");
        var generation = await context.Results.SaveFalResultAsync(
            context.Job.ProjectId,
            sourceCaptureId,
            sourceGenerationId,
            JobType.MultiAngle,
            result.Prompt,
            result,
            context.Db,
            context.Storage,
            cancellationToken);

        generation.Azimuth = request.HorizontalAngle;
        generation.Elevation = request.VerticalAngle;
        generation.Zoom = request.Zoom;
        await context.Db.SaveChangesAsync(cancellationToken);

        return generation.Id;
    }
}
