using System.Diagnostics;
using Rhino;
using Rhino.Commands;

namespace RhinoImageStudio.Plugin.Mac.Commands;

public sealed class MacOpenStudioCommand : Command
{
    public override string EnglishName => "ImageStudioOpen";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var backend = RhinoImageStudioMacPlugIn.Instance.BackendManager;
        var started = backend.StartAsync().GetAwaiter().GetResult();

        if (!started)
        {
            RhinoApp.WriteLine("Rhino Image Studio backend failed to start.");
            return Result.Failure;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "open",
            Arguments = backend.BaseUrl,
            UseShellExecute = false
        });

        RhinoApp.WriteLine($"Rhino Image Studio opened at {backend.BaseUrl}");
        return Result.Success;
    }
}
