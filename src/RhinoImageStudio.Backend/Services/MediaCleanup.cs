namespace RhinoImageStudio.Backend.Services;

public static class MediaCleanup
{
    public static async Task DeleteFilesAsync(
        IStorageService storage,
        string? filePath,
        string? thumbnailPath,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(filePath))
            await storage.DeleteFileAsync(filePath, cancellationToken);
        if (!string.IsNullOrEmpty(thumbnailPath))
            await storage.DeleteFileAsync(thumbnailPath, cancellationToken);
    }
}
