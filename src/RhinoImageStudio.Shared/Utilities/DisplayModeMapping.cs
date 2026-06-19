using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Enums;

namespace RhinoImageStudio.Shared.Utilities;

/// <summary>
/// Single source of truth for DisplayMode enum ↔ Rhino English display mode names.
/// </summary>
public static class DisplayModeMapping
{
    private static readonly IReadOnlyDictionary<DisplayMode, string> ToRhinoName = new Dictionary<DisplayMode, string>
    {
        [DisplayMode.Shaded] = "Shaded",
        [DisplayMode.Wireframe] = "Wireframe",
        [DisplayMode.Rendered] = "Rendered",
        [DisplayMode.Ghosted] = "Ghosted",
        [DisplayMode.XRay] = "X-Ray",
        [DisplayMode.Technical] = "Technical",
        [DisplayMode.Artistic] = "Artistic",
        [DisplayMode.Pen] = "Pen",
        [DisplayMode.Arctic] = "Arctic",
        [DisplayMode.Raytraced] = "Raytraced",
    };

    private static readonly IReadOnlyDictionary<string, DisplayMode> FromRhinoName =
        ToRhinoName.ToDictionary(kvp => kvp.Value, kvp => kvp.Key, StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyList<string> AllRhinoEnglishNames { get; } =
        ToRhinoName.Values.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList();

    public static string ToRhinoEnglishName(DisplayMode mode) =>
        ToRhinoName.TryGetValue(mode, out var name) ? name : "Shaded";

    public static bool IsCurrentDisplayMode(string? displayMode) =>
        string.Equals(displayMode, RhinoBridgeConstants.DisplayModeCurrent, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(displayMode, RhinoBridgeConstants.DisplayModeViewport, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Parses UI/API display mode string to enum. Handles Rhino English names and enum names.
    /// Returns null when "Current"/"viewport" (use active viewport mode).
    /// </summary>
    public static DisplayMode? Parse(string? displayMode)
    {
        if (string.IsNullOrWhiteSpace(displayMode) || IsCurrentDisplayMode(displayMode))
            return null;

        if (Enum.TryParse<DisplayMode>(displayMode, ignoreCase: true, out var parsed))
            return parsed;

        return FromRhinoName.TryGetValue(displayMode!, out var fromName) ? fromName : DisplayMode.Shaded;
    }

    /// <summary>
    /// Resolves stored display mode name for persistence (enum from Rhino English name).
    /// </summary>
    public static DisplayMode ResolveForStorage(string? displayModeName) =>
        Parse(displayModeName) ?? DisplayMode.Shaded;
}
