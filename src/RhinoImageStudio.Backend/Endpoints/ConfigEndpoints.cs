using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Endpoints;

public static class ConfigEndpoints
{
    public static RouteGroupBuilder MapConfigEndpoints(this RouteGroupBuilder api, int port)
    {
        api.MapGet("/config", async (ISecretStorage secrets, IStorageService storage, CancellationToken ct) =>
        {
            var hasFalApiKey = await secrets.HasSecretAsync(SecretKeyNames.FalApiKey);
            var hasGeminiApiKey = await secrets.HasSecretAsync(SecretKeyNames.GeminiApiKey);
            return Results.Ok(new ConfigDto(hasFalApiKey, hasGeminiApiKey, storage.BasePath, port, "gemini"));
        });

        api.MapPost("/config/api-key", async (SetApiKeyRequest request, ISecretStorage secrets, CancellationToken ct) =>
        {
            await secrets.SetSecretAsync(SecretKeyNames.FalApiKey, request.ApiKey);
            return Results.Ok(new { success = true });
        });

        api.MapPost("/config/gemini-api-key", async (SetGeminiApiKeyRequest request, ISecretStorage secrets, CancellationToken ct) =>
        {
            await secrets.SetSecretAsync(SecretKeyNames.GeminiApiKey, request.ApiKey);
            return Results.Ok(new { success = true });
        });

        api.MapPost("/config/fal-api-key", async (SetFalApiKeyRequest request, ISecretStorage secrets, CancellationToken ct) =>
        {
            await secrets.SetSecretAsync(SecretKeyNames.FalApiKey, request.ApiKey);
            return Results.Ok(new { success = true });
        });

        api.MapPost("/config/verify-gemini-key", async (ISecretStorage secrets, IHttpClientFactory httpClientFactory, CancellationToken ct) =>
        {
            var apiKey = await secrets.GetSecretAsync(SecretKeyNames.GeminiApiKey);
            if (string.IsNullOrEmpty(apiKey))
                return Results.Ok(new { valid = false, error = "No Gemini API key configured" });

            try
            {
                using var httpClient = httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                var response = await httpClient.GetAsync(
                    $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}", ct);

                if (response.IsSuccessStatusCode)
                    return Results.Ok(new { valid = true, error = (string?)null });

                var body = await response.Content.ReadAsStringAsync(ct);
                return Results.Ok(new { valid = false, error = $"API returned {response.StatusCode}: {body}" });
            }
            catch (TaskCanceledException)
            {
                return Results.Ok(new { valid = false, error = "Connection timed out (5s)" });
            }
            catch (HttpRequestException ex)
            {
                return Results.Ok(new { valid = false, error = $"Connection failed: {ex.Message}" });
            }
        });

        api.MapDelete("/config/secrets/gemini", async (ISecretStorage secrets) =>
        {
            await secrets.DeleteSecretAsync(SecretKeyNames.GeminiApiKey);
            return Results.NoContent();
        });

        return api;
    }
}
