using System.Text;
using RhinoImageStudio.Shared.Contracts;

namespace RhinoImageStudio.Backend.Services;

/// <summary>
/// Builds augmented prompts for AI image generation with masks and overlays.
/// Shared between JobProcessor (execution) and debug endpoint (inspection).
/// </summary>
public static class PromptBuilder
{
    /// <summary>
    /// Builds a 2-image overlay prompt (source + colored overlay).
    /// Used when MaskPayload with colored overlays is provided.
    /// </summary>
    public static string BuildOverlayPrompt(MaskPayloadData maskPayload, string userPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("IMPORTANT: You are provided with TWO images:");
        sb.AppendLine("1. IMAGE 1 is the ORIGINAL clean photograph/render (no markings).");
        sb.AppendLine("2. IMAGE 2 is the SAME image with colored overlay annotations indicating areas to edit.");
        sb.AppendLine();
        sb.AppendLine("The colored overlays are EDITING MARKERS, NOT content. Do NOT reproduce these colors in the output.");
        sb.AppendLine();
        sb.AppendLine("EDITING INSTRUCTIONS BY COLOR:");
        foreach (var layer in maskPayload.Layers)
        {
            sb.AppendLine($"- {layer.ColorName.ToUpper()} ({layer.Color}) regions: {layer.Instruction}");
        }
        sb.AppendLine();
        sb.AppendLine("RULES:");
        sb.AppendLine("- Use IMAGE 1 as the base for your edit. Use IMAGE 2 only to understand WHERE to apply edits.");
        sb.AppendLine("- Edit ONLY the colored regions. Apply each instruction to its corresponding color.");
        sb.AppendLine("- All non-marked areas MUST remain EXACTLY identical to IMAGE 1.");
        sb.AppendLine("- Match lighting, perspective, texture, and style of surrounding areas.");
        sb.AppendLine($"- Overall scene context: {userPrompt}");
        return sb.ToString();
    }

    /// <summary>
    /// Builds a mask-based inpainting prompt (white pixels = edit area).
    /// Used when MaskLayers with B&W masks are provided.
    /// </summary>
    public static string BuildMaskPrompt(List<MaskLayerData> maskLayers, string userPrompt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are editing an image using masks. White pixels = area to edit, black = keep unchanged.");
        for (int i = 0; i < maskLayers.Count; i++)
        {
            sb.AppendLine($"MASK {i + 1} instruction: {maskLayers[i].Instruction}");
        }
        sb.AppendLine();
        sb.AppendLine("IMPORTANT: Preserve all black (unmasked) areas exactly.");
        sb.AppendLine($"Overall context: {userPrompt}");
        return sb.ToString();
    }
}
