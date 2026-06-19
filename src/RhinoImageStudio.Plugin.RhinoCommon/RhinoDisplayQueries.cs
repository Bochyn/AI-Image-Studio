using System.Text.Json;
using Rhino;
using Rhino.Display;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Plugin.RhinoCommon;

public static class RhinoDisplayQueries
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string GetDisplayModesJson()
    {
        var modes = DisplayModeDescription.GetDisplayModes()
            .Select(m => new RhinoDisplayModeInfo(m.EnglishName, m.Id.ToString()))
            .ToList();
        return JsonSerializer.Serialize(modes, JsonOptions);
    }

    public static string GetViewportsJson()
    {
        var doc = RhinoDoc.ActiveDoc;
        if (doc == null)
            return "[]";

        var viewports = doc.Views
            .Select(v =>
            {
                var viewport = v.ActiveViewport;
                var displayMode = viewport.DisplayMode?.EnglishName ?? "Shaded";
                return new RhinoViewportInfo(
                    viewport.Name,
                    v == doc.Views.ActiveView,
                    viewport.Size.Width,
                    viewport.Size.Height,
                    displayMode);
            })
            .ToList();
        return JsonSerializer.Serialize(viewports, JsonOptions);
    }

    public static string GetActiveDisplayModeJson()
    {
        var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
        var modeName = view?.ActiveViewport.DisplayMode?.EnglishName ?? "Shaded";
        return JsonSerializer.Serialize(modeName, JsonOptions);
    }
}
