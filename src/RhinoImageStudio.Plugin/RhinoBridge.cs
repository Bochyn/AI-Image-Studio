using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using Rhino;
using RhinoImageStudio.Plugin.RhinoCommon;
using RhinoImageStudio.Shared.Constants;

namespace RhinoImageStudio.Plugin;

/// <summary>
/// Bridge object exposed to JavaScript via WebView2 AddHostObjectToScript.
/// </summary>
[ComVisible(true)]
[ClassInterface(ClassInterfaceType.AutoDual)]
public class RhinoBridge
{
    private readonly int _backendPort;
    private readonly HttpClient _httpClient;

    public RhinoBridge(int backendPort)
    {
        _backendPort = backendPort;
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri($"http://localhost:{backendPort}")
        };
    }

    public string GetApiUrl() => $"http://localhost:{_backendPort}";

    public string? CaptureViewport(string sessionId, int width, int height, string displayModeStr)
    {
        try
        {
            if (!Guid.TryParse(sessionId, out var projectId))
            {
                RhinoApp.WriteLine("Capture failed: invalid project id.");
                return null;
            }

            var displayMode = string.Equals(displayModeStr, "Current", StringComparison.OrdinalIgnoreCase)
                ? RhinoBridgeConstants.DisplayModeCurrent
                : displayModeStr;

            var capture = RhinoUiThread.RunAsync(() =>
                    ViewportCaptureService.CaptureActiveViewport(width, height, displayMode))
                .GetAwaiter()
                .GetResult();

            if (capture == null)
                return null;

            return CaptureUploadClient.UploadAsync(
                    _httpClient,
                    projectId,
                    capture.ImageBytes,
                    capture.Width,
                    capture.Height,
                    capture.DisplayModeName,
                    capture.ViewName)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Capture error: {ex.Message}");
            return null;
        }
    }

    public string GetDisplayModes() =>
        RhinoUiThread.RunAsync(RhinoDisplayQueries.GetDisplayModesJson).GetAwaiter().GetResult() ?? "[]";

    public string? GetActiveViewportName() =>
        RhinoUiThread.RunAsync(() => RhinoDoc.ActiveDoc?.Views.ActiveView?.ActiveViewport.Name)
            .GetAwaiter()
            .GetResult();

    public bool SetActiveViewport(string viewportName) =>
        RhinoUiThread.RunAsync(() =>
        {
            var doc = RhinoDoc.ActiveDoc;
            if (doc == null) return false;

            foreach (var view in doc.Views)
            {
                if (!view.ActiveViewport.Name.Equals(viewportName, StringComparison.OrdinalIgnoreCase))
                    continue;
                doc.Views.ActiveView = view;
                return true;
            }

            return false;
        }).GetAwaiter().GetResult();

    public string GetViewports() =>
        RhinoUiThread.RunAsync(RhinoDisplayQueries.GetViewportsJson).GetAwaiter().GetResult() ?? "[]";

    public void ZoomSelected() =>
        RhinoUiThread.RunAsync(() => RhinoApp.RunScript("_ZoomSelected", false)).GetAwaiter().GetResult();

    public void ZoomExtents() =>
        RhinoUiThread.RunAsync(() => RhinoApp.RunScript("_ZoomExtents", false)).GetAwaiter().GetResult();

    public void RunCommand(string command) =>
        RhinoUiThread.RunAsync(() => RhinoApp.RunScript(command, false)).GetAwaiter().GetResult();

    public string GetActiveDisplayMode()
    {
        var json = RhinoUiThread.RunAsync(RhinoDisplayQueries.GetActiveDisplayModeJson)
            .GetAwaiter()
            .GetResult();
        return json == null ? "Shaded" : JsonSerializer.Deserialize<string>(json) ?? "Shaded";
    }
}
