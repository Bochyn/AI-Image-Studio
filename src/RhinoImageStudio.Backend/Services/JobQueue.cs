using System.Threading.Channels;
using RhinoImageStudio.Shared.Models;

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
/// Thread-safe background job queue using System.Threading.Channels.
/// </summary>
public class JobQueue : IJobQueue
{
    private const int MaxQueueSize = 64;

    private readonly Channel<Job> _channel;
    private int _queueCount;

    public JobQueue()
    {
        _channel = Channel.CreateBounded<Job>(new BoundedChannelOptions(MaxQueueSize)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    }

    public int QueueCount => _queueCount;

    public async ValueTask EnqueueAsync(Job job, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(job);

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
