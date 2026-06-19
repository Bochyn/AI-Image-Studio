using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services.Jobs;

internal static class GeminiProgressSimulator
{
    public static async Task RunAsync(
        Job job,
        JobProgressBroadcaster progress,
        int startProgress,
        int maxProgress,
        CancellationToken cancellationToken)
    {
        var current = startProgress;
        var messages = new[]
        {
            "Analyzing input...",
            "Processing with AI...",
            "Generating image...",
            "Refining details...",
            "Almost there..."
        };
        var messageIndex = 0;

        try
        {
            while (current < maxProgress && !cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(800, cancellationToken);

                var remaining = maxProgress - current;
                var increment = Math.Max(1, remaining / 8);
                current = Math.Min(current + increment, maxProgress);

                if (current > startProgress + (maxProgress - startProgress) / 3)
                    messageIndex = Math.Min(messageIndex + 1, messages.Length - 1);

                progress.Report(job, current, messages[messageIndex]);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the API call completes.
        }
    }
}
