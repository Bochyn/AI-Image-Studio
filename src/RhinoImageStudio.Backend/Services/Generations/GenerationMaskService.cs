using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Services.Generations;

public sealed class GenerationMaskService
{
    public async Task<object> GetMasksAsync(AppDbContext db, Guid generationId, CancellationToken ct)
    {
        var loaded = await GenerationJobReader.TryGetGenerateRequestAsync(db, generationId, ct);
        if (loaded is null)
            return Array.Empty<object>();

        var request = loaded.Value.Request;
        if (request.MaskPayload != null)
            return request.MaskPayload;
        if (request.MaskLayers is null || request.MaskLayers.Count == 0)
            return Array.Empty<object>();

        return request.MaskLayers;
    }

    public async Task<byte[]?> GetOverlayImageAsync(AppDbContext db, Guid generationId, CancellationToken ct)
    {
        var loaded = await GenerationJobReader.TryGetGenerateRequestAsync(db, generationId, ct);
        if (loaded?.Request.MaskPayload is null)
            return null;

        return Convert.FromBase64String(loaded.Value.Request.MaskPayload.OverlayImageBase64);
    }

    public async Task<byte[]?> GetMaskLayerImageAsync(
        AppDbContext db,
        Guid generationId,
        int index,
        CancellationToken ct)
    {
        var loaded = await GenerationJobReader.TryGetGenerateRequestAsync(db, generationId, ct);
        if (loaded?.Request.MaskLayers is null || index < 0 || index >= loaded.Value.Request.MaskLayers.Count)
            return null;

        return Convert.FromBase64String(loaded.Value.Request.MaskLayers[index].MaskImageBase64);
    }
}
