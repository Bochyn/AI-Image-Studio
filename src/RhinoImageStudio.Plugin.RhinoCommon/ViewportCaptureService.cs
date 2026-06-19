using System.Drawing;
using System.Drawing.Imaging;
using Rhino;
using Rhino.Display;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Utilities;

namespace RhinoImageStudio.Plugin.RhinoCommon;

public static class ViewportCaptureService
{
    public static ViewportCaptureResult? CaptureActiveViewport(int width, int height, string displayMode)
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc == null)
        {
            RhinoApp.WriteLine("Rhino Image Studio capture failed: no active document.");
            return null;
        }

        var view = doc.Views.ActiveView;
        if (view == null)
        {
            RhinoApp.WriteLine("Rhino Image Studio capture failed: no active view.");
            return null;
        }

        try
        {
            using var bitmap = CaptureBitmap(view, width, height, displayMode);
            if (bitmap == null)
            {
                RhinoApp.WriteLine("Rhino Image Studio capture failed: bitmap is null.");
                return null;
            }

            var viewport = view.ActiveViewport;
            var cameraLocation = viewport.CameraLocation;
            var cameraTarget = viewport.CameraTarget;
            var displayModeName = DisplayModeMapping.IsCurrentDisplayMode(displayMode)
                ? viewport.DisplayMode?.EnglishName ?? displayMode
                : ResolveRhinoDisplayMode(displayMode)?.EnglishName ?? displayMode;

            return new ViewportCaptureResult(
                ToPngBytes(bitmap),
                bitmap.Width,
                bitmap.Height,
                view.ActiveViewport.Name,
                displayModeName,
                $"{cameraLocation.X:F2},{cameraLocation.Y:F2},{cameraLocation.Z:F2}",
                $"{cameraTarget.X:F2},{cameraTarget.Y:F2},{cameraTarget.Z:F2}",
                viewport.Camera35mmLensLength);
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Rhino Image Studio capture failed: {ex.Message}");
            return null;
        }
    }

    public static byte[] ToPngBytes(Bitmap bitmap)
    {
        using var stream = new MemoryStream();
#pragma warning disable CA1416
        bitmap.Save(stream, ImageFormat.Png);
#pragma warning restore CA1416
        return stream.ToArray();
    }

    private static Bitmap? CaptureBitmap(RhinoView view, int width, int height, string displayMode)
    {
        var size = new Size(width, height);

        if (DisplayModeMapping.IsCurrentDisplayMode(displayMode))
            return view.CaptureToBitmap(size);

        var mode = ResolveRhinoDisplayMode(displayMode);
        return mode == null ? view.CaptureToBitmap(size) : view.CaptureToBitmap(size, mode);
    }

    private static DisplayModeDescription? ResolveRhinoDisplayMode(string displayMode)
    {
        var parsed = DisplayModeMapping.Parse(displayMode);
        var name = parsed.HasValue
            ? DisplayModeMapping.ToRhinoEnglishName(parsed.Value)
            : displayMode;

        var mode = DisplayModeDescription.FindByName(name);
        if (mode != null)
            return mode;

        RhinoApp.WriteLine($"Rhino Image Studio display mode '{name}' not found. Using current viewport mode.");
        return null;
    }
}

public sealed class ViewportCaptureResult
{
    public ViewportCaptureResult(
        byte[] imageBytes,
        int width,
        int height,
        string? viewName,
        string displayModeName,
        string? cameraPosition,
        string? cameraTarget,
        double? cameraLens)
    {
        ImageBytes = imageBytes;
        Width = width;
        Height = height;
        ViewName = viewName;
        DisplayModeName = displayModeName;
        CameraPosition = cameraPosition;
        CameraTarget = cameraTarget;
        CameraLens = cameraLens;
    }

    public byte[] ImageBytes { get; }
    public int Width { get; }
    public int Height { get; }
    public string? ViewName { get; }
    public string DisplayModeName { get; }
    public string? CameraPosition { get; }
    public string? CameraTarget { get; }
    public double? CameraLens { get; }
}
