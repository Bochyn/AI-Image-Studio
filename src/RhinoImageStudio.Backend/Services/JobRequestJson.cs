using System.Text.Json;
using System.Text.Json.Serialization;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Shared JSON serializer options for job request persistence and deserialization.
/// </summary>
public static class JobRequestJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);

    public static T Deserialize<T>(string json) =>
        JsonSerializer.Deserialize<T>(json, Options)
        ?? throw new InvalidOperationException($"Invalid {typeof(T).Name} JSON");
}
