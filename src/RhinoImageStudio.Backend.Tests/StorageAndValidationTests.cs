using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Backend.Validation;
using RhinoImageStudio.Shared.Contracts;
using Xunit;

namespace RhinoImageStudio.Backend.Tests;

public class StorageServicePathTests
{
    [Fact]
    public void GetAbsolutePath_rejects_sibling_directory_prefix_bypass()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ris-storage-test-{Guid.NewGuid():N}");
        var sibling = $"{tempRoot}-evil";
        Directory.CreateDirectory(tempRoot);
        Directory.CreateDirectory(sibling);

        try
        {
            var storage = new StorageService(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<StorageService>.Instance,
                Microsoft.Extensions.Options.Options.Create(new RhinoImageStudio.Backend.Options.StorageOptions
                {
                    BasePath = tempRoot
                }));

            Assert.Throws<UnauthorizedAccessException>(() =>
                storage.GetAbsolutePath($"../{Path.GetFileName(sibling)}/secret.png"));
        }
        finally
        {
            Directory.Delete(sibling, true);
            Directory.Delete(tempRoot, true);
        }
    }

    [Fact]
    public void GetAbsolutePath_allows_file_inside_root()
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), $"ris-storage-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempRoot);

        try
        {
            var storage = new StorageService(
                Microsoft.Extensions.Logging.Abstractions.NullLogger<StorageService>.Instance,
                Microsoft.Extensions.Options.Options.Create(new RhinoImageStudio.Backend.Options.StorageOptions
                {
                    BasePath = tempRoot
                }));

            var path = storage.GetAbsolutePath("captures/test.png");
            Assert.StartsWith(tempRoot, path, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempRoot, true);
        }
    }
}

public class JobRequestValidatorsTests
{
    [Fact]
    public void Validate_refine_requires_prompt()
    {
        var error = JobRequestValidators.Validate(new RefineRequest(Guid.NewGuid(), Guid.NewGuid(), " "));
        Assert.NotNull(error);
    }

    [Fact]
    public void Validate_multi_angle_requires_single_source()
    {
        var error = JobRequestValidators.Validate(new MultiAngleRequest(
            Guid.NewGuid(),
            SourceGenerationId: Guid.NewGuid(),
            SourceCaptureId: Guid.NewGuid()));
        Assert.Contains("only one", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Validate_upscale_rejects_invalid_factor()
    {
        var error = JobRequestValidators.Validate(new UpscaleRequest(
            Guid.NewGuid(),
            Guid.NewGuid(),
            UpscaleFactor: 10));
        Assert.NotNull(error);
    }
}
