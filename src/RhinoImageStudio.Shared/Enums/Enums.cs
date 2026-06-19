namespace RhinoImageStudio.Shared.Enums;

/// <summary>
/// Status of a job in the queue
/// </summary>
public enum JobStatus
{
    Queued = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Canceled = 4
}

/// <summary>
/// Type of job/stage in the pipeline
/// </summary>
public enum JobType
{
    Capture = 0,
    Generate = 1,
    Refine = 2,
    MultiAngle = 3,
    Upscale = 4,
    Export = 5
}

/// <summary>
/// Display modes available in Rhino
/// </summary>
public enum DisplayMode
{
    Shaded = 0,
    Wireframe = 1,
    Rendered = 2,
    Ghosted = 3,
    XRay = 4,
    Technical = 5,
    Artistic = 6,
    Pen = 7,
    Arctic = 8,
    Raytraced = 9
}

/// <summary>
/// Aspect ratio presets
/// </summary>
public enum AspectRatio
{
    Auto,
    Square_1_1,
    Landscape_16_9,
    Landscape_4_3,
    Landscape_3_2,
    Landscape_21_9,
    Portrait_9_16,
    Portrait_3_4,
    Portrait_2_3,
    Portrait_4_5
}

/// <summary>
/// Resolution presets
/// </summary>
public enum Resolution
{
    R_1K,   // 1024px
    R_2K,   // 2048px
    R_4K    // 4096px
}

/// <summary>
/// Image output format
/// </summary>
public enum ImageFormat
{
    Png = 0,
    Jpeg = 1,
    Webp = 2
}
