namespace RhinoImageStudio.Shared.Constants;

/// <summary>
/// HTTP bridge work types and client identifiers.
/// </summary>
public static class RhinoBridgeConstants
{
    public const string CaptureViewport = "captureViewport";
    public const string GetDisplayModes = "getDisplayModes";
    public const string GetViewports = "getViewports";
    public const string GetActiveDisplayMode = "getActiveDisplayMode";

    public const string MacPluginClientId = "mac-plugin";
    public const string BridgeTokenHeader = "X-Rhino-Bridge-Token";
    public const string BridgeTokenFileName = "bridge.token";

    public const string DisplayModeCurrent = "Current";
    public const string DisplayModeViewport = "viewport";
}
