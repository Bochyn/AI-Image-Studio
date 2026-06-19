using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Enums;

namespace RhinoImageStudio.Backend.Services.Generations;

public sealed class GenerationDebugService
{
    private static readonly Regex Base64RedactionRegex = new(
        @"""([A-Za-z0-9+/=]{20})[A-Za-z0-9+/=]*([A-Za-z0-9+/=]{20})""",
        RegexOptions.Compiled);

    public async Task<object?> BuildDebugResponseAsync(AppDbContext db, Guid generationId, CancellationToken ct)
    {
        var job = await db.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.ResultId == generationId, ct);

        if (job is null)
            return null;

        if (job.Type != JobType.Generate)
        {
            return new
            {
                jobType = job.Type.ToString(),
                info = "Debug details only available for Generate jobs. Raw request stored but uses different schema."
            };
        }

        var request = JobRequestJson.Deserialize<GenerateRequest>(job.RequestJson);

        var sourceType = request.SourceCaptureId != null
            ? "capture"
            : request.ParentGenerationId != null
                ? "generation"
                : (string?)null;
        var sourceId = request.SourceCaptureId ?? request.ParentGenerationId;

        List<object>? referenceDetails = null;
        if (request.ReferenceImageIds is { Count: > 0 })
        {
            var refIds = request.ReferenceImageIds;
            var refs = await db.ReferenceImages
                .AsNoTracking()
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
                    thumbnailUrl = r?.ThumbnailPath != null
                        ? $"/images/{r.ThumbnailPath}"
                        : r?.FilePath != null
                            ? $"/images/{r.FilePath}"
                            : null
                };
            }).ToList();
        }

        var rawJsonNode = System.Text.Json.Nodes.JsonNode.Parse(job.RequestJson);
        var rawJson = rawJsonNode?.ToJsonString(new JsonSerializerOptions
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }) ?? job.RequestJson;

        rawJson = Base64RedactionRegex.Replace(
            rawJson,
            m => $"\"{m.Groups[1].Value}...{m.Groups[2].Value}\"");

        string? augmentedPrompt = null;
        if (request.MaskPayload != null)
            augmentedPrompt = PromptBuilder.BuildOverlayPrompt(request.MaskPayload, request.Prompt);
        else if (request.MaskLayers is { Count: > 0 })
            augmentedPrompt = PromptBuilder.BuildMaskPrompt(request.MaskLayers, request.Prompt);

        return new
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
            maskOverlay = request.MaskPayload != null
                ? new
                {
                    overlayImageSize = $"[PNG, ~{request.MaskPayload.OverlayImageBase64.Length * 3 / 4 / 1024}KB]",
                    layers = request.MaskPayload.Layers.Select(l => new
                    {
                        color = l.Color,
                        colorName = l.ColorName,
                        instruction = l.Instruction
                    })
                }
                : (object?)null,
            numImages = request.NumImages,
            outputFormat = request.OutputFormat,
            rawJson
        };
    }
}
