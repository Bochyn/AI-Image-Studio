using RhinoImageStudio.Backend.Services;

namespace RhinoImageStudio.Backend.Endpoints;

public static class ImageEndpoints
{
    public static WebApplication MapImageEndpoints(this WebApplication app)
    {
        app.MapGet("/images/{**path}", async (string path, IStorageService storage, CancellationToken ct) =>
        {
            try
            {
                var data = await storage.ReadFileAsync(path, ct);
                var contentType = path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png"
                    : path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                      path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ? "image/jpeg"
                    : path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ? "image/webp"
                    : "application/octet-stream";
                return Results.File(data, contentType);
            }
            catch (FileNotFoundException)
            {
                return Results.NotFound();
            }
            catch (UnauthorizedAccessException)
            {
                return Results.Forbid();
            }
        });

        return app;
    }
}
