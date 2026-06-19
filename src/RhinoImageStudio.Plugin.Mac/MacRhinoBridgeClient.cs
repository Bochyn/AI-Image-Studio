using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Utilities;
using RhinoImageStudio.Plugin.RhinoCommon;

namespace RhinoImageStudio.Plugin.Mac;

public sealed class MacRhinoBridgeClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly Uri _baseUri;
    private CancellationTokenSource? _cts;
    private Task? _pollingTask;
    private bool _isDisposed;

    public MacRhinoBridgeClient(string baseUrl)
    {
        _baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
        _httpClient = new HttpClient
        {
            BaseAddress = _baseUri,
            Timeout = TimeSpan.FromSeconds(35)
        };
    }

    public void Start()
    {
        if (_pollingTask is { IsCompleted: false })
            return;

        _cts = new CancellationTokenSource();
        _pollingTask = Task.Run(() => PollAsync(_cts.Token));
        Rhino.RhinoApp.WriteLine($"Rhino Image Studio bridge connected to {_baseUri}.");
    }

    public void Stop()
    {
        if (_cts == null)
            return;

        _cts.Cancel();
        try
        {
            _pollingTask?.Wait(TimeSpan.FromSeconds(2));
        }
        catch
        {
            // Ignore shutdown races.
        }
        finally
        {
            _cts.Dispose();
            _cts = null;
            _pollingTask = null;
        }
    }

    private async Task PollAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var workItem = await PollOnceAsync(ct);
                if (workItem != null)
                    await HandleWorkItemAsync(workItem, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                Rhino.RhinoApp.WriteLine($"Rhino Image Studio bridge polling error: {ex.Message}");
                await Task.Delay(TimeSpan.FromSeconds(2), ct).ConfigureAwait(false);
            }
        }
    }

    private async Task<RhinoBridgeWorkItem?> PollOnceAsync(CancellationToken ct)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"api/rhino/bridge/next?clientId={RhinoBridgeConstants.MacPluginClientId}");
        request.Headers.Add(RhinoBridgeConstants.BridgeTokenHeader, BridgeTokenReader.ReadToken());

        using var response = await _httpClient.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            using var retryRequest = new HttpRequestMessage(
                HttpMethod.Get,
                $"api/rhino/bridge/next?clientId={RhinoBridgeConstants.MacPluginClientId}");
            retryRequest.Headers.Add(RhinoBridgeConstants.BridgeTokenHeader, BridgeTokenReader.ReadToken());
            using var retryResponse = await _httpClient.SendAsync(retryRequest, ct);
            if (retryResponse.StatusCode == System.Net.HttpStatusCode.NoContent)
                return null;
            retryResponse.EnsureSuccessStatusCode();
            return await retryResponse.Content.ReadFromJsonAsync<RhinoBridgeWorkItem>(JsonOptions, ct);
        }

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
            return null;

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RhinoBridgeWorkItem>(JsonOptions, ct);
    }

    private async Task HandleWorkItemAsync(RhinoBridgeWorkItem workItem, CancellationToken ct)
    {
        try
        {
            switch (workItem.Type)
            {
                case RhinoBridgeConstants.CaptureViewport:
                    await HandleCaptureAsync(workItem, ct);
                    break;
                case RhinoBridgeConstants.GetDisplayModes:
                    await CompleteAsync(workItem.Id, true, null, null,
                        RhinoDisplayQueries.GetDisplayModesJson(), ct);
                    break;
                case RhinoBridgeConstants.GetViewports:
                    await CompleteAsync(workItem.Id, true, null, null,
                        RhinoDisplayQueries.GetViewportsJson(), ct);
                    break;
                case RhinoBridgeConstants.GetActiveDisplayMode:
                    await CompleteAsync(workItem.Id, true, null, null,
                        RhinoDisplayQueries.GetActiveDisplayModeJson(), ct);
                    break;
                default:
                    await CompleteAsync(workItem.Id, false, null, $"Unsupported bridge request: {workItem.Type}", null, ct);
                    break;
            }
        }
        catch (Exception ex)
        {
            await CompleteAsync(workItem.Id, false, null, ex.Message, null, ct);
        }
    }

    private async Task HandleCaptureAsync(RhinoBridgeWorkItem workItem, CancellationToken ct)
    {
        if (workItem.Capture == null)
        {
            await CompleteAsync(workItem.Id, false, null, "Capture request missing payload.", null, ct);
            return;
        }

        var capture = await RhinoUiThread.RunAsync(
            () => ViewportCaptureService.CaptureActiveViewport(
                workItem.Capture.Width,
                workItem.Capture.Height,
                workItem.Capture.DisplayMode),
            ct);

        if (capture == null)
        {
            await CompleteAsync(workItem.Id, false, null, "Viewport capture returned no image.", null, ct);
            return;
        }

        var captureId = await CaptureUploadClient.UploadAsync(
            _httpClient,
            workItem.Capture.ProjectId,
            capture.ImageBytes,
            capture.Width,
            capture.Height,
            capture.DisplayModeName,
            capture.ViewName,
            ct);

        await CompleteAsync(workItem.Id, true, captureId, null, null, ct);
    }

    private async Task CompleteAsync(
        Guid requestId,
        bool success,
        string? captureId,
        string? error,
        string? resultJson,
        CancellationToken ct)
    {
        var completion = new RhinoBridgeCompletion(success, captureId, error, resultJson);
        using var request = new HttpRequestMessage(HttpMethod.Post, $"api/rhino/bridge/{requestId}/complete")
        {
            Content = JsonContent.Create(completion, options: JsonOptions)
        };
        request.Headers.Add(RhinoBridgeConstants.BridgeTokenHeader, BridgeTokenReader.ReadToken());

        using var response = await _httpClient.SendAsync(request, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            using var retryRequest = new HttpRequestMessage(HttpMethod.Post, $"api/rhino/bridge/{requestId}/complete")
            {
                Content = JsonContent.Create(completion, options: JsonOptions)
            };
            retryRequest.Headers.Add(RhinoBridgeConstants.BridgeTokenHeader, BridgeTokenReader.ReadToken());
            using var retryResponse = await _httpClient.SendAsync(retryRequest, ct);
            retryResponse.EnsureSuccessStatusCode();
            return;
        }

        response.EnsureSuccessStatusCode();
    }

    public void Dispose()
    {
        if (_isDisposed)
            return;

        _isDisposed = true;
        Stop();
        _httpClient.Dispose();
    }
}
