using RhinoImageStudio.Backend.Infrastructure;
using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Endpoints;

public static class ConfigEndpoints
{
    public static RouteGroupBuilder MapConfigEndpoints(this RouteGroupBuilder api, int port)
    {
        api.MapGet("/config", async (IConfigService config, CancellationToken ct) =>
            Results.Ok(await config.GetConfigAsync(port, ct)));

        api.MapPost("/config/gemini-api-key", async (SetGeminiApiKeyRequest request, IConfigService config, CancellationToken ct) =>
        {
            try
            {
                await config.SetGeminiApiKeyAsync(request.ApiKey, ct);
                return Results.Ok(new { success = true });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireLocalApiToken();

        api.MapPost("/config/fal-api-key", async (SetFalApiKeyRequest request, IConfigService config, CancellationToken ct) =>
        {
            try
            {
                await config.SetFalApiKeyAsync(request.ApiKey, ct);
                return Results.Ok(new { success = true });
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        }).RequireLocalApiToken();

        api.MapPost("/config/verify-gemini-key", async (IConfigService config, CancellationToken ct) =>
        {
            var result = await config.VerifyGeminiKeyAsync(ct);
            return Results.Ok(new { valid = result.Valid, error = result.Error });
        }).RequireLocalApiToken();

        api.MapDelete("/config/secrets/gemini", async (IConfigService config, CancellationToken ct) =>
        {
            await config.DeleteGeminiApiKeyAsync(ct);
            return Results.NoContent();
        }).RequireLocalApiToken();

        api.MapDelete("/config/secrets/fal", async (IConfigService config, CancellationToken ct) =>
        {
            await config.DeleteFalApiKeyAsync(ct);
            return Results.NoContent();
        }).RequireLocalApiToken();

        return api;
    }
}
