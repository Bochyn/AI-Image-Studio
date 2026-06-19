using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Backend.Validation;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;
using Xunit;

namespace RhinoImageStudio.Backend.Tests;

public class GenerateRequestValidatorTests
{
    [Fact]
    public void Rejects_masks_for_fal_routed_openai_model()
    {
        var request = new GenerateRequest(
            ProjectId: Guid.NewGuid(),
            Prompt: "test",
            Model: FalModels.GptImage2Edit,
            MaskLayers: [new MaskLayerData("mask", "edit region")]);

        var error = GenerateRequestValidator.Validate(request);
        Assert.NotNull(error);
        Assert.Contains("not supported", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Allows_up_to_flash_reference_limit()
    {
        var refs = Enumerable.Range(0, 14).Select(_ => Guid.NewGuid()).ToList();
        var request = new GenerateRequest(
            ProjectId: Guid.NewGuid(),
            Prompt: "test",
            Model: GeminiModels.NanoBanana,
            ReferenceImageIds: refs);

        Assert.Null(GenerateRequestValidator.Validate(request));
    }

    [Fact]
    public void Rejects_excess_references_for_flash()
    {
        var refs = Enumerable.Range(0, 15).Select(_ => Guid.NewGuid()).ToList();
        var request = new GenerateRequest(
            ProjectId: Guid.NewGuid(),
            Prompt: "test",
            Model: GeminiModels.NanoBanana,
            ReferenceImageIds: refs);

        var error = GenerateRequestValidator.Validate(request);
        Assert.NotNull(error);
        Assert.Contains("14", error);
    }
}
