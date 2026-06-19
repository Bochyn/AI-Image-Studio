using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Png;
using Microsoft.Extensions.Options;
using RhinoImageStudio.Backend.Options;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Service for managing file storage (images, thumbnails, exports)
/// </summary>
public interface IStorageService
{
    string BasePath { get; }
    string CapturesPath { get; }
    string GenerationsPath { get; }
    string ThumbnailsPath { get; }
    string ExportsPath { get; }
    string TempPath { get; }
    string ReferencesPath { get; }

    Task<string> SaveCaptureAsync(Guid captureId, byte[] imageData, string format = "png", CancellationToken cancellationToken = default);
    Task<string> SaveReferenceAsync(Guid referenceId, byte[] imageData, string format = "png", CancellationToken cancellationToken = default);
    Task<string> SaveGenerationAsync(Guid generationId, byte[] imageData, string format = "png", CancellationToken cancellationToken = default);
    Task<string> SaveThumbnailAsync(Guid id, byte[] imageData, CancellationToken cancellationToken = default);
    Task<byte[]> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default);
    string GetAbsolutePath(string relativePath);
    string GetRelativePath(string absolutePath);
}

public class StorageService : IStorageService
{
    private readonly ILogger<StorageService> _logger;

    public string BasePath { get; }
    public string CapturesPath => Path.Combine(BasePath, "captures");
    public string GenerationsPath => Path.Combine(BasePath, "generations");
    public string ThumbnailsPath => Path.Combine(BasePath, "thumbnails");
    public string ExportsPath => Path.Combine(BasePath, "exports");
    public string TempPath => Path.Combine(BasePath, "temp");
    public string ReferencesPath => Path.Combine(BasePath, "references");

    public StorageService(ILogger<StorageService> logger, IOptions<StorageOptions> options)
    {
        _logger = logger;
        BasePath = options.Value.BasePath;

        // Ensure all directories exist
        EnsureDirectoriesExist();
    }

    private void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(CapturesPath);
        Directory.CreateDirectory(GenerationsPath);
        Directory.CreateDirectory(ThumbnailsPath);
        Directory.CreateDirectory(ExportsPath);
        Directory.CreateDirectory(TempPath);
        Directory.CreateDirectory(ReferencesPath);

        _logger.LogInformation("Storage initialized at: {BasePath}", BasePath);
    }

    public async Task<string> SaveCaptureAsync(Guid captureId, byte[] imageData, string format = "png", CancellationToken cancellationToken = default)
    {
        var fileName = $"{captureId}.{format}";
        var relativePath = Path.Combine("captures", fileName);
        var absolutePath = Path.Combine(CapturesPath, fileName);

        await File.WriteAllBytesAsync(absolutePath, imageData, cancellationToken);
        _logger.LogDebug("Capture saved: {Path}", relativePath);

        return relativePath;
    }

    public async Task<string> SaveReferenceAsync(Guid referenceId, byte[] imageData, string format = "png", CancellationToken cancellationToken = default)
    {
        var fileName = $"{referenceId}.{format}";
        var relativePath = Path.Combine("references", fileName);
        var absolutePath = Path.Combine(ReferencesPath, fileName);

        await File.WriteAllBytesAsync(absolutePath, imageData, cancellationToken);
        _logger.LogDebug("Reference saved: {Path}", relativePath);

        return relativePath;
    }

    public async Task<string> SaveGenerationAsync(Guid generationId, byte[] imageData, string format = "png", CancellationToken cancellationToken = default)
    {
        var fileName = $"{generationId}.{format}";
        var relativePath = Path.Combine("generations", fileName);
        var absolutePath = Path.Combine(GenerationsPath, fileName);

        await File.WriteAllBytesAsync(absolutePath, imageData, cancellationToken);
        _logger.LogDebug("Generation saved: {Path}", relativePath);

        return relativePath;
    }

    public async Task<string> SaveThumbnailAsync(Guid id, byte[] imageData, CancellationToken cancellationToken = default)
    {
        var fileName = $"{id}_thumb.png";
        var relativePath = Path.Combine("thumbnails", fileName);
        var absolutePath = Path.Combine(ThumbnailsPath, fileName);

        using var inputStream = new MemoryStream(imageData);
        using var image = await SixLabors.ImageSharp.Image.LoadAsync(inputStream, cancellationToken);

        var maxSize = 256;
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new SixLabors.ImageSharp.Size(maxSize, maxSize),
            Mode = ResizeMode.Max
        }));

        await using var outputStream = File.Create(absolutePath);
        await image.SaveAsync(outputStream, new PngEncoder(), cancellationToken);
        _logger.LogDebug("Thumbnail saved: {Path} ({Width}x{Height})", relativePath, image.Width, image.Height);

        return relativePath;
    }

    public async Task<byte[]> ReadFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetAbsolutePath(relativePath);

        if (!File.Exists(absolutePath))
        {
            throw new FileNotFoundException($"File not found: {relativePath}");
        }

        return await File.ReadAllBytesAsync(absolutePath, cancellationToken);
    }

    public Task DeleteFileAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        var absolutePath = GetAbsolutePath(relativePath);

        if (File.Exists(absolutePath))
        {
            File.Delete(absolutePath);
            _logger.LogDebug("File deleted: {Path}", relativePath);
        }

        return Task.CompletedTask;
    }

    public string GetAbsolutePath(string relativePath)
    {
        // Normalize path separators for Windows compatibility (URL uses /, Windows uses \)
        var normalizedPath = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var absolutePath = Path.GetFullPath(Path.Combine(BasePath, normalizedPath));

        // Prevent path traversal attacks
        if (!absolutePath.StartsWith(BasePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Access denied: path escapes storage root");
        }

        return absolutePath;
    }

    public string GetRelativePath(string absolutePath)
    {
        if (absolutePath.StartsWith(BasePath, StringComparison.OrdinalIgnoreCase))
        {
            return absolutePath.Substring(BasePath.Length).TrimStart(Path.DirectorySeparatorChar);
        }
        return absolutePath;
    }
}
