using RhinoImageStudio.Shared.Enums;

namespace RhinoImageStudio.Shared.Contracts;

// ============================================================================
// Project Contracts
// ============================================================================

public record CreateProjectRequest(string Name, string? Description = null);

public record UpdateProjectRequest(string? Name = null, string? Description = null, bool? IsPinned = null);

public record ProjectDto(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool IsPinned,
    int CaptureCount,
    int GenerationCount,
    string? LastThumbnailUrl
);

public record ProjectListResponse(List<ProjectDto> Projects, int TotalCount);
public record HealthResponse(string Status, DateTime Timestamp);

// ============================================================================
// Capture Contracts
// ============================================================================

public record CaptureRequest(
    Guid ProjectId,
    int Width = 1024,
    int Height = 1024,
    DisplayMode DisplayMode = DisplayMode.Shaded,
    bool TransparentBackground = false
);

public record CaptureDto(
    Guid Id,
    Guid ProjectId,
    string ImageUrl,
    string? ThumbnailUrl,
    int Width,
    int Height,
    DisplayMode DisplayMode,
    string? ViewName,
    DateTime CreatedAt
);

// ============================================================================
// Generation Contracts
// ============================================================================

public record MaskLayerData(string MaskImageBase64, string Instruction);

public record MaskOverlayLayerData(string Color, string ColorName, string Instruction);
public record MaskPayloadData(string OverlayImageBase64, List<MaskOverlayLayerData> Layers);

public record GenerateRequest(
    Guid ProjectId,
    string Prompt,
    Guid? SourceCaptureId = null,
    Guid? ParentGenerationId = null,
    string? Model = null,  // e.g., "gemini-2.5-flash-image", "gemini-3-pro-image-preview", "fal-ai/nano-banana/edit"
    string? AspectRatio = null,  // e.g., "1:1", "16:9", "4:3" - passed directly to Gemini API
    string? Resolution = null,   // e.g., "1K", "2K", "4K"
    int NumImages = 1,
    string? OutputFormat = null,  // e.g., "png", "jpeg"
    List<Guid>? ReferenceImageIds = null,
    List<MaskLayerData>? MaskLayers = null,
    MaskPayloadData? MaskPayload = null,
    string? Quality = null,
    string? InputFidelity = null
);

public record RefineRequest(
    Guid ProjectId,
    Guid ParentGenerationId,
    string Prompt
);

public record MultiAngleRequest(
    Guid ProjectId,
    Guid? SourceGenerationId = null,  // Either generation or capture must be provided
    Guid? SourceCaptureId = null,
    double HorizontalAngle = 0,       // 0-360 degrees
    double VerticalAngle = 0,         // -30 to 90 degrees
    double Zoom = 5,                  // 0-10
    double LoraScale = 0.8,           // 0-1
    int NumImages = 1
);

public record UpscaleRequest(
    Guid ProjectId,
    Guid SourceGenerationId,
    int UpscaleFactor = 2,              // 1-4
    string Model = "Standard V2",        // Topaz model type
    bool FaceEnhancement = false,
    ImageFormat OutputFormat = ImageFormat.Jpeg
);

public record GenerationDto(
    Guid Id,
    Guid ProjectId,
    Guid? ParentGenerationId,
    Guid? SourceCaptureId,
    JobType Stage,
    string? Prompt,
    string? ImageUrl,
    string? ThumbnailUrl,
    int? Width,
    int? Height,
    double? Azimuth,
    double? Elevation,
    double? Zoom,
    string? ModelId,
    string? ParametersJson,
    DateTime CreatedAt,
    bool IsArchived = false,
    DateTime? ArchivedAt = null,
    string? ProjectName = null
);

public record ReferenceImageDto(
    Guid Id,
    Guid ProjectId,
    string OriginalFileName,
    string ImageUrl,
    string? ThumbnailUrl,
    DateTime CreatedAt
);

// ============================================================================
// Job Contracts
// ============================================================================

public record JobDto(
    Guid Id,
    Guid ProjectId,
    JobType Type,
    JobStatus Status,
    int Progress,
    string? ProgressMessage,
    string? ErrorMessage,
    Guid? ResultId,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt
);

public record JobProgressEvent(
    Guid JobId,
    JobStatus Status,
    int Progress,
    string? Message,
    Guid? ResultId = null
);

// ============================================================================
// Config Contracts
// ============================================================================

public record ConfigDto(
    bool HasFalApiKey,
    bool HasGeminiApiKey,
    string DataPath,
    int BackendPort,
    string DefaultProvider = "gemini"  // "gemini" or "fal"
);

public record SetGeminiApiKeyRequest(string ApiKey);

public record SetFalApiKeyRequest(string ApiKey);

// ============================================================================
// Export Contracts
// ============================================================================

public record ExportRequest(
    Guid GenerationId,
    ImageFormat Format = ImageFormat.Png,
    int? Width = null,
    int? Height = null,
    bool IncludeMetadata = true
);

public record ExportResult(
    string FilePath,
    long FileSizeBytes
);
