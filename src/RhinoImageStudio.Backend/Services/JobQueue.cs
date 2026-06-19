using System.Threading.Channels;
using RhinoImageStudio.Shared.Models;
using RhinoImageStudio.Shared.Enums;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Interface for the background job queue
/// </summary>
public interface IJobQueue
{
    ValueTask EnqueueAsync(Job job, CancellationToken cancellationToken = default);
    ValueTask<Job> DequeueAsync(CancellationToken cancellationToken);
    int QueueCount { get; }
}

/// <summary>
/// Thread-safe background job queue using System.Threading.Channels
/// </summary>
public class JobQueue : IJobQueue
{
    private readonly Channel<Job> _channel;
    private int _queueCount;

    public JobQueue()
    {
        // Unbounded channel with single reader for optimal performance
        _channel = Channel.CreateUnbounded<Job>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public int QueueCount => _queueCount;

    public async ValueTask EnqueueAsync(Job job, CancellationToken cancellationToken = default)
    {
        if (job == null)
            throw new ArgumentNullException(nameof(job));

        await _channel.Writer.WriteAsync(job, cancellationToken);
        Interlocked.Increment(ref _queueCount);
    }

    public async ValueTask<Job> DequeueAsync(CancellationToken cancellationToken)
    {
        var job = await _channel.Reader.ReadAsync(cancellationToken);
        Interlocked.Decrement(ref _queueCount);
        return job;
    }
}
