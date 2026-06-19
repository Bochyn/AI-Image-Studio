using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Shared.Contracts;

public static class MappingExtensions
{
    public static GenerationDto ToDto(this Generation g) => new(
        g.Id,
        g.ProjectId,
        g.ParentGenerationId,
        g.SourceCaptureId,
        g.Stage,
        g.Prompt,
        g.FilePath != null ? $"/images/{g.FilePath}" : null,
        g.ThumbnailPath != null ? $"/images/{g.ThumbnailPath}" : null,
        g.Width,
        g.Height,
        g.Azimuth,
        g.Elevation,
        g.Zoom,
        g.ModelId,
        g.ParametersJson,
        g.CreatedAt,
        g.IsArchived,
        g.ArchivedAt,
        g.Project?.Name
    );

    public static CaptureDto ToDto(this Capture c) => new(
        c.Id,
        c.ProjectId,
        $"/images/{c.FilePath}",
        c.ThumbnailPath != null ? $"/images/{c.ThumbnailPath}" : null,
        c.Width,
        c.Height,
        c.DisplayMode,
        c.ViewName,
        c.CreatedAt
    );

    public static JobDto ToDto(this Job j) => new(
        j.Id,
        j.ProjectId,
        j.Type,
        j.Status,
        j.Progress,
        j.ProgressMessage,
        j.ErrorMessage,
        j.ResultId,
        j.CreatedAt,
        j.StartedAt,
        j.CompletedAt
    );

    public static ReferenceImageDto ToDto(this ReferenceImage r) => new(
        r.Id,
        r.ProjectId,
        r.OriginalFileName,
        $"/images/{r.FilePath}",
        r.ThumbnailPath != null ? $"/images/{r.ThumbnailPath}" : null,
        r.CreatedAt
    );
}
