namespace RhinoImageStudio.Shared.Contracts;

public sealed record RhinoBridgeWorkItem(
    Guid Id,
    string Type,
    RhinoCaptureRequest? Capture = null);

public sealed record RhinoCaptureRequest(
    Guid ProjectId,
    int Width,
    int Height,
    string DisplayMode);

public sealed record RhinoBridgeCompletion(
    bool Success,
    string? CaptureId,
    string? Error,
    string? ResultJson = null);

public sealed record CaptureUploadResponse(string Id);

public sealed record RhinoDisplayModeInfo(string Name, string Id);

public sealed record RhinoViewportInfo(
    string Name,
    bool IsActive,
    int Width = 0,
    int Height = 0,
    string? DisplayMode = null);
