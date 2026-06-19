using Rhino;
using Rhino.Commands;

namespace RhinoImageStudio.Plugin.Mac.Commands;

public sealed class MacBackendCommand : Command
{
    public override string EnglishName => "ImageStudioStartBackend";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var backend = RhinoImageStudioMacPlugIn.Instance.BackendManager;
        var started = backend.StartAsync().GetAwaiter().GetResult();

        if (!started)
        {
            RhinoApp.WriteLine("Rhino Image Studio backend failed to start.");
            return Result.Failure;
        }

        RhinoApp.WriteLine($"Rhino Image Studio backend is running at {backend.BaseUrl}");
        return Result.Success;
    }
}
