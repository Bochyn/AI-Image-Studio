using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Services;

public interface IConfigService
{
    Task<ConfigDto> GetConfigAsync(int port, CancellationToken ct = default);
    Task SetFalApiKeyAsync(string apiKey, CancellationToken ct = default);
    Task SetGeminiApiKeyAsync(string apiKey, CancellationToken ct = default);
    Task DeleteGeminiApiKeyAsync(CancellationToken ct = default);
    Task DeleteFalApiKeyAsync(CancellationToken ct = default);
    Task<GeminiKeyVerificationResult> VerifyGeminiKeyAsync(CancellationToken ct = default);
}

public record GeminiKeyVerificationResult(bool Valid, string? Error);

public sealed class ConfigService : IConfigService
{
    private readonly ISecretStorage _secrets;
    private readonly IStorageService _storage;
    private readonly IGeminiClient _geminiClient;

    public ConfigService(ISecretStorage secrets, IStorageService storage, IGeminiClient geminiClient)
    {
        _secrets = secrets;
        _storage = storage;
        _geminiClient = geminiClient;
    }

    public async Task<ConfigDto> GetConfigAsync(int port, CancellationToken ct = default)
    {
        var hasFalApiKey = await _secrets.HasSecretAsync(SecretKeyNames.FalApiKey);
        var hasGeminiApiKey = await _secrets.HasSecretAsync(SecretKeyNames.GeminiApiKey);
        return new ConfigDto(hasFalApiKey, hasGeminiApiKey, _storage.BasePath, port, "gemini");
    }

    public async Task SetFalApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        ValidateApiKey(apiKey, "fal.ai API key");
        await _secrets.SetSecretAsync(SecretKeyNames.FalApiKey, apiKey.Trim());
    }

    public async Task SetGeminiApiKeyAsync(string apiKey, CancellationToken ct = default)
    {
        ValidateApiKey(apiKey, "Gemini API key");
        await _secrets.SetSecretAsync(SecretKeyNames.GeminiApiKey, apiKey.Trim());
    }

    public Task DeleteGeminiApiKeyAsync(CancellationToken ct = default) =>
        _secrets.DeleteSecretAsync(SecretKeyNames.GeminiApiKey);

    public Task DeleteFalApiKeyAsync(CancellationToken ct = default) =>
        _secrets.DeleteSecretAsync(SecretKeyNames.FalApiKey);

    public Task<GeminiKeyVerificationResult> VerifyGeminiKeyAsync(CancellationToken ct = default) =>
        _geminiClient.VerifyApiKeyAsync(ct);

    private static void ValidateApiKey(string apiKey, string label)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException($"{label} cannot be empty.", nameof(apiKey));
    }
}
