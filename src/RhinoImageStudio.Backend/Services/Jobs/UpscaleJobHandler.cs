using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Enums;

namespace RhinoImageStudio.Backend.Services.Jobs;

public sealed class UpscaleJobHandler : IJobHandler
{
    public JobType JobType => JobType.Upscale;

    public async Task<Guid> ExecuteAsync(JobExecutionContext context, CancellationToken cancellationToken)
    {
        var request = JobRequestJson.Deserialize<UpscaleRequest>(context.Job.RequestJson);

        var sourceGeneration = await context.Db.Generations.FindAsync(
            [request.SourceGenerationId], cancellationToken)
            ?? throw new InvalidOperationException("Source generation not found");

        context.Progress.Report(context.Job, 10, "Preparing input...");

        var imageData = await context.Storage.ReadFileAsync(sourceGeneration.FilePath!, cancellationToken);
        var imageUrl = await context.FalClient.UploadImageAsync(
            imageData, $"{sourceGeneration.Id}.png", cancellationToken);

        var falInput = new Dictionary<string, object>
        {
            ["image_url"] = imageUrl,
            ["upscale_factor"] = request.UpscaleFactor,
            ["model"] = request.Model,
            ["face_enhancement"] = request.FaceEnhancement,
            ["output_format"] = request.OutputFormat.ToString().ToLowerInvariant()
        };

        context.Progress.Report(context.Job, 20, "Submitting to fal.ai...");
        context.Job.ProviderModelId = FalModels.TopazUpscale;
        var queueResponse = await context.FalClient.SubmitAsync(
            FalModels.TopazUpscale, falInput, cancellationToken);
        context.Job.FalRequestId = queueResponse.RequestId;

        var result = await context.FalPoller.PollAsync(
            context.Job,
            context.FalClient,
            FalModels.TopazUpscale,
            queueResponse.RequestId,
            cancellationToken: cancellationToken);

        context.Progress.Report(context.Job, 90, "Saving result...");
        var generation = await context.Results.SaveFalResultAsync(
            context.Job.ProjectId,
            sourceGeneration.SourceCaptureId,
            request.SourceGenerationId,
            JobType.Upscale,
            null,
            result,
            context.Db,
            context.Storage,
            cancellationToken);

        return generation.Id;
    }
}
