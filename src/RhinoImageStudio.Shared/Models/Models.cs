using RhinoImageStudio.Shared.Enums;

namespace RhinoImageStudio.Shared.Models;

/// <summary>
/// Represents a work project containing captures, generations, and history
/// </summary>
public class Project
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsPinned { get; set; }

    // Navigation properties
    public List<Capture> Captures { get; set; } = [];
    public List<Generation> Generations { get; set; } = [];
    public List<ReferenceImage> References { get; set; } = [];
}

/// <summary>
/// A captured viewport image
/// </summary>
public class Capture
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public DisplayMode DisplayMode { get; set; }
    public string? ViewName { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Metadata from Rhino
    public string? CameraPosition { get; set; }
    public string? CameraTarget { get; set; }
    public double? CameraLens { get; set; }

    // Navigation
    public Project? Project { get; set; }
}

/// <summary>
/// A reference image uploaded by the user for style/content guidance
/// </summary>
public class ReferenceImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? ThumbnailPath { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public Project? Project { get; set; }
}

/// <summary>
/// A generated/refined image result
/// </summary>
public class Generation
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public Guid? ParentGenerationId { get; set; }
    public Guid? SourceCaptureId { get; set; }

    // Stage info
    public JobType Stage { get; set; }
    public string? Prompt { get; set; }
    public string? NegativePrompt { get; set; }

    // Model parameters (stored as JSON)
    public string? ParametersJson { get; set; }

    // Results
    public string? FilePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? Seed { get; set; }

    // Multi-angle specific
    public double? Azimuth { get; set; }
    public double? Elevation { get; set; }
    public double? Zoom { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? FalRequestId { get; set; }
    public string? ModelId { get; set; }

    // Archive support (soft-delete)
    public bool IsArchived { get; set; } = false;
    public DateTime? ArchivedAt { get; set; }

    // Navigation
    public Project? Project { get; set; }
    public Capture? SourceCapture { get; set; }
    public Generation? ParentGeneration { get; set; }
    public List<Generation> ChildGenerations { get; set; } = [];
}

/// <summary>
/// A job in the queue
/// </summary>
public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public JobType Type { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Queued;

    // Request data (stored as JSON)
    public string RequestJson { get; set; } = "{}";

    // Result reference
    public Guid? ResultId { get; set; }  // CaptureId or GenerationId

    // Progress tracking
    public int Progress { get; set; }
    public string? ProgressMessage { get; set; }
    public string? ErrorMessage { get; set; }

    // fal.ai tracking
    public string? FalRequestId { get; set; }

    /// <summary>
    /// Provider model id used for this job (fal/OpenAI route). Set at submit time for accurate cancel.
    /// </summary>
    public string? ProviderModelId { get; set; }

    // Timestamps
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
