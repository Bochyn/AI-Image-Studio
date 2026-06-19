using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Backend.Services;
using RhinoImageStudio.Shared.Contracts;
using RhinoImageStudio.Shared.Enums;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services.Generations;

/// <summary>
/// Loads generate-job request payloads for a generation result.
/// </summary>
public static class GenerationJobReader
{
    public static async Task<(Job Job, GenerateRequest Request)?> TryGetGenerateRequestAsync(
        AppDbContext db,
        Guid generationId,
        CancellationToken ct)
    {
        var job = await db.Jobs
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.ResultId == generationId, ct);

        if (job is null || job.Type != JobType.Generate)
            return null;

        var request = JobRequestJson.Deserialize<GenerateRequest>(job.RequestJson);
        return (job, request);
    }
}
