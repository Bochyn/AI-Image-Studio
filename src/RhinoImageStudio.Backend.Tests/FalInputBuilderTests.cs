using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;
using Xunit;

namespace RhinoImageStudio.Backend.Tests;

public class FalInputBuilderTests
{
    [Fact]
    public void Build_uses_prompt_argument_for_fal_payload()
    {
        var request = new GenerateRequest(
            ProjectId: Guid.NewGuid(),
            Prompt: "original",
            NumImages: 1);

        var input = FalInputBuilder.Build(
            FalModels.GptImage2Edit,
            request,
            "augmented prompt",
            null,
            null);

        Assert.Equal("augmented prompt", input["prompt"]);
    }

    [Fact]
    public void Build_legacy_nano_banana_includes_reference_urls()
    {
        var request = new GenerateRequest(ProjectId: Guid.NewGuid(), Prompt: "edit");
        var input = FalInputBuilder.Build(
            FalModels.NanoBananaEdit,
            request,
            "prompt",
            "https://example.com/source.png",
            ["https://example.com/ref1.png"]);

        var urls = Assert.IsType<string[]>(input["image_urls"]);
        Assert.Equal(2, urls.Length);
        Assert.Equal("https://example.com/source.png", urls[0]);
    }
}
