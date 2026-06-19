using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Rhino;
using Rhino.PlugIns;

// Assembly attributes (required since GenerateAssemblyInfo is disabled)
[assembly: AssemblyTitle("Rhino Image Studio")]
[assembly: AssemblyDescription("AI-powered viewport visualization plugin for Rhinoceros")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Rhino Image Studio")]
[assembly: AssemblyProduct("Rhino Image Studio")]
[assembly: AssemblyCopyright("Copyright © 2026")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]
[assembly: ComVisible(false)]

// Plugin GUID - This is the ID of the Rhino plug-in
[assembly: Guid("A1B2C3D4-E5F6-7890-ABCD-123456789ABC")]

// Assembly version
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Rhino plugin description attributes
[assembly: PlugInDescription(DescriptionType.Address, "")]
[assembly: PlugInDescription(DescriptionType.Country, "")]
[assembly: PlugInDescription(DescriptionType.Email, "")]
[assembly: PlugInDescription(DescriptionType.Phone, "")]
[assembly: PlugInDescription(DescriptionType.Fax, "")]
[assembly: PlugInDescription(DescriptionType.Organization, "Rhino Image Studio")]
[assembly: PlugInDescription(DescriptionType.UpdateUrl, "")]
[assembly: PlugInDescription(DescriptionType.WebSite, "")]

namespace RhinoImageStudio.Plugin;

/// <summary>
/// Rhino Image Studio Plugin - AI-powered viewport visualization
/// </summary>
public class RhinoImageStudioPlugIn : PlugIn
{
    private static RhinoImageStudioPlugIn? _instance;
    private BackendManager? _backendManager;

    public RhinoImageStudioPlugIn()
    {
        _instance = this;
    }

    /// <summary>
    /// Gets the only instance of the RhinoImageStudioPlugIn plugin.
    /// </summary>
    public static RhinoImageStudioPlugIn Instance => _instance!;

    /// <summary>
    /// The backend manager that controls the sidecar process.
    /// </summary>
    public BackendManager BackendManager => _backendManager!;

    /// <summary>
    /// Load when needed (when command is first invoked)
    /// </summary>
    public override PlugInLoadTime LoadTime => PlugInLoadTime.WhenNeeded;

    protected override LoadReturnCode OnLoad(ref string errorMessage)
    {
        RhinoApp.WriteLine("Rhino Image Studio loading...");

        try
        {
            // Initialize backend manager
            _backendManager = new BackendManager();

            // Register the panel
            var panelType = typeof(ImageStudioPanel);
            Rhino.UI.Panels.RegisterPanel(this, panelType, "Image Studio", null);

            RhinoApp.WriteLine("Rhino Image Studio loaded successfully.");
            return LoadReturnCode.Success;
        }
        catch (Exception ex)
        {
            errorMessage = $"Failed to load Rhino Image Studio: {ex.Message}";
            RhinoApp.WriteLine(errorMessage);
            return LoadReturnCode.ErrorShowDialog;
        }
    }

    protected override void OnShutdown()
    {
        RhinoApp.WriteLine("Rhino Image Studio shutting down...");

        try
        {
            _backendManager?.Dispose();
        }
        catch (Exception ex)
        {
            RhinoApp.WriteLine($"Error during shutdown: {ex.Message}");
        }

        base.OnShutdown();
    }
}
