using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;
using Xunit;

namespace RhinoImageStudio.Backend.Tests;

public class BridgeTokenServiceTests
{
    [Fact]
    public void Validate_accepts_matching_token()
    {
        var service = new BridgeTokenService();
        Assert.True(service.Validate(service.Token));
    }

    [Fact]
    public void Validate_rejects_missing_or_wrong_token()
    {
        var service = new BridgeTokenService();
        Assert.False(service.Validate(null));
        Assert.False(service.Validate(string.Empty));
        Assert.False(service.Validate("not-the-bridge-token"));
    }
}

public class RhinoBridgeServiceTests
{
    [Fact]
    public async Task WaitForWorkAsync_rejects_invalid_token()
    {
        var service = new RhinoBridgeService(new BridgeTokenService());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.WaitForWorkAsync("test-client", "invalid-token", CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAsync_rejects_invalid_token()
    {
        var service = new RhinoBridgeService(new BridgeTokenService());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CompleteAsync(Guid.NewGuid(), new RhinoBridgeCompletion(true, null, null), "invalid-token"));
    }
}
