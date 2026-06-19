using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Services.Jobs;

internal static class SourceImageLoader
{
    public static async Task<byte[]?> LoadSourceImageAsync(
        GenerateRequest request,
        AppDbContext db,
        IStorageService storage,
        CancellationToken cancellationToken)
    {
        if (request.SourceCaptureId.HasValue)
        {
            var capture = await db.Captures.FindAsync([request.SourceCaptureId.Value], cancellationToken);
            if (capture != null)
                return await storage.ReadFileAsync(capture.FilePath, cancellationToken);
        }
        else if (request.ParentGenerationId.HasValue)
        {
            var parent = await db.Generations.FindAsync([request.ParentGenerationId.Value], cancellationToken);
            if (parent?.FilePath != null)
                return await storage.ReadFileAsync(parent.FilePath, cancellationToken);
        }

        return null;
    }

    public static async Task<List<byte[]>?> LoadReferenceImagesAsync(
        GenerateRequest request,
        AppDbContext db,
        IStorageService storage,
        CancellationToken cancellationToken)
    {
        if (request.ReferenceImageIds is not { Count: > 0 })
            return null;

        var images = new List<byte[]>();
        var maxRefs = ModelCapabilityLimits.GetMaxReferences(request.Model);
        foreach (var refId in request.ReferenceImageIds.Take(maxRefs))
        {
            var refImage = await db.ReferenceImages.FindAsync([refId], cancellationToken);
            if (refImage != null)
                images.Add(await storage.ReadFileAsync(refImage.FilePath, cancellationToken));
        }

        return images.Count > 0 ? images : null;
    }
}
