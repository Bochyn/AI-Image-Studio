using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Builds fal.ai input payloads per model.
/// </summary>
public static class FalInputBuilder
{
    private delegate Dictionary<string, object> FalPayloadBuilder(
        GenerateRequest request,
        string prompt,
        string? sourceImageUrl,
        IReadOnlyList<string>? referenceImageUrls);

    private static readonly IReadOnlyDictionary<string, FalPayloadBuilder> Builders =
        new Dictionary<string, FalPayloadBuilder>(StringComparer.Ordinal)
        {
            [FalModels.SeedreamV5LiteEdit] = BuildSeedream,
            [FalModels.GptImage15Edit] = BuildGptImage15,
            [FalModels.GptImage2Edit] = BuildGptImage2,
        };

    public static Dictionary<string, object> Build(
        string falModelId,
        GenerateRequest request,
        string prompt,
        string? sourceImageUrl,
        IReadOnlyList<string>? referenceImageUrls)
    {
        if (Builders.TryGetValue(falModelId, out var builder))
            return builder(request, prompt, sourceImageUrl, referenceImageUrls);

        return BuildLegacyNanoBanana(request, prompt, sourceImageUrl, referenceImageUrls);
    }

    private static Dictionary<string, object> BuildSeedream(
        GenerateRequest request,
        string prompt,
        string? sourceImageUrl,
        IReadOnlyList<string>? referenceImageUrls)
    {
        var input = new Dictionary<string, object>
        {
            ["prompt"] = prompt,
            ["num_images"] = request.NumImages,
            ["image_size"] = request.AspectRatio ?? "auto_2K",
        };
        AddImageUrls(input, sourceImageUrl, referenceImageUrls);
        return input;
    }

    private static Dictionary<string, object> BuildGptImage15(
        GenerateRequest request,
        string prompt,
        string? sourceImageUrl,
        IReadOnlyList<string>? referenceImageUrls)
    {
        var input = new Dictionary<string, object>
        {
            ["prompt"] = prompt,
            ["num_images"] = request.NumImages,
            ["image_size"] = request.AspectRatio ?? "auto",
            ["output_format"] = request.OutputFormat ?? "png",
        };
        if (request.Quality != null) input["quality"] = request.Quality;
        if (request.InputFidelity != null) input["input_fidelity"] = request.InputFidelity;
        AddImageUrls(input, sourceImageUrl, referenceImageUrls);
        return input;
    }

    private static Dictionary<string, object> BuildGptImage2(
        GenerateRequest request,
        string prompt,
        string? sourceImageUrl,
        IReadOnlyList<string>? referenceImageUrls)
    {
        var input = new Dictionary<string, object>
        {
            ["prompt"] = prompt,
            ["num_images"] = Math.Clamp(request.NumImages, 1, 4),
            ["image_size"] = request.AspectRatio ?? "auto",
            ["quality"] = request.Quality ?? "high",
            ["output_format"] = request.OutputFormat?.ToLowerInvariant() ?? "png",
        };
        AddImageUrls(input, sourceImageUrl, referenceImageUrls);
        return input;
    }

    private static Dictionary<string, object> BuildLegacyNanoBanana(
        GenerateRequest request,
        string prompt,
        string? sourceImageUrl,
        IReadOnlyList<string>? referenceImageUrls)
    {
        var legacy = new Dictionary<string, object>
        {
            ["prompt"] = prompt,
            ["num_images"] = request.NumImages,
            ["aspect_ratio"] = request.AspectRatio ?? "1:1",
            ["resolution"] = request.Resolution ?? "1K",
            ["output_format"] = request.OutputFormat?.ToLowerInvariant() ?? "png"
        };
        AddImageUrls(legacy, sourceImageUrl, referenceImageUrls);
        return legacy;
    }

    private static void AddImageUrls(
        Dictionary<string, object> input,
        string? sourceImageUrl,
        IReadOnlyList<string>? referenceImageUrls)
    {
        var imageUrls = new List<string>();
        if (sourceImageUrl != null) imageUrls.Add(sourceImageUrl);
        if (referenceImageUrls != null) imageUrls.AddRange(referenceImageUrls);
        if (imageUrls.Count > 0)
            input["image_urls"] = imageUrls.ToArray();
    }
}
