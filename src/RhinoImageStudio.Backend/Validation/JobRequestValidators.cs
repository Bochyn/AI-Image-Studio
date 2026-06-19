using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Validation;

public static class JobRequestValidators
{
    public static string? Validate(RefineRequest request)
    {
        if (request.ProjectId == Guid.Empty)
            return "ProjectId is required";
        if (request.ParentGenerationId == Guid.Empty)
            return "ParentGenerationId is required";
        if (string.IsNullOrWhiteSpace(request.Prompt))
            return "Prompt is required";
        return null;
    }

    public static string? Validate(MultiAngleRequest request)
    {
        if (request.ProjectId == Guid.Empty)
            return "ProjectId is required";
        if (!request.SourceGenerationId.HasValue && !request.SourceCaptureId.HasValue)
            return "Either SourceGenerationId or SourceCaptureId is required";
        if (request.SourceGenerationId.HasValue && request.SourceCaptureId.HasValue)
            return "Provide only one of SourceGenerationId or SourceCaptureId";
        if (request.HorizontalAngle is < 0 or > 360)
            return "HorizontalAngle must be between 0 and 360";
        if (request.VerticalAngle is < -30 or > 90)
            return "VerticalAngle must be between -30 and 90";
        if (request.Zoom is < 0 or > 10)
            return "Zoom must be between 0 and 10";
        if (request.LoraScale is < 0 or > 1)
            return "LoraScale must be between 0 and 1";
        if (request.NumImages < 1)
            return "NumImages must be at least 1";
        return null;
    }

    public static string? Validate(UpscaleRequest request)
    {
        if (request.ProjectId == Guid.Empty)
            return "ProjectId is required";
        if (request.SourceGenerationId == Guid.Empty)
            return "SourceGenerationId is required";
        if (request.UpscaleFactor is < 1 or > 4)
            return "UpscaleFactor must be between 1 and 4";
        return null;
    }
}
