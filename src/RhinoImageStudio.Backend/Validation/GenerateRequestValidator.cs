using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Validation;

public static class GenerateRequestValidator
{
    public static string? Validate(GenerateRequest request)
    {
        var selectedModel = request.Model ?? GeminiModels.NanoBanana;
        var maxReferences = ModelCapabilityLimits.GetMaxReferences(selectedModel);

        if (request.ReferenceImageIds != null && request.ReferenceImageIds.Count > maxReferences)
            return $"Maximum {maxReferences} reference images allowed for model {selectedModel}";

        if (request.MaskPayload != null)
            return ValidateMaskPayload(request, request.MaskPayload);

        if (request.MaskLayers != null)
            return ValidateMaskLayers(request, request.MaskLayers);

        return null;
    }

    private static string? ValidateMaskPayload(GenerateRequest request, MaskPayloadData payload)
    {
        if (!ModelCapabilityLimits.SupportsMasks(request.Model))
            return "Mask layers are not supported for the selected model";

        if (string.IsNullOrWhiteSpace(payload.OverlayImageBase64))
            return "MaskPayload must have OverlayImageBase64";
        try { Convert.FromBase64String(payload.OverlayImageBase64); }
        catch (FormatException) { return "MaskPayload contains invalid base64 overlay image"; }

        if (payload.Layers == null || payload.Layers.Count == 0)
            return "MaskPayload must have at least one layer";

        var maxMaskLayers = ModelCapabilityLimits.GetMaxMaskLayers(request.Model);
        if (payload.Layers.Count > maxMaskLayers)
            return $"Maximum {maxMaskLayers} mask layers allowed";

        for (var i = 0; i < payload.Layers.Count; i++)
        {
            var layer = payload.Layers[i];
            if (string.IsNullOrWhiteSpace(layer.Instruction))
                return $"Mask layer {i + 1} must have an instruction";
            if (string.IsNullOrWhiteSpace(layer.ColorName))
                return $"Mask layer {i + 1} must have a color name";
        }

        return ValidateMaskModelLimits(request, hasSource: true, maskCount: 0, overlayRefCount: 2);
    }

    private static string? ValidateMaskLayers(GenerateRequest request, IReadOnlyList<MaskLayerData> maskLayers)
    {
        if (!ModelCapabilityLimits.SupportsMasks(request.Model))
            return "Mask layers are not supported for the selected model";

        var maxMaskLayers = ModelCapabilityLimits.GetMaxMaskLayers(request.Model);
        if (maskLayers.Count > maxMaskLayers)
            return $"Maximum {maxMaskLayers} mask layers allowed";

        for (var i = 0; i < maskLayers.Count; i++)
        {
            var mask = maskLayers[i];
            if (string.IsNullOrWhiteSpace(mask.Instruction))
                return $"Mask layer {i + 1} must have an instruction";
            if (string.IsNullOrWhiteSpace(mask.MaskImageBase64))
                return $"Mask layer {i + 1} must have image data (MaskImageBase64)";
            try { Convert.FromBase64String(mask.MaskImageBase64); }
            catch (FormatException) { return $"Mask layer {i + 1} contains invalid base64 image data"; }
        }

        var hasSource = request.SourceCaptureId.HasValue || request.ParentGenerationId.HasValue;
        return ValidateMaskModelLimits(request, hasSource, maskLayers.Count, overlayRefCount: 0);
    }

    private static string? ValidateMaskModelLimits(
        GenerateRequest request,
        bool hasSource,
        int maskCount,
        int overlayRefCount)
    {
        var selectedModel = request.Model ?? GeminiModels.NanoBanana;

        if (FalModels.IsFalRouted(selectedModel))
            return "Mask layers are not supported for fal.ai models";

        var refCount = request.ReferenceImageIds?.Count ?? 0;
        var totalImages = overlayRefCount > 0
            ? overlayRefCount + refCount
            : (hasSource ? 1 : 0) + refCount + maskCount;

        var maxTotalImages = ModelCapabilityLimits.GetMaxTotalImages(selectedModel);

        return totalImages > maxTotalImages
            ? $"Total image count ({totalImages}) exceeds limit ({maxTotalImages}) for model {selectedModel}."
            : null;
    }
}
