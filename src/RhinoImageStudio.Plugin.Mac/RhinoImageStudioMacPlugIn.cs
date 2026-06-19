using System.Reflection;
using System.Runtime.InteropServices;
using Rhino;
using Rhino.PlugIns;

[assembly: AssemblyTitle("Rhino Image Studio")]
[assembly: AssemblyDescription("AI-powered viewport visualization plugin for Rhinoceros on macOS")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Rhino Image Studio")]
[assembly: AssemblyProduct("Rhino Image Studio")]
[assembly: AssemblyCopyright("Copyright (c) 2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]

// Same product plug-in id as the Windows plug-in. Only one platform build should be installed at a time.
[assembly: Guid("A1B2C3D4-E5F6-7890-ABCD-123456789ABC")]

[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

[assembly: PlugInDescription(DescriptionType.Organization, "Rhino Image Studio")]
[assembly: PlugInDescription(DescriptionType.WebSite, "")]
[assembly: PlugInDescription(DescriptionType.Email, "")]

namespace RhinoImageStudio.Plugin.Mac;

public sealed class RhinoImageStudioMacPlugIn : PlugIn
{
    private MacBackendManager? _backendManager;

    public RhinoImageStudioMacPlugIn()
    {
        Instance = this;
    }

    public static RhinoImageStudioMacPlugIn Instance { get; private set; } = null!;
    public MacBackendManager BackendManager => _backendManager ??= new MacBackendManager();

    public override PlugInLoadTime LoadTime => PlugInLoadTime.AtStartup;

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        RhinoApp.WriteLine("Rhino Image Studio macOS shell loaded.");
        return LoadReturnCode.Success;
    }

    protected override void OnShutdown()
    {
        RhinoApp.WriteLine("Rhino Image Studio macOS shell shutting down.");
        _backendManager?.Dispose();
        base.OnShutdown();
    }
}
