using System.Collections.Concurrent;
using System.Threading.Channels;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Services;

public interface IRhinoBridgeService
{
    bool IsConnected { get; }
    DateTimeOffset? LastSeenUtc { get; }
    Task<RhinoBridgeWorkItem?> WaitForWorkAsync(string clientId, string? bridgeToken, CancellationToken ct);
    Task<string> RequestCaptureAsync(Guid projectId, int width, int height, string displayMode, CancellationToken ct);
    Task<string> RequestQueryAsync(string workType, CancellationToken ct);
    Task CompleteAsync(Guid requestId, RhinoBridgeCompletion completion, string? bridgeToken);
}

public sealed class RhinoBridgeService : IRhinoBridgeService
{
    private static readonly TimeSpan ConnectedWindow = TimeSpan.FromSeconds(45);
    private static readonly TimeSpan CaptureTimeout = TimeSpan.FromSeconds(90);
    private static readonly TimeSpan QueryTimeout = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan LongPollTimeout = TimeSpan.FromSeconds(25);
    private const int MaxPendingWorkItems = 16;

    private readonly Channel<RhinoBridgeWorkItem> _workItems = Channel.CreateBounded<RhinoBridgeWorkItem>(
        new BoundedChannelOptions(MaxPendingWorkItems)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<RhinoBridgeCompletion>> _pendingCompletions = new();
    private readonly IBridgeTokenService _bridgeToken;
    private DateTimeOffset? _lastSeenUtc;

    public RhinoBridgeService(IBridgeTokenService bridgeToken)
    {
        _bridgeToken = bridgeToken;
    }

    public bool IsConnected => _lastSeenUtc.HasValue && DateTimeOffset.UtcNow - _lastSeenUtc.Value < ConnectedWindow;
    public DateTimeOffset? LastSeenUtc => _lastSeenUtc;

    public async Task<RhinoBridgeWorkItem?> WaitForWorkAsync(string clientId, string? bridgeToken, CancellationToken ct)
    {
        if (!_bridgeToken.Validate(bridgeToken))
            throw new UnauthorizedAccessException("Invalid bridge token.");

        MarkSeen();

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(LongPollTimeout);

        try
        {
            var item = await _workItems.Reader.ReadAsync(timeout.Token);
            MarkSeen();
            return item;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            MarkSeen();
            return null;
        }
    }

    public Task<string> RequestCaptureAsync(Guid projectId, int width, int height, string displayMode, CancellationToken ct) =>
        RequestWorkAsync(
            new RhinoBridgeWorkItem(
                Guid.NewGuid(),
                RhinoBridgeConstants.CaptureViewport,
                new RhinoCaptureRequest(projectId, width, height, displayMode)),
            CaptureTimeout,
            completion =>
            {
                if (string.IsNullOrWhiteSpace(completion.CaptureId))
                    throw new InvalidOperationException("Rhino viewport capture did not return a capture id.");
                return completion.CaptureId;
            },
            ct);

    public Task<string> RequestQueryAsync(string workType, CancellationToken ct) =>
        RequestWorkAsync(
            new RhinoBridgeWorkItem(Guid.NewGuid(), workType),
            QueryTimeout,
            completion =>
            {
                if (string.IsNullOrWhiteSpace(completion.ResultJson))
                    throw new InvalidOperationException($"Rhino bridge query '{workType}' returned no data.");
                return completion.ResultJson;
            },
            ct);

    public Task CompleteAsync(Guid requestId, RhinoBridgeCompletion completion, string? bridgeToken)
    {
        if (!_bridgeToken.Validate(bridgeToken))
            throw new UnauthorizedAccessException("Invalid bridge token.");

        MarkSeen();

        if (_pendingCompletions.TryRemove(requestId, out var pending))
            pending.TrySetResult(completion);
        else
            // Late completion after client timeout — log-worthy but not fatal
            return Task.CompletedTask;

        return Task.CompletedTask;
    }

    private async Task<string> RequestWorkAsync(
        RhinoBridgeWorkItem item,
        TimeSpan timeout,
        Func<RhinoBridgeCompletion, string> extractResult,
        CancellationToken ct)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Rhino bridge is not connected.");

        var completion = new TaskCompletionSource<RhinoBridgeCompletion>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (!_pendingCompletions.TryAdd(item.Id, completion))
            throw new InvalidOperationException("Could not register Rhino bridge request.");

        if (!_workItems.Writer.TryWrite(item))
        {
            _pendingCompletions.TryRemove(item.Id, out _);
            throw new InvalidOperationException(
                $"Rhino bridge queue is full ({MaxPendingWorkItems} pending items). Try again shortly.");
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);

        try
        {
            using var registration = timeoutCts.Token.Register(() => completion.TrySetCanceled(timeoutCts.Token));
            var result = await completion.Task;

            if (!result.Success)
                throw new InvalidOperationException(result.Error ?? "Rhino bridge request failed.");

            return extractResult(result);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("Timed out waiting for Rhino bridge response.");
        }
        finally
        {
            _pendingCompletions.TryRemove(item.Id, out _);
        }
    }

    private void MarkSeen() => _lastSeenUtc = DateTimeOffset.UtcNow;
}
