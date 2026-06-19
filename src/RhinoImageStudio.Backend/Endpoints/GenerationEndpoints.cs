using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Enums;

namespace RhinoImageStudio.Backend.Endpoints;

public static class GenerationEndpoints
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    public static RouteGroupBuilder MapGenerationEndpoints(this RouteGroupBuilder api)
    {
        api.MapGet("/projects/{projectId:guid}/generations/archived", async (Guid projectId, AppDbContext db, CancellationToken ct) =>
        {
            var archived = await db.Generations
                .Where(g => g.ProjectId == projectId && g.IsArchived)
                .OrderByDescending(g => g.ArchivedAt)
                .ToListAsync(ct);

            return Results.Ok(archived.Select(g => g.ToDto()).ToList());
        });

        api.MapGet("/projects/{projectId:guid}/generations", async (Guid projectId, AppDbContext db, CancellationToken ct) =>
        {
            var generations = await db.Generations
                .Where(g => g.ProjectId == projectId)
                .Where(g => !g.IsArchived)
                .OrderByDescending(g => g.CreatedAt)
                .ToListAsync(ct);

            return Results.Ok(generations.Select(g => g.ToDto()).ToList());
        });

        api.MapGet("/generations", async (AppDbContext db, int? limit, int? offset, CancellationToken ct) =>
        {
            var query = db.Generations
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
            var generation = await db.Generations.FindAsync(new object[] { id }, ct);
            if (generation is null) return Results.NotFound();

            return Results.Ok(generation.ToDto());
        });

        api.MapGet("/generations/{id:guid}/debug", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var job = await db.Jobs.FirstOrDefaultAsync(j => j.ResultId == id, ct);
            if (job is null) return Results.NotFound(new { error = "No job found for this generation" });

            if (job.Type != JobType.Generate)
                return Results.Json(new { jobType = job.Type.ToString(), info = "Debug details only available for Generate jobs. Raw request stored but uses different schema." }, JsonOptions);

            var request = JobRequestJson.Deserialize<GenerateRequest>(job.RequestJson);

            var sourceType = request.SourceCaptureId != null ? "capture" : request.ParentGenerationId != null ? "generation" : (string?)null;
            var sourceId = request.SourceCaptureId ?? request.ParentGenerationId;

            List<object>? referenceDetails = null;
            if (request.ReferenceImageIds is { Count: > 0 })
            {
                var refIds = request.ReferenceImageIds;
                var refs = await db.ReferenceImages
                    .Where(r => refIds.Contains(r.Id))
                    .Select(r => new { r.Id, r.OriginalFileName, r.ThumbnailPath, r.FilePath })
                    .ToListAsync(ct);
                referenceDetails = refIds.Select(rid =>
                {
                    var r = refs.FirstOrDefault(x => x.Id == rid);
                    return (object)new
                    {
                        id = rid.ToString(),
                        fileName = r?.OriginalFileName ?? "(unknown)",
                        thumbnailUrl = r?.ThumbnailPath != null ? $"/images/{r.ThumbnailPath}" : (r?.FilePath != null ? $"/images/{r.FilePath}" : null)
                    };
                }).ToList();
            }

            var rawJsonNode = System.Text.Json.Nodes.JsonNode.Parse(job.RequestJson);
            var rawJson = rawJsonNode?.ToJsonString(new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            }) ?? job.RequestJson;

            rawJson = System.Text.RegularExpressions.Regex.Replace(
                rawJson,
                @"""([A-Za-z0-9+/=]{20})[A-Za-z0-9+/=]*([A-Za-z0-9+/=]{20})""",
                m => $"\"{m.Groups[1].Value}...{m.Groups[2].Value}\"");

            string? augmentedPrompt = null;
            if (request.MaskPayload != null)
                augmentedPrompt = PromptBuilder.BuildOverlayPrompt(request.MaskPayload, request.Prompt);
            else if (request.MaskLayers is { Count: > 0 })
                augmentedPrompt = PromptBuilder.BuildMaskPrompt(request.MaskLayers, request.Prompt);

            var response = new
            {
                prompt = request.Prompt,
                augmentedPrompt,
                model = request.Model,
                aspectRatio = request.AspectRatio,
                resolution = request.Resolution,
                sourceType,
                sourceId,
                referenceCount = request.ReferenceImageIds?.Count ?? 0,
                referenceImageIds = request.ReferenceImageIds,
                referenceDetails,
                masks = request.MaskLayers?.Select((m, i) => new
                {
                    index = i + 1,
                    instruction = m.Instruction,
                    imageSize = $"[PNG, ~{m.MaskImageBase64.Length * 3 / 4 / 1024}KB]"
                }),
                maskOverlay = request.MaskPayload != null ? new
                {
                    overlayImageSize = $"[PNG, ~{request.MaskPayload.OverlayImageBase64.Length * 3 / 4 / 1024}KB]",
                    layers = request.MaskPayload.Layers.Select(l => new
                    {
                        color = l.Color,
                        colorName = l.ColorName,
                        instruction = l.Instruction
                    })
                } : (object?)null,
                numImages = request.NumImages,
                outputFormat = request.OutputFormat,
                rawJson
            };

            return Results.Json(response, JsonOptions);
        });

        api.MapGet("/generations/{id:guid}/masks", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var job = await db.Jobs.FirstOrDefaultAsync(j => j.ResultId == id, ct);
            if (job is null || job.Type != JobType.Generate)
                return Results.Json(Array.Empty<object>(), JsonOptions);

            var request = JsonSerializer.Deserialize<GenerateRequest>(job.RequestJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } });

            if (request?.MaskPayload != null)
                return Results.Json(request.MaskPayload, JsonOptions);

            if (request?.MaskLayers is null || request.MaskLayers.Count == 0)
                return Results.Json(Array.Empty<object>(), JsonOptions);

            return Results.Json(request.MaskLayers, JsonOptions);
        });

        api.MapGet("/generations/{id:guid}/masks/overlay", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var job = await db.Jobs.FirstOrDefaultAsync(j => j.ResultId == id, ct);
            if (job is null || job.Type != JobType.Generate)
                return Results.NotFound();

            var request = JsonSerializer.Deserialize<GenerateRequest>(job.RequestJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } });
            if (request?.MaskPayload is null)
                return Results.NotFound();

            var bytes = Convert.FromBase64String(request.MaskPayload.OverlayImageBase64);
            return Results.Bytes(bytes, "image/png");
        });

        api.MapGet("/generations/{id:guid}/masks/{index:int}/image", async (Guid id, int index, AppDbContext db, CancellationToken ct) =>
        {
            var job = await db.Jobs.FirstOrDefaultAsync(j => j.ResultId == id, ct);
            if (job is null || job.Type != JobType.Generate)
                return Results.NotFound();

            var request = JsonSerializer.Deserialize<GenerateRequest>(job.RequestJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true, Converters = { new JsonStringEnumConverter() } });
            if (request?.MaskLayers is null || index < 0 || index >= request.MaskLayers.Count)
                return Results.NotFound();

            var bytes = Convert.FromBase64String(request.MaskLayers[index].MaskImageBase64);
            return Results.Bytes(bytes, "image/png");
        });

        api.MapDelete("/generations/{id:guid}", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var generation = await db.Generations.FindAsync(new object[] { id }, ct);
            if (generation is null) return Results.NotFound();
            generation.IsArchived = true;
            generation.ArchivedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { archived = true, id = generation.Id });
        });

        api.MapPut("/generations/{id:guid}/restore", async (Guid id, AppDbContext db, CancellationToken ct) =>
        {
            var generation = await db.Generations.FindAsync(new object[] { id }, ct);
            if (generation is null) return Results.NotFound();
            generation.IsArchived = false;
            generation.ArchivedAt = null;
            await db.SaveChangesAsync(ct);
            return Results.Ok(new { restored = true, id = generation.Id });
        });

        api.MapDelete("/generations/{id:guid}/permanent", async (Guid id, AppDbContext db, IStorageService storage, CancellationToken ct) =>
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
        });

        return api;
    }
}
