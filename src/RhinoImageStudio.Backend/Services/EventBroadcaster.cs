using System.Collections.Concurrent;
using System.Threading.Channels;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Pub/sub event broadcaster for SSE.
/// Each subscriber gets a dedicated channel - broadcast writes to ALL subscribers.
/// </summary>
public interface IEventBroadcaster
{
    void Broadcast(JobDto jobDto);
    void BroadcastToProject(Guid projectId, JobDto jobDto);
    IAsyncEnumerable<JobDto> SubscribeAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<JobDto> SubscribeToProjectAsync(Guid projectId, CancellationToken cancellationToken);
}

public class EventBroadcaster : IEventBroadcaster
{
    private readonly ConcurrentDictionary<Guid, Channel<JobDto>> _globalSubscribers = new();
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, Channel<JobDto>>> _projectSubscribers = new();

    public void Broadcast(JobDto jobDto)
    {
        foreach (var (_, channel) in _globalSubscribers)
        {
            channel.Writer.TryWrite(jobDto);
        }
    }

    public void BroadcastToProject(Guid projectId, JobDto jobDto)
    {
        // Broadcast to all global subscribers
        Broadcast(jobDto);

        // Also broadcast to project-specific subscribers
        if (_projectSubscribers.TryGetValue(projectId, out var subscribers))
        {
            foreach (var (_, channel) in subscribers)
            {
                channel.Writer.TryWrite(jobDto);
            }
        }
    }

    public async IAsyncEnumerable<JobDto> SubscribeAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateBounded<JobDto>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        _globalSubscribers.TryAdd(subscriberId, channel);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return evt;
            }
        }
        finally
        {
            _globalSubscribers.TryRemove(subscriberId, out _);
            channel.Writer.TryComplete();
        }
    }

    public async IAsyncEnumerable<JobDto> SubscribeToProjectAsync(
        Guid projectId,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var subscriberId = Guid.NewGuid();
        var channel = Channel.CreateBounded<JobDto>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false
        });

        var projectSubs = _projectSubscribers.GetOrAdd(projectId, _ => new ConcurrentDictionary<Guid, Channel<JobDto>>());
        projectSubs.TryAdd(subscriberId, channel);

        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(cancellationToken))
            {
                yield return evt;
            }
        }
        finally
        {
            projectSubs.TryRemove(subscriberId, out _);
            channel.Writer.TryComplete();

            // Clean up empty project subscriber dictionaries
            if (projectSubs.IsEmpty)
            {
                _projectSubscribers.TryRemove(projectId, out _);
            }
        }
    }
}
