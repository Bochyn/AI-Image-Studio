namespace RhinoImageStudio.Shared.Constants;

/// <summary>
/// fal.ai model identifiers
/// </summary>
public static class FalModels
{
    // Generation & Refine
    public const string NanoBananaPro = "fal-ai/nano-banana-pro";
    public const string NanoBananaEdit = "fal-ai/nano-banana/edit";

    // Multi-Angle
    public const string QwenMultipleAngles = "fal-ai/qwen-image-edit-2511-multiple-angles";

    // Upscale
    public const string TopazUpscale = "fal-ai/topaz/upscale/image";
    public const string ClarityUpscaler = "fal-ai/clarity-upscaler";
    public const string Esrgan = "fal-ai/esrgan";
    public const string CreativeUpscaler = "fal-ai/creative-upscaler";

    // Seedream & GPT-Image
    public const string SeedreamV5LiteEdit = "fal-ai/bytedance/seedream/v5/lite/edit";
    public const string GptImage15Edit = "fal-ai/gpt-image-1.5/edit";
    public const string GptImage2Edit = "openai/gpt-image-2/edit";

    public static bool IsFalRouted(string modelId) =>
        modelId.StartsWith("fal-", StringComparison.Ordinal) ||
        modelId.StartsWith("openai/", StringComparison.Ordinal);

    public static int GetMaxPollAttempts(string modelId) => modelId switch
    {
        GptImage2Edit => 300,
        GptImage15Edit => 300,
        _ => 120
    };
}

/// <summary>
/// Google Gemini model identifiers
/// </summary>
public static class GeminiModels
{
    // Nano Banana - Primary image generation model
    public const string NanoBanana = "gemini-3.1-flash-image-preview";

    // Nano Banana Pro - Advanced image generation (future)
    public const string NanoBananaPro = "gemini-3-pro-image-preview";
}

/// <summary>
/// Provider types for image generation
/// </summary>
public static class Providers
{
    public const string Gemini = "gemini";
    public const string FalAi = "fal";
}

/// <summary>
/// File storage paths
/// </summary>
public static class StoragePaths
{
    public const string Captures = "captures";
    public const string Generations = "generations";
    public const string Thumbnails = "thumbnails";
    public const string Exports = "exports";
    public const string Temp = "temp";
}

/// <summary>
/// Default values
/// </summary>
public static class Defaults
{
    public const int CaptureWidth = 1024;
    public const int CaptureHeight = 1024;
    public const int ThumbnailSize = 256;
    public const int DefaultPort = 17532;  // "RISTU" on phone keypad
    public const string DatabaseName = "rhinoimagestudio.db";
}
