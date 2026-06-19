using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Constants;
using Xunit;

namespace RhinoImageStudio.Backend.Tests;

public class ModelCapabilityLimitsTests
{
    [Theory]
    [InlineData(GeminiModels.NanoBanana, 14, 16, 2)]
    [InlineData(GeminiModels.NanoBananaPro, 11, 14, 8)]
    public void Gemini_limits_match_ui_contract(string modelId, int maxRefs, int maxTotal, int maxMasks)
    {
        var limits = ModelCapabilityLimits.TryGetGeminiLimits(modelId);
        Assert.NotNull(limits);
        Assert.Equal(maxRefs, limits.MaxReferences);
        Assert.Equal(maxTotal, limits.MaxTotalImages);
        Assert.Equal(maxMasks, limits.MaxMaskLayers);
    }

    [Fact]
    public void Fal_models_do_not_support_masks()
    {
        Assert.False(ModelCapabilityLimits.SupportsMasks(FalModels.GptImage2Edit));
        Assert.False(ModelCapabilityLimits.SupportsMasks(FalModels.SeedreamV5LiteEdit));
    }

    [Theory]
    [InlineData(FalModels.SeedreamV5LiteEdit, 9)]
    [InlineData(FalModels.GptImage15Edit, 4)]
    [InlineData(FalModels.GptImage2Edit, 4)]
    public void Fal_reference_limits_match_ui_contract(string modelId, int maxRefs)
    {
        Assert.Equal(maxRefs, ModelCapabilityLimits.GetMaxReferences(modelId));
    }

    [Theory]
    [InlineData(GeminiModels.NanoBanana, true)]
    [InlineData(GeminiModels.NanoBananaPro, true)]
    [InlineData(FalModels.GptImage2Edit, false)]
    public void SupportsImageSize_follows_gemini_models_only(string modelId, bool expected)
    {
        Assert.Equal(expected, ModelCapabilityLimits.SupportsImageSize(modelId));
    }
}
