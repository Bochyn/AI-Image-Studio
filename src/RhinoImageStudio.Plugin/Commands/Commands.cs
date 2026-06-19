using Rhino;
using Rhino.Commands;
using Rhino.UI;
using RhinoImageStudio.Plugin.RhinoCommon;
using RhinoImageStudio.Shared.Constants;

namespace RhinoImageStudio.Plugin.Commands;

/// <summary>
/// Command to open/toggle the Image Studio panel
/// </summary>
public class ImageStudioCommand : Command
{
    public ImageStudioCommand()
    {
        Instance = this;
    }

    public static ImageStudioCommand? Instance { get; private set; }

    public override string EnglishName => "ImageStudio";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var panelId = ImageStudioPanel.PanelId;
        var visible = Panels.IsPanelVisible(panelId);

        if (visible)
        {
            Panels.ClosePanel(panelId);
            RhinoApp.WriteLine("Image Studio panel closed.");
        }
        else
        {
            Panels.OpenPanel(panelId);
            RhinoApp.WriteLine("Image Studio panel opened.");
        }

        return Result.Success;
    }
}

/// <summary>
/// Command to show the Image Studio panel (alias)
/// </summary>
public class ShowImageStudioCommand : Command
{
    public ShowImageStudioCommand()
    {
        Instance = this;
    }

    public static ShowImageStudioCommand? Instance { get; private set; }

    public override string EnglishName => "ShowImageStudio";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var panelId = ImageStudioPanel.PanelId;

        if (!Panels.IsPanelVisible(panelId))
        {
            Panels.OpenPanel(panelId);
        }

        RhinoApp.WriteLine("Image Studio panel shown.");
        return Result.Success;
    }
}

/// <summary>
/// Command to capture current viewport
/// </summary>
public class CaptureViewportCommand : Command
{
    public CaptureViewportCommand()
    {
        Instance = this;
    }

    public static CaptureViewportCommand? Instance { get; private set; }

    public override string EnglishName => "ImageStudioCapture";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        // This command requires an active session
        // For now, just capture and show result

        var result = ViewportCaptureService.CaptureActiveViewport(
            1024, 1024, RhinoBridgeConstants.DisplayModeCurrent);

        if (result == null)
        {
            RhinoApp.WriteLine("Failed to capture viewport.");
            return Result.Failure;
        }

        RhinoApp.WriteLine($"Viewport captured: {result.Width}x{result.Height}");
        RhinoApp.WriteLine($"View: {result.ViewName}, Mode: {result.DisplayModeName}");

        // Could copy to clipboard or save to file here
        // For now, just confirm success

        return Result.Success;
    }
}
