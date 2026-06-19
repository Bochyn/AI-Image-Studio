using System.Text.Json;
using System.Text.Json.Serialization;

namespace RhinoImageStudio.Backend.Infrastructure;

/// <summary>
/// Shared JSON serializer options for API responses.
/// </summary>
public static class ApiJsonOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
