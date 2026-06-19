# Seedream + GPT-Image Integration Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Seedream v5 Lite Edit and GPT-Image 1.5 Edit to Generate/Refine modes.

**Architecture:** New model entries in `models.ts` (SoT), new fal.ai payload builders in `JobProcessor.cs`, extended `GenerateRequest` DTO with quality/fidelity, conditional UI dropdowns in InspectorPanel.

**Tech Stack:** TypeScript/React (models.ts, InspectorPanel), C#/.NET 8 (JobProcessor, Contracts, Constants)

**Spec:** `docs/superpowers/specs/2026-03-21-fal-models-design.md`

---

### Task 1: Update `Constants.cs` + `Contracts.cs` — new models and DTO fields

**Files:**
- Modify: `src/RhinoImageStudio.Shared/Constants/Constants.cs`
- Modify: `src/RhinoImageStudio.Shared/Contracts/Contracts.cs`

- [ ] **Step 1: Add new FalModels constants**

In `Constants.cs`, add to `FalModels` class:

```csharp
public const string SeedreamV5LiteEdit = "fal-ai/bytedance/seedream/v5/lite/edit";
public const string GptImage15Edit = "fal-ai/gpt-image-1.5/edit";
```

Also update `GeminiModels.NanoBanana` from `"gemini-2.5-flash-image"` to `"gemini-3.1-flash-image-preview"` (missed in previous task).

- [ ] **Step 2: Add Quality and InputFidelity to GenerateRequest**

In `Contracts.cs`, update `GenerateRequest` record — add two optional fields:

```csharp
public record GenerateRequest(
    Guid ProjectId,
    string Prompt,
    Guid? SourceCaptureId = null,
    Guid? ParentGenerationId = null,
    string? Model = null,
    string? AspectRatio = null,
    string? Resolution = null,
    int NumImages = 1,
    string? OutputFormat = null,
    List<Guid>? ReferenceImageIds = null,
    List<MaskLayerData>? MaskLayers = null,
    MaskPayloadData? MaskPayload = null,
    string? Quality = null,
    string? InputFidelity = null
);
```

- [ ] **Step 3: Commit**

```bash
git add src/RhinoImageStudio.Shared/
git commit --author="Claude <claude@anthropic.com>" -m "feat(shared): add Seedream/GPT-Image constants and DTO fields"
```

---

### Task 2: Update `models.ts` — new model entries

**Files:**
- Modify: `src/RhinoImageStudio.UI/src/lib/models.ts`

- [ ] **Step 1: Add Seedream AR constant**

After existing AR constants, add:

```typescript
// Seedream v5 Lite Edit presets
const SEEDREAM_IMAGE_SIZES: AspectRatioOption[] = [
  { value: 'auto_2K', label: 'Auto 2K', ratio: undefined },
  { value: 'auto_3K', label: 'Auto 3K', ratio: undefined },
  { value: 'square_hd', label: 'Square HD', ratio: 1 },
  { value: 'square', label: 'Square', ratio: 1 },
  { value: 'portrait_4_3', label: 'Portrait 4:3', ratio: 3/4 },
  { value: 'portrait_16_9', label: 'Portrait 16:9', ratio: 9/16 },
  { value: 'landscape_4_3', label: 'Landscape 4:3', ratio: 4/3 },
  { value: 'landscape_16_9', label: 'Landscape 16:9', ratio: 16/9 },
];
```

- [ ] **Step 2: Add GPT-Image AR constant**

```typescript
// GPT-Image 1.5 Edit sizes (pixel-based)
const GPT_IMAGE_SIZES: AspectRatioOption[] = [
  { value: 'auto', label: 'Auto' },
  { value: '1024x1024', label: '1:1 (1024x1024)', ratio: 1 },
  { value: '1536x1024', label: '3:2 (1536x1024)', ratio: 3/2 },
  { value: '1024x1536', label: '2:3 (1024x1536)', ratio: 2/3 },
];
```

- [ ] **Step 3: Add qualityOptions and fidelityOptions to ModelInfo interface**

```typescript
export interface ModelInfo {
  // ... existing fields ...
  qualityOptions?: { value: string; label: string }[];
  fidelityOptions?: { value: string; label: string }[];
}
```

- [ ] **Step 4: Add Seedream model entry to MODELS**

```typescript
'fal-ai/bytedance/seedream/v5/lite/edit': {
  id: 'fal-ai/bytedance/seedream/v5/lite/edit',
  provider: 'fal',
  name: 'Seedream v5 Lite',
  shortName: 'seedream',
  description: 'ByteDance high-quality image editing (up to 3K)',
  capabilities: {
    supportsNegativePrompt: false,
    supportsSeed: false,
    supportsAspectRatio: true,
    supportsNumImages: true,
    supportsStrength: false,
    supportsReferences: true,
    supportsMasks: false,
  },
  aspectRatios: SEEDREAM_IMAGE_SIZES,
  maxReferences: 9,
},
```

- [ ] **Step 5: Add GPT-Image model entry to MODELS**

```typescript
'fal-ai/gpt-image-1.5/edit': {
  id: 'fal-ai/gpt-image-1.5/edit',
  provider: 'fal',
  name: 'GPT-Image 1.5',
  shortName: 'gpt-image',
  description: 'OpenAI image editing with quality control',
  capabilities: {
    supportsNegativePrompt: false,
    supportsSeed: false,
    supportsAspectRatio: true,
    supportsNumImages: true,
    supportsStrength: false,
    supportsReferences: true,
    supportsMasks: false,
  },
  aspectRatios: GPT_IMAGE_SIZES,
  qualityOptions: [
    { value: 'low', label: 'Low' },
    { value: 'medium', label: 'Medium' },
    { value: 'high', label: 'High' },
  ],
  fidelityOptions: [
    { value: 'low', label: 'Low' },
    { value: 'high', label: 'High' },
  ],
  maxReferences: 4,
},
```

- [ ] **Step 6: Update AVAILABLE_MODELS**

Add both models to `generate` and `refine` arrays:

```typescript
generate: ['gemini-3.1-flash-image-preview', 'gemini-3-pro-image-preview',
           'fal-ai/bytedance/seedream/v5/lite/edit', 'fal-ai/gpt-image-1.5/edit'],
refine:   ['gemini-3.1-flash-image-preview', 'gemini-3-pro-image-preview',
           'fal-ai/bytedance/seedream/v5/lite/edit', 'fal-ai/gpt-image-1.5/edit'],
```

- [ ] **Step 7: Add quality/fidelity to GenerationSettings**

```typescript
export interface GenerationSettings {
  aspectRatio: string;
  resolution: string;
  numImages: number;
  outputFormat: 'jpeg' | 'png';
  seed?: number;
  negativePrompt?: string;
  strength?: number;
  quality?: string;
  inputFidelity?: string;
}
```

- [ ] **Step 8: TypeScript check**

Run: `cd src/RhinoImageStudio.UI && npx tsc --noEmit`

- [ ] **Step 9: Commit**

```bash
git add src/RhinoImageStudio.UI/src/lib/models.ts
git commit --author="Claude <claude@anthropic.com>" -m "feat(models): add Seedream v5 and GPT-Image 1.5 model configs"
```

---

### Task 3: Update `InspectorPanel.tsx` — quality/fidelity dropdowns

**Files:**
- Modify: `src/RhinoImageStudio.UI/src/components/Studio/InspectorPanel.tsx`

- [ ] **Step 1: Add quality dropdown (conditional on model having qualityOptions)**

Find the section where aspectRatio/resolution selects are rendered. After them, add:

```tsx
{currentModel?.qualityOptions && (
  <div>
    <label className="text-xs text-muted">Quality</label>
    <select
      value={settings.generation.quality || 'high'}
      onChange={(e) => updateGenerationSettings({ quality: e.target.value })}
      className="..."  // match existing select styling
    >
      {currentModel.qualityOptions.map(opt => (
        <option key={opt.value} value={opt.value}>{opt.label}</option>
      ))}
    </select>
  </div>
)}
```

- [ ] **Step 2: Add fidelity dropdown (conditional)**

```tsx
{currentModel?.fidelityOptions && (
  <div>
    <label className="text-xs text-muted">Input Fidelity</label>
    <select
      value={settings.generation.inputFidelity || 'high'}
      onChange={(e) => updateGenerationSettings({ inputFidelity: e.target.value })}
      className="..."
    >
      {currentModel.fidelityOptions.map(opt => (
        <option key={opt.value} value={opt.value}>{opt.label}</option>
      ))}
    </select>
  </div>
)}
```

- [ ] **Step 3: Ensure quality/fidelity are passed in handleSubmit**

Check that `settings.generation` (which now includes `quality` and `inputFidelity`) is spread into the generate payload. The existing code likely does `...settings.generation` so these should flow automatically.

- [ ] **Step 4: TypeScript check**

Run: `cd src/RhinoImageStudio.UI && npx tsc --noEmit`

- [ ] **Step 5: Commit**

```bash
git add src/RhinoImageStudio.UI/src/components/Studio/InspectorPanel.tsx
git commit --author="Claude <claude@anthropic.com>" -m "feat(ui): add quality and fidelity dropdowns for GPT-Image model"
```

---

### Task 4: Update `JobProcessor.cs` — fal.ai payload builders

**Files:**
- Modify: `src/RhinoImageStudio.Backend/Services/JobProcessor.cs`

- [ ] **Step 1: Update ProcessGenerateJobAsync — add fal.ai model dispatch**

Currently the fal.ai path always uses `FalModels.NanoBananaEdit`. Change to use the model from request:

Find the fal.ai branch (the `else if` with `HasSecretAsync("fal_api_key")`). Update the payload building to check which fal.ai model is selected:

```csharp
// Determine fal.ai model ID from request
var falModelId = request.Model ?? FalModels.NanoBananaEdit;

var falInput = new Dictionary<string, object>
{
    ["prompt"] = augmentedPrompt ?? request.Prompt,
};

// Upload source image if present
if (sourceImageBytes != null)
{
    var imageUrl = await falClient.UploadImageAsync(sourceImageBytes, "source.png", cancellationToken);

    if (falModelId == FalModels.SeedreamV5LiteEdit)
    {
        // Seedream: image_urls array (source + references)
        var imageUrls = new List<string> { imageUrl };
        // Add reference images
        if (referenceImageBytes != null)
            foreach (var refBytes in referenceImageBytes)
                imageUrls.Add(await falClient.UploadImageAsync(refBytes, "ref.png", cancellationToken));
        falInput["image_urls"] = imageUrls.ToArray();
        falInput["image_size"] = request.AspectRatio ?? "auto_2K";
    }
    else if (falModelId == FalModels.GptImage15Edit)
    {
        // GPT-Image: image_urls array
        var imageUrls = new List<string> { imageUrl };
        if (referenceImageBytes != null)
            foreach (var refBytes in referenceImageBytes)
                imageUrls.Add(await falClient.UploadImageAsync(refBytes, "ref.png", cancellationToken));
        falInput["image_urls"] = imageUrls.ToArray();
        falInput["image_size"] = request.AspectRatio ?? "auto";
        if (request.Quality != null) falInput["quality"] = request.Quality;
        if (request.InputFidelity != null) falInput["input_fidelity"] = request.InputFidelity;
        falInput["output_format"] = request.OutputFormat ?? "png";
    }
    else
    {
        // Legacy NanoBanana path
        falInput["image_urls"] = new[] { imageUrl };
    }
}
else if (falModelId == FalModels.SeedreamV5LiteEdit || falModelId == FalModels.GptImage15Edit)
{
    // Text-to-image not supported for these edit models — need source
    throw new InvalidOperationException($"Model {falModelId} requires a source image");
}

falInput["num_images"] = request.NumImages;

var queueResponse = await falClient.SubmitAsync(falModelId, falInput, cancellationToken);
```

- [ ] **Step 2: Update ProcessRefineJobAsync — add model-aware dispatch**

Currently refine always uses Gemini or a hardcoded fal.ai path. Add model parameter support:

The `RefineRequest` doesn't have a `Model` field. For now, refine for fal.ai models should use the same payload as generate, with `ParentGenerationId` as source. This means the frontend needs to submit refine as a generate request with `ParentGenerationId` set. No backend change needed for refine — it goes through `ProcessGenerateJobAsync` when model is fal.ai.

Actually, check the existing flow: if refine is a separate `JobType.Refine`, then `ProcessRefineJobAsync` is called. We need to either:
a) Route fal.ai refine through generate, or
b) Add model support to refine.

For simplicity: keep refine as Gemini-only for now. The fal.ai models handle "refine" through the generate flow with `ParentGenerationId`.

- [ ] **Step 3: Build to verify**

Run: `cd src/RhinoImageStudio.Backend && dotnet build` (on Windows)

- [ ] **Step 4: Commit**

```bash
git add src/RhinoImageStudio.Backend/Services/JobProcessor.cs
git commit --author="Claude <claude@anthropic.com>" -m "feat(backend): add Seedream and GPT-Image payload builders in JobProcessor"
```

---

### Task 5: Build UI + verify

**Files:**
- Build only, no code changes

- [ ] **Step 1: Build UI**

Run: `cd src/RhinoImageStudio.UI && pnpm install && npx tsc --noEmit && pnpm run build`

- [ ] **Step 2: Commit built assets**

```bash
git add src/RhinoImageStudio.Backend/wwwroot/
git commit --author="Claude <claude@anthropic.com>" -m "build: rebuild UI with Seedream and GPT-Image models"
```
