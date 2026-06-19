using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Endpoints;

public static class ProjectEndpoints
{
    private static string? ToImageUrl(string? path)
        => string.IsNullOrWhiteSpace(path) ? null : $"/images/{path.Replace('\\', '/')}";

    public static RouteGroupBuilder MapProjectEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/health", () => Results.Ok(new HealthResponse("healthy", DateTime.UtcNow)));

        api.MapGet("/projects", async (AppDbContext db, CancellationToken ct) =>
        {
            var projects = await db.Projects
                .OrderByDescending(s => s.IsPinned)
                .ThenByDescending(s => s.UpdatedAt)
                .Select(s => new ProjectDto(
                    s.Id,
                    s.Name,
                    s.Description,
                    s.CreatedAt,
                    s.UpdatedAt,
                    s.IsPinned,
                    s.Captures.Count,
                    s.Generations.Count(g => !g.IsArchived),
                    ToImageUrl(
                        s.Generations
                            .Where(g => !g.IsArchived)
                            .OrderByDescending(g => g.CreatedAt)
                            .Select(g => g.ThumbnailPath)
                            .FirstOrDefault()
                    )
                ))
                .ToListAsync(ct);

            return Results.Ok(new ProjectListResponse(projects, projects.Count));
        });

        api.MapGet("/projects/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.FindAsync(new object[] { id }, ct);
            return project is null ? Results.NotFound() : Results.Ok(project);
        });

        api.MapPost("/projects", async (CreateProjectRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var project = new Project
            {
                Name = request.Name,
                Description = request.Description
            };

            db.Projects.Add(project);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/projects/{project.Id}", project);
        });

        api.MapPut("/projects/{id:guid}", async (Guid id, UpdateProjectRequest request, AppDbContext db, CancellationToken ct) =>
        {
            var project = await db.Projects.FindAsync(new object[] { id }, ct);
            if (project is null) return Results.NotFound();

            if (request.Name is not null) project.Name = request.Name;
            if (request.Description is not null) project.Description = request.Description;
            if (request.IsPinned.HasValue) project.IsPinned = request.IsPinned.Value;
            project.UpdatedAt = DateTime.UtcNow;

            await db.SaveChangesAsync(ct);
            return Results.Ok(project);
        });

        api.MapDelete("/projects/{id:guid}", async (Guid id, AppDbContext db, IStorageService storage, CancellationToken ct) =>
        {
            var project = await db.Projects
                .Include(s => s.Captures)
                .Include(s => s.Generations)
                .Include(s => s.References)
                .FirstOrDefaultAsync(s => s.Id == id, ct);

            if (project is null) return Results.NotFound();

            foreach (var capture in project.Captures)
            {
                await storage.DeleteFileAsync(capture.FilePath, ct);
                if (capture.ThumbnailPath != null)
                    await storage.DeleteFileAsync(capture.ThumbnailPath, ct);
            }
            foreach (var generation in project.Generations)
            {
                if (generation.FilePath != null)
                    await storage.DeleteFileAsync(generation.FilePath, ct);
                if (generation.ThumbnailPath != null)
                    await storage.DeleteFileAsync(generation.ThumbnailPath, ct);
            }
            foreach (var reference in project.References)
            {
                await storage.DeleteFileAsync(reference.FilePath, ct);
                if (reference.ThumbnailPath != null)
                    await storage.DeleteFileAsync(reference.ThumbnailPath, ct);
            }

            db.Projects.Remove(project);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        return api;
    }
}
