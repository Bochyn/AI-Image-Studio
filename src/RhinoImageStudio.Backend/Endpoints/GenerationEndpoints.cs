using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Backend.Infrastructure;
using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Backend.Services.Generations;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Endpoints;

public static class GenerationEndpoints
{
    public static RouteGroupBuilder MapGenerationEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/projects/{projectId:guid}/generations/archived", async (Guid projectId, AppDbContext db, CancellationToken ct) =>
        {
            var archived = await db.Generations
                .AsNoTracking()
                .Where(g => g.ProjectId == projectId && g.IsArchived)
                .OrderByDescending(g => g.ArchivedAt)
                .ToListAsync(ct);

            return Results.Ok(archived.Select(g => g.ToDto()).ToList());
        });

        api.MapGet("/projects/{projectId:guid}/generations", async (Guid projectId, AppDbContext db, CancellationToken ct) =>
        {
            var generations = await db.Generations
                .AsNoTracking()
                .Where(g => g.ProjectId == projectId && !g.IsArchived)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(generations.Select(g => g.ToDto()).ToList());
        });

        api.MapGet("/generations", async (AppDbContext db, int? limit, int? offset, CancellationToken ct) =>
        {
            var query = db.Generations
                .AsNoTracking()
                .Include(g => g.Project)
                .Where(g => !g.IsArchived)
                .OrderByDescending(g => g.CreatedAt);

            var total = await query.CountAsync(ct);
            var generations = await query
                .Skip(offset ?? 0)
                .Take(limit ?? 50)
                .ToListAsync(ct);

            return Results.Ok(new { generations = generations.Select(g => g.ToDto()).ToList(), total });
        });

        api.MapGet("/generations/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var generation = await db.Generations
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id, ct);

            return generation is null ? Results.NotFound() : Results.Ok(generation.ToDto());
        });

        api.MapGet("/generations/{id:guid}/debug", async (
            Guid id,
            GenerationDebugService debugService,
            AppDbContext db,
            IHostEnvironment env,
            CancellationToken ct) =>
        {
            if (!env.IsDevelopment())
                return Results.NotFound();

            var response = await debugService.BuildDebugResponseAsync(db, id, ct);
            return response is null
                ? Results.NotFound(new { error = "No job found for this generation" })
                : Results.Json(response, ApiJsonOptions.Default);
        });

        api.MapGet("/generations/{id:guid}/masks", async (
            Guid id,
            GenerationMaskService maskService,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var masks = await maskService.GetMasksAsync(db, id, ct);
            return Results.Json(masks, ApiJsonOptions.Default);
        });

        api.MapGet("/generations/{id:guid}/masks/overlay", async (
            Guid id,
            GenerationMaskService maskService,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var bytes = await maskService.GetOverlayImageAsync(db, id, ct);
            return bytes is null ? Results.NotFound() : Results.Bytes(bytes, "image/png");
        });

        api.MapGet("/generations/{id:guid}/masks/{index:int}/image", async (
            Guid id,
            int index,
            GenerationMaskService maskService,
            AppDbContext db,
            CancellationToken ct) =>
        {
            var bytes = await maskService.GetMaskLayerImageAsync(db, id, index, ct);
            return bytes is null ? Results.NotFound() : Results.Bytes(bytes, "image/png");
        });

        api.MapDelete("/generations/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var generation = await db.Generations.FindAsync(new object[] { id }, ct);
            if (generation is null) return Results.NotFound();

            generation.IsArchived = true;
            generation.ArchivedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { archived = true, id = generation.Id });
        }).RequireLocalApiToken();

        api.MapPut("/generations/{id:guid}/restore", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var generation = await db.Generations.FindAsync(new object[] { id }, ct);
            if (generation is null) return Results.NotFound();

            generation.IsArchived = false;
            generation.ArchivedAt = null;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { restored = true, id = generation.Id });
        }).RequireLocalApiToken();

        api.MapDelete("/generations/{id:guid}/permanent", async (
            Guid id,
            AppDbContext db,
            IStorageService storage,
            CancellationToken ct) =>
        {
            var generation = await db.Generations.FindAsync(new object[] { id }, ct);
            if (generation is null) return Results.NotFound();
            if (!generation.IsArchived)
                return Results.BadRequest("Generation must be archived before permanent deletion");

            if (generation.FilePath != null)
                await storage.DeleteFileAsync(generation.FilePath, ct);
            if (generation.ThumbnailPath != null)
                await storage.DeleteFileAsync(generation.ThumbnailPath, ct);

            db.Generations.Remove(generation);
            await db.SaveChangesAsync(ct);
            return Results.NoContent();
        }).RequireLocalApiToken();

        return api;
    }
}
