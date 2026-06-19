using Rhino;
using Rhino.Commands;
using System.IO;
using System.Text.Json;

namespace RhinoImageStudio.Plugin.Mac.Commands;

public sealed class MacStatusCommand : Command
{
    public override string EnglishName => "ImageStudioMacStatus";

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
        var statusDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RhinoImageStudio"
        );
        Directory.CreateDirectory(statusDirectory);

        var statusPath = Path.Combine(statusDirectory, "mac-plugin-status.json");
        var statusJson = JsonSerializer.Serialize(new
        {
            loaded = true,
            command = EnglishName,
            runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            document = doc.Name ?? "Untitled",
            timestampUtc = DateTimeOffset.UtcNow
        });

        File.WriteAllText(statusPath, statusJson);

        RhinoApp.WriteLine("Rhino Image Studio macOS shell is loaded.");
        RhinoApp.WriteLine($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");
        RhinoApp.WriteLine($"Document: {doc.Name ?? "Untitled"}");
        RhinoApp.WriteLine($"Status marker: {statusPath}");
        return Result.Success;
    }
}
