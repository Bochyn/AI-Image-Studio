using Microsoft.EntityFrameworkCore;
using RhinoImageStudio.Backend.Data;
using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Enums;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services.Jobs;

public sealed class JobResultPersister
{
    private readonly IHttpClientFactory _httpClientFactory;

    public JobResultPersister(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Generation> SaveFalResultAsync(
        Guid projectId,
        Guid? sourceCaptureId,
        Guid? parentGenerationId,
        JobType stage,
        string? prompt,
        FalResultResponse result,
        AppDbContext dbContext,
        IStorageService storage,
        CancellationToken cancellationToken,
        string? parametersJson = null,
        string? modelId = null)
    {
        var falImage = result.Images?.FirstOrDefault() ?? result.Image
            ?? throw new InvalidOperationException("No image in result");

        using var httpClient = _httpClientFactory.CreateClient("ImageDownloader");
        var imageData = await httpClient.GetByteArrayAsync(falImage.Url, cancellationToken);

        var generation = new Generation
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SourceCaptureId = sourceCaptureId,
            ParentGenerationId = parentGenerationId,
            Stage = stage,
            Prompt = prompt,
            Width = falImage.Width,
            Height = falImage.Height,
            Seed = result.Seed,
            FalRequestId = result.RequestId,
            ModelId = modelId ?? stage switch
            {
                JobType.Generate => FalModels.NanoBananaEdit,
                JobType.Refine => FalModels.NanoBananaEdit,
                JobType.MultiAngle => FalModels.QwenMultipleAngles,
                JobType.Upscale => FalModels.TopazUpscale,
                _ => null
            },
            ParametersJson = parametersJson
        };

        generation.FilePath = await storage.SaveGenerationAsync(
            generation.Id, imageData, GetFalImageFormat(falImage), cancellationToken);
        generation.ThumbnailPath = await storage.SaveThumbnailAsync(generation.Id, imageData, cancellationToken);

        dbContext.Generations.Add(generation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return generation;
    }

    public async Task<Generation> SaveGeminiResultAsync(
        Guid projectId,
        Guid? sourceCaptureId,
        Guid? parentGenerationId,
        JobType stage,
        string? prompt,
        GeminiImageResult result,
        string modelId,
        AppDbContext dbContext,
        IStorageService storage,
        CancellationToken cancellationToken,
        string? parametersJson = null)
    {
        var extension = result.MimeType.Contains("png", StringComparison.OrdinalIgnoreCase) ? "png" : "jpg";

        var generation = new Generation
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            SourceCaptureId = sourceCaptureId,
            ParentGenerationId = parentGenerationId,
            Stage = stage,
            Prompt = prompt,
            ModelId = modelId,
            ParametersJson = parametersJson
        };

        generation.FilePath = await storage.SaveGenerationAsync(
            generation.Id, result.ImageData, extension, cancellationToken);
        generation.ThumbnailPath = await storage.SaveThumbnailAsync(
            generation.Id, result.ImageData, cancellationToken);

        dbContext.Generations.Add(generation);
        await dbContext.SaveChangesAsync(cancellationToken);

        return generation;
    }

    private static string GetFalImageFormat(FalImage image) =>
        image.ContentType?.ToLowerInvariant() switch
        {
            "image/jpeg" => "jpeg",
            "image/png" => "png",
            "image/webp" => "webp",
            _ when image.Url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) => "jpeg",
            _ when image.Url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) => "jpeg",
            _ when image.Url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) => "webp",
            _ => "png"
        };
}
