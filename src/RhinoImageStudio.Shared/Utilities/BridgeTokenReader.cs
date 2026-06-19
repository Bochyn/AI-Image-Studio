using RhinoImageStudio.Shared.Constants;

namespace RhinoImageStudio.Shared.Utilities;

/// <summary>
/// Reads the persisted localhost bridge token shared between backend and plugins.
/// </summary>
public static class BridgeTokenReader
{
    public static string ReadToken()
    {
        var tokenPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RhinoImageStudio",
            RhinoBridgeConstants.BridgeTokenFileName);

        if (!File.Exists(tokenPath))
            return string.Empty;

        return File.ReadAllText(tokenPath).Trim();
    }
}
