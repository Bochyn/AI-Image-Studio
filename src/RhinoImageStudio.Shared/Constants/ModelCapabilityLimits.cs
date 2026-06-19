namespace RhinoImageStudio.Shared.Constants;

/// <summary>
/// Backend mirror of model limits defined in UI models.ts.
/// Keep in sync when changing maxReferences / maxTotalImages / maxMaskLayers there.
/// </summary>
public static class ModelCapabilityLimits
{
    public sealed record Limits(
        int MaxReferences,
        int MaxMaskLayers,
        int MaxTotalImages,
        bool SupportsMasks,
        bool SupportsImageSize = false);

    private static readonly Dictionary<string, Limits> GeminiLimits = new(StringComparer.Ordinal)
    {
        [GeminiModels.NanoBanana] = new(14, 2, 16, SupportsMasks: true, SupportsImageSize: true),
        [GeminiModels.NanoBananaPro] = new(11, 8, 14, SupportsMasks: true, SupportsImageSize: true),
    };

    private static readonly Dictionary<string, Limits> FalLimits = new(StringComparer.Ordinal)
    {
        [FalModels.SeedreamV5LiteEdit] = new(9, 0, 0, SupportsMasks: false),
        [FalModels.GptImage15Edit] = new(4, 0, 0, SupportsMasks: false),
        [FalModels.GptImage2Edit] = new(4, 0, 0, SupportsMasks: false),
        [FalModels.NanoBananaEdit] = new(4, 0, 0, SupportsMasks: false),
    };

    public static Limits? TryGetLimits(string? modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            return GeminiLimits[GeminiModels.NanoBanana];

        var normalized = modelId!.Trim();
        if (GeminiLimits.TryGetValue(normalized, out var gemini))
            return gemini;
        if (FalLimits.TryGetValue(normalized, out var fal))
            return fal;

        return null;
    }

    public static Limits? TryGetGeminiLimits(string? modelId)
    {
        if (string.IsNullOrWhiteSpace(modelId))
            return GeminiLimits[GeminiModels.NanoBanana];

        var normalized = modelId!.Trim();
        return GeminiLimits.TryGetValue(normalized, out var limits) ? limits : null;
    }

    public static int GetMaxReferences(string? modelId) =>
        TryGetLimits(modelId)?.MaxReferences ?? 4;

    public static int GetMaxMaskLayers(string? modelId) =>
        TryGetGeminiLimits(modelId)?.MaxMaskLayers ?? 8;

    public static int GetMaxTotalImages(string? modelId) =>
        TryGetGeminiLimits(modelId)?.MaxTotalImages ?? 3;

    public static bool SupportsMasks(string? modelId)
    {
        if (FalModels.IsFalRouted(modelId ?? string.Empty))
            return false;

        return TryGetGeminiLimits(modelId)?.SupportsMasks ?? false;
    }

    public static bool SupportsImageSize(string? modelId) =>
        TryGetGeminiLimits(modelId)?.SupportsImageSize ?? false;
}
