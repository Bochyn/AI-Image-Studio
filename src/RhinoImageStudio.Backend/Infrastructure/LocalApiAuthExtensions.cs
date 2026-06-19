using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Constants;

namespace RhinoImageStudio.Backend.Infrastructure;

/// <summary>
/// Requires the localhost shared token on mutating API endpoints.
/// Same token as the Rhino bridge poll/complete flow (<see cref="RhinoBridgeConstants.BridgeTokenHeader"/>).
/// </summary>
public static class LocalApiAuthExtensions
{
    public static RouteHandlerBuilder RequireLocalApiToken(this RouteHandlerBuilder builder)
    {
        return builder.AddEndpointFilter(async (context, next) =>
        {
            var tokenService = context.HttpContext.RequestServices.GetRequiredService<IBridgeTokenService>();
            var token = context.HttpContext.Request.Headers[RhinoBridgeConstants.BridgeTokenHeader].FirstOrDefault();

            if (!tokenService.Validate(token))
                return Results.Unauthorized();

            return await next(context);
        });
    }
}
