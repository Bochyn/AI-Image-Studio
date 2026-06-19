using RhinoImageStudio.Backend.Services;
using System.Text.Json;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Endpoints;

public static class RhinoBridgeEndpoints
{
    public static RouteGroupBuilder MapRhinoBridgeEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/rhino/status", (IRhinoBridgeService bridge) =>
            Results.Ok(new RhinoBridgeStatusResponse(bridge.IsConnected, bridge.LastSeenUtc)));

        api.MapGet("/rhino/display-modes", (IRhinoBridgeService bridge, CancellationToken ct) =>
            ExecuteBridgeQueryAsync(
                bridge,
                RhinoBridgeConstants.GetDisplayModes,
                json => Results.Ok(JsonSerializer.Deserialize<List<RhinoDisplayModeInfo>>(json, JsonOptions) ?? []),
                ct));

        api.MapGet("/rhino/viewports", (IRhinoBridgeService bridge, CancellationToken ct) =>
            ExecuteBridgeQueryAsync(
                bridge,
                RhinoBridgeConstants.GetViewports,
                json => Results.Ok(JsonSerializer.Deserialize<List<RhinoViewportInfo>>(json, JsonOptions) ?? []),
                ct));

        api.MapGet("/rhino/active-display-mode", (IRhinoBridgeService bridge, CancellationToken ct) =>
            ExecuteBridgeQueryAsync(
                bridge,
                RhinoBridgeConstants.GetActiveDisplayMode,
                json => Results.Ok(new RhinoActiveDisplayModeResponse(json.Trim('"'))),
                ct));

        api.MapPost("/rhino/capture", async (
            RhinoCaptureRequestDto request,
            IRhinoBridgeService bridge,
            CancellationToken ct) =>
        {
            if (!bridge.IsConnected)
                return Results.Problem("Rhino bridge is not connected.", statusCode: StatusCodes.Status503ServiceUnavailable);

            try
            {
                var captureId = await bridge.RequestCaptureAsync(
                    request.ProjectId,
                    request.Width,
                    request.Height,
                    request.DisplayMode,
                    ct);

                return Results.Ok(new RhinoCaptureResponse(captureId));
            }
            catch (TimeoutException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status504GatewayTimeout);
            }
            catch (InvalidOperationException ex)
            {
                return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
            }
        });

        api.MapGet("/rhino/bridge/next", async (
            string? clientId,
            HttpRequest request,
            IRhinoBridgeService bridge,
            CancellationToken ct) =>
        {
            try
            {
                var token = request.Headers[RhinoBridgeConstants.BridgeTokenHeader].FirstOrDefault();
                var item = await bridge.WaitForWorkAsync(clientId ?? "unknown", token, ct);
                return item is null ? Results.NoContent() : Results.Ok(item);
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        });

        api.MapPost("/rhino/bridge/{requestId:guid}/complete", async (
            Guid requestId,
            RhinoBridgeCompletion completion,
            HttpRequest request,
            IRhinoBridgeService bridge) =>
        {
            try
            {
                var token = request.Headers[RhinoBridgeConstants.BridgeTokenHeader].FirstOrDefault();
                await bridge.CompleteAsync(requestId, completion, token);
                return Results.NoContent();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Unauthorized();
            }
        });

        return api;
    }

    private static async Task<IResult> ExecuteBridgeQueryAsync(
        IRhinoBridgeService bridge,
        string workType,
        Func<string, IResult> onSuccess,
        CancellationToken ct)
    {
        if (!bridge.IsConnected)
            return Results.Problem("Rhino bridge is not connected.", statusCode: StatusCodes.Status503ServiceUnavailable);

        try
        {
            var json = await bridge.RequestQueryAsync(workType, ct);
            return onSuccess(json);
        }
        catch (TimeoutException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status504GatewayTimeout);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
}

public sealed record RhinoBridgeStatusResponse(bool Connected, DateTimeOffset? LastSeenUtc);

public sealed record RhinoCaptureRequestDto(Guid ProjectId, int Width, int Height, string DisplayMode);

public sealed record RhinoCaptureResponse(string CaptureId);

public sealed record RhinoActiveDisplayModeResponse(string Mode);
