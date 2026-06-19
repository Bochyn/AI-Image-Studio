using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Backend.Infrastructure;
using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Backend.Validation;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Models;
using RhinoImageStudio.Shared.Utilities;

namespace RhinoImageStudio.Backend.Endpoints;

public static class CaptureEndpoints
{
    private const int MaxCaptureUploadBytes = 50 * 1024 * 1024;

    public static RouteGroupBuilder MapCaptureEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/projects/{projectId:guid}/captures", async (Guid projectId, AppDbContext db, CancellationToken ct) =>
        {
            var captures = await db.Captures
                .AsNoTracking()
                .Where(c => c.ProjectId == projectId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(captures.Select(c => c.ToDto()).ToList());
        });

        api.MapPost("/captures", async (HttpRequest httpRequest, AppDbContext db, IStorageService storage, CancellationToken ct) =>
        {
            var form = await httpRequest.ReadFormAsync(ct);
            var file = form.Files.GetFile("image");
            var projectIdStr = form["projectId"].ToString();
            var widthStr = form["width"].ToString();
            var heightStr = form["height"].ToString();
            var displayModeStr = form["displayMode"].ToString();
            var viewName = form["viewName"].ToString();

            if (file is null || !Guid.TryParse(projectIdStr, out var projectId))
                return Results.BadRequest("Missing image or projectId");

            if (file.Length > MaxCaptureUploadBytes)
                return Results.BadRequest("File too large, max 50MB");

            var projectError = await ProjectValidator.EnsureExistsAsync(db, projectId, ct);
            if (projectError != null)
                return Results.BadRequest(projectError);

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var imageData = ms.ToArray();

            var capture = new Capture
            {
                ProjectId = projectId,
                Width = int.TryParse(widthStr, out var w) ? w : 1024,
                Height = int.TryParse(heightStr, out var h) ? h : 1024,
                DisplayMode = DisplayModeMapping.ResolveForStorage(displayModeStr),
                ViewName = string.IsNullOrEmpty(viewName) ? null : viewName
            };

            capture.FilePath = await storage.SaveCaptureAsync(capture.Id, imageData, cancellationToken: ct);
            capture.ThumbnailPath = await storage.SaveThumbnailAsync(capture.Id, imageData, ct);

            db.Captures.Add(capture);

            var project = await db.Projects.FindAsync(new object[] { projectId }, ct);
            if (project != null)
                project.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/captures/{capture.Id}", capture.ToDto());
        }).RequireLocalApiToken();

        api.MapDelete("/captures/{id:guid}", async (Guid id, AppDbContext db, IStorageService storage, CancellationToken ct) =>
        {
            var capture = await db.Captures.FindAsync(new object[] { id }, ct);
            if (capture is null) return Results.NotFound();

            await storage.DeleteFileAsync(capture.FilePath, ct);
            if (capture.ThumbnailPath != null)
                await storage.DeleteFileAsync(capture.ThumbnailPath, ct);

            db.Captures.Remove(capture);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        }).RequireLocalApiToken();

        api.MapGet("/projects/{projectId:guid}/references", async (Guid projectId, AppDbContext db, CancellationToken ct) =>
        {
            var references = await db.ReferenceImages
                .AsNoTracking()
                .Where(r => r.ProjectId == projectId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(references.Select(r => r.ToDto()).ToList());
        });

        api.MapPost("/projects/{projectId:guid}/references", async (
            Guid projectId,
            HttpRequest httpRequest,
            AppDbContext db,
            IStorageService storage,
            CancellationToken ct) =>
        {
            var projectError = await ProjectValidator.EnsureExistsAsync(db, projectId, ct);
            if (projectError != null)
                return Results.BadRequest(projectError);

            var form = await httpRequest.ReadFormAsync(ct);
            var file = form.Files.GetFile("image");

            if (file is null)
                return Results.BadRequest("Missing image file");

            if (file.Length > 10 * 1024 * 1024)
                return Results.BadRequest("File too large, max 10MB");

            if (!file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                return Results.BadRequest("File must be an image");

            var existingCount = await db.ReferenceImages.CountAsync(r => r.ProjectId == projectId, ct);
            if (existingCount >= 4)
                return Results.BadRequest("Maximum 4 reference images per project");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var imageData = ms.ToArray();

            var reference = new ReferenceImage
            {
                ProjectId = projectId,
                OriginalFileName = Path.GetFileName(file.FileName) ?? "reference.png"
            };

            reference.FilePath = await storage.SaveReferenceAsync(reference.Id, imageData, cancellationToken: ct);
            reference.ThumbnailPath = await storage.SaveThumbnailAsync(reference.Id, imageData, ct);

            db.ReferenceImages.Add(reference);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/references/{reference.Id}", reference.ToDto());
        }).RequireLocalApiToken();

        api.MapDelete("/references/{id:guid}", async (Guid id, AppDbContext db, IStorageService storage, CancellationToken ct) =>
        {
            var reference = await db.ReferenceImages.FindAsync(new object[] { id }, ct);
            if (reference is null) return Results.NotFound();

            await storage.DeleteFileAsync(reference.FilePath, ct);
            if (reference.ThumbnailPath != null)
                await storage.DeleteFileAsync(reference.ThumbnailPath, ct);

            db.ReferenceImages.Remove(reference);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        }).RequireLocalApiToken();

        return api;
    }
}
