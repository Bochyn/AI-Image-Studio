using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services;

public interface IBridgeTokenService
{
    string Token { get; }
    bool Validate(string? token);
}

/// <summary>
/// Shared localhost bridge token persisted alongside app data.
/// </summary>
public sealed class BridgeTokenService : IBridgeTokenService
{
    private readonly string _token;

    public BridgeTokenService()
    {
        var appDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RhinoImageStudio");
        Directory.CreateDirectory(appDir);
        var tokenPath = Path.Combine(appDir, RhinoBridgeConstants.BridgeTokenFileName);

        if (File.Exists(tokenPath))
        {
            _token = File.ReadAllText(tokenPath).Trim();
            if (string.IsNullOrEmpty(_token))
                _token = Regenerate(tokenPath);
        }
        else
        {
            _token = Regenerate(tokenPath);
        }
    }

    public string Token => _token;

    public bool Validate(string? token) =>
        !string.IsNullOrWhiteSpace(token) &&
        string.Equals(token.Trim(), _token, StringComparison.Ordinal);

    private static string Regenerate(string tokenPath)
    {
        var token = Guid.NewGuid().ToString("N");
        File.WriteAllText(tokenPath, token);
        return token;
    }
}
