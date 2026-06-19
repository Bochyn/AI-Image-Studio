using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services.Jobs;

public sealed class FalJobPoller
{
    private readonly JobProgressBroadcaster _progress;

    public FalJobPoller(JobProgressBroadcaster progress)
    {
        _progress = progress;
    }

    public async Task<FalResultResponse> PollAsync(
        Job job,
        IFalAiClient falClient,
        string modelId,
        string requestId,
        string? statusUrl = null,
        string? responseUrl = null,
        CancellationToken cancellationToken = default)
    {
        var maxAttempts = FalModels.GetMaxPollAttempts(modelId);
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            await Task.Delay(1000, cancellationToken);
            attempt++;

            var status = await falClient.GetStatusAsync(modelId, requestId, statusUrl, cancellationToken);

            var progress = 20 + (int)((attempt / (float)maxAttempts) * 60);
            _progress.Report(job, Math.Min(progress, 80), $"Processing... ({status.Status})");

            if (status.Status == "COMPLETED")
                return await falClient.GetResultAsync(modelId, requestId, responseUrl, cancellationToken);

            if (status.Status == "FAILED")
            {
                var errorMessage = status.Logs?.LastOrDefault()?.Message ?? "Unknown error";
                throw new InvalidOperationException($"fal.ai job failed: {errorMessage}");
            }
        }

        throw new TimeoutException("Job timed out waiting for fal.ai response");
    }
}
