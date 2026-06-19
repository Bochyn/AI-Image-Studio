# Seedream v5 Lite Edit + GPT-Image 1.5 Edit Integration

**Date:** 2026-03-21
**Status:** Approved

## Summary

Add two new fal.ai models to AI Image Studio: `fal-ai/bytedance/seedream/v5/lite/edit` (Seedream) and `fal-ai/gpt-image-1.5/edit` (GPT-Image). Both available for Generate and Refine modes. No mask support (prompt engineering only). No new backend code paths for masks.

## 1. Seedream v5 Lite Edit

**Model ID:** `fal-ai/bytedance/seedream/v5/lite/edit`

### API Parameters (from OpenAPI schema)

| Parameter | Type | Required | Default | Notes |
|---|---|---|---|---|
| `prompt` | string | yes | — | Editing instructions |
| `image_urls` | string[] | yes | — | Up to 10 images. First = source, rest = references ("Figure 1", "Figure 2" in prompt) |
| `image_size` | enum or `{width, height}` | no | `auto_2K` | Presets: `square_hd`, `square`, `portrait_4_3`, `portrait_16_9`, `landscape_4_3`, `landscape_16_9`, `auto_2K`, `auto_3K`. Custom: `{width, height}` max 14142px |
| `num_images` | int | no | 1 | Range 1-6 |
| `max_images` | int | no | 1 | Multiplier with num_images, range 1-6 |
| `enable_safety_checker` | bool | no | true | — |

**Response:** `{ images: [{url, width, height, content_type}], seed: int }`

**Not supported:** seed (input), strength, guidance_scale, masks, negative_prompt

### ModelInfo config

```
id: fal-ai/bytedance/seedream/v5/lite/edit
provider: fal
aspectRatios: fal.ai presets (square_hd, landscape_4_3, etc.) + auto_2K, auto_3K
resolutions: none (handled by image_size preset)
supportsReferences: true (max 9 — 10 total minus source)
supportsSeed: false
supportsNumImages: true (1-6)
supportsMasks: false
supportsNegativePrompt: false
supportsStrength: false
supportsAspectRatio: true
```

## 2. GPT-Image 1.5 Edit

**Model ID:** `fal-ai/gpt-image-1.5/edit`

### API Parameters (from official docs)

| Parameter | Type | Required | Default | Notes |
|---|---|---|---|---|
| `prompt` | string | yes | — | min 2, max 32000 chars |
| `image_urls` | string[] | yes | — | Reference images for editing |
| `image_size` | enum | no | `auto` | Options: `auto`, `1024x1024`, `1536x1024`, `1024x1536` |
| `quality` | enum | no | `high` | `low`, `medium`, `high` |
| `input_fidelity` | enum | no | `high` | `low`, `high` |
| `num_images` | int | no | 1 | Range 1-4 |
| `output_format` | enum | no | `png` | `jpeg`, `png`, `webp` |
| `background` | enum | no | `auto` | `auto`, `transparent`, `opaque` |
| `mask_image_url` | string | no | — | Available but NOT used (per design: prompt engineering only) |

**Response:** `{ images: [{url, width, height, content_type, file_name}] }`

**Not supported:** seed, strength, guidance_scale, negative_prompt

### ModelInfo config

```
id: fal-ai/gpt-image-1.5/edit
provider: fal
aspectRatios: auto, 1024x1024 (1:1), 1536x1024 (3:2), 1024x1536 (2:3)
resolutions: none (size is in AR value)
qualityOptions: low, medium, high
fidelityOptions: low, high
supportsReferences: true (via image_urls)
supportsSeed: false
supportsNumImages: true (1-4)
supportsMasks: false
supportsNegativePrompt: false
supportsStrength: false
supportsAspectRatio: true
```

## 3. New fields in ModelInfo

```typescript
qualityOptions?: { value: string; label: string }[];
fidelityOptions?: { value: string; label: string }[];
```

InspectorPanel shows these dropdowns only when model has them.

## 4. AVAILABLE_MODELS update

```typescript
generate: ['gemini-3.1-flash-image-preview', 'gemini-3-pro-image-preview',
           'fal-ai/bytedance/seedream/v5/lite/edit', 'fal-ai/gpt-image-1.5/edit'],
refine:   ['gemini-3.1-flash-image-preview', 'gemini-3-pro-image-preview',
           'fal-ai/bytedance/seedream/v5/lite/edit', 'fal-ai/gpt-image-1.5/edit'],
```

## 5. Backend — JobProcessor.cs

New payload builders for both models in `ProcessGenerateJobAsync` and `ProcessRefineJobAsync`.

**Seedream payload:**
```json
{
  "prompt": "...",
  "image_urls": ["source_url", "ref1_url", "ref2_url"],
  "image_size": "auto_2K",
  "num_images": 1
}
```

**GPT-Image payload:**
```json
{
  "prompt": "...",
  "image_urls": ["source_url"],
  "image_size": "1024x1024",
  "quality": "high",
  "input_fidelity": "high",
  "num_images": 1,
  "output_format": "png"
}
```

Both use existing fal.ai submit/poll pattern via `FalAiClient`.

## 6. Backend — Constants.cs

```csharp
public const string SeedreamV5LiteEdit = "fal-ai/bytedance/seedream/v5/lite/edit";
public const string GptImage15Edit = "fal-ai/gpt-image-1.5/edit";
```

## 7. Backend — GenerateRequest / Contracts

Add optional fields to `GenerateRequest`:
```csharp
public string? Quality { get; set; }        // for GPT-Image
public string? InputFidelity { get; set; }   // for GPT-Image
```

## 8. Files to modify

| File | Changes |
|---|---|
| `UI/src/lib/models.ts` | New model entries, new AR constants, qualityOptions/fidelityOptions fields |
| `UI/src/lib/types.ts` | Add quality/fidelity to GenerationSettings if needed |
| `UI/src/components/Studio/InspectorPanel.tsx` | Conditional quality/fidelity dropdowns |
| `UI/src/lib/api.ts` | Pass quality/fidelity in generate request |
| `Shared/Constants/Constants.cs` | New FalModels constants |
| `Shared/Contracts/Contracts.cs` | Add Quality/InputFidelity to GenerateRequest |
| `Backend/Services/JobProcessor.cs` | New payload builders for both models |
| `Backend/Endpoints/JobEndpoints.cs` | Validation — both models without masks |

## 9. Out of Scope

- Native mask support for GPT-Image (mask_image_url) — future enhancement
- Seedream max_images multiplier UI — just use num_images
- GPT-Image background parameter UI — use auto
- GPT-Image BYOK (bring-your-own-key) endpoint
