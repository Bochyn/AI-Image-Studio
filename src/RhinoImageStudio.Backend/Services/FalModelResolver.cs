using RhinoImageStudio.Shared.Constants;
using RhinoImageStudio.Shared.Enums;
using RhinoImageStudio.Shared.Models;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Resolves fal.ai model IDs for job cancellation and polling.
/// </summary>
public static class FalModelResolver
{
    public static string ResolveModelId(Job job) =>
        job.ProviderModelId ?? job.Type switch
        {
            JobType.Generate => FalModels.NanoBananaEdit,
            JobType.Refine => FalModels.NanoBananaEdit,
            JobType.MultiAngle => FalModels.QwenMultipleAngles,
            JobType.Upscale => FalModels.TopazUpscale,
            _ => FalModels.NanoBananaEdit
        };
}
