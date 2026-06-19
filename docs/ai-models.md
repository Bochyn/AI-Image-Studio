# Supported AI Models

Rhino Image Studio uses several AI models for image generation and processing. Each model has its own use case and parameters.

## Model Overview

| Model | Provider | Use case | Main parameters | References | Masks |
|-------|----------|----------|------------------|------------|-------|
| Gemini 3.1 Flash | Google | Generate / Edit / Inpainting | Aspect Ratio, Resolution | Max 14 | Max 2 |
| Gemini 3 Pro | Google | Generate / Edit / Inpainting | Aspect Ratio, Resolution | Max 11 | Max 8 |
| Seedream v5 Lite | fal.ai (ByteDance) | Image editing | Image Size presets | Max 9 | – |
| GPT-Image 1.5 | fal.ai (OpenAI) | Image editing | Quality, Fidelity | Max 4 | – |
| Qwen Multi-Angle | fal.ai | Camera angle change | Rotation, Elevation, Zoom | – | – |
| Topaz Upscale | fal.ai | Upscaling | Factor, Model type | – | – |

---

## Gemini 3.1 Flash

**ID:** `gemini-3.1-flash-image-preview`
**Provider:** Google DeepMind
**Modes:** Generate, Refine (Edit)

The default generation model — fast, with extended resolution and aspect ratio support.

### Available Aspect Ratios

| Value | Proportions |
|-------|-------------|
| `1:1` | Square |
| `1:4` | Ultra-portrait |
| `1:8` | Extreme portrait |
| `2:3` | Portrait |
| `3:2` | Landscape |
| `3:4` | Portrait |
| `4:1` | Ultra-landscape |
| `4:3` | Landscape |
| `4:5` | Portrait |
| `5:4` | Landscape |
| `8:1` | Extreme landscape |
| `9:16` | Stories, Reels |
| `16:9` | Widescreen |
| `21:9` | Cinematic |

### Available Resolutions

| Value | Pixels | Use case |
|-------|--------|----------|
| `512` | 512px | Quick draft (0.5K) |
| `1K` | 1024px | Preview, iteration |
| `2K` | 2048px | Presentations, web |
| `4K` | 4096px | Print, final renders |

### Capabilities

- Multiple variant generation
- Input image strength control
- **Reference Images** — up to 14 reference images
- **Mask Inpainting** — up to 2 mask layers (2-image overlay)
- No negative prompt support

---

## Gemini 3 Pro (Preview)

**ID:** `gemini-3-pro-image-preview`
**Provider:** Google DeepMind
**Modes:** Generate, Refine (Edit)

The main model for architectural visualization. Turns viewport captures into photorealistic renders.

### Available Aspect Ratios

| Value | Proportions | Typical use |
|-------|-------------|-------------|
| `1:1` | Square | Instagram, icons |
| `2:3` | Portrait | Posters, portraits |
| `3:2` | Landscape | Classic photography |
| `3:4` | Portrait | Portrait photo |
| `4:3` | Landscape | Presentations |
| `4:5` | Portrait | Instagram portrait |
| `5:4` | Landscape | Print photo |
| `9:16` | Portrait | Stories, Reels |
| `16:9` | Landscape | Widescreen, YouTube |
| `21:9` | Ultra-wide | Cinematic |

### Available Resolutions

| Value | Pixels | Use case |
|-------|--------|----------|
| `1K` | 1024px | Quick preview, iteration |
| `2K` | 2048px | Presentations, web |
| `4K` | 4096px | Print, final renders |

### Capabilities

- Negative prompt (excluding elements)
- Multiple variant generation (1–4 images)
- Input image strength control
- **Reference Images** — up to 11 reference images (materials, objects, style)
- **Mask Inpainting** — up to 8 mask layers (2-image overlay: source + overlay + max 4 refs)

---

## Seedream v5 Lite (Edit)

**ID:** `fal-ai/bytedance/seedream/v5/lite/edit`
**Provider:** fal.ai (ByteDance)
**Modes:** Generate, Refine (Edit)
**Price:** $0.035 / image

ByteDance image editing model with multi-input support. Accepts up to 10 input images (source + references) and resolutions up to 3K.

### Image Size Presets

| API value | Label | Proportions |
|-----------|-------|-------------|
| `auto_2K` | Auto 2K | Automatic |
| `auto_3K` | Auto 3K | Automatic |
| `square_hd` | 1:1 HD | 1:1 |
| `square` | 1:1 | 1:1 |
| `portrait_4_3` | 3:4 | 3:4 |
| `portrait_16_9` | 9:16 | 9:16 |
| `landscape_4_3` | 4:3 | 4:3 |
| `landscape_16_9` | 16:9 | 16:9 |

### Capabilities

- **Reference Images** — up to 9 (10 total minus source). In the prompt, references are described as "Figure 1", "Figure 2" and so on.
- Generate up to 6 variants (`num_images`)
- No support for: input seed, strength, negative prompt, masks
- Output: PNG only

### API Payload

```json
{
  "prompt": "...",
  "image_urls": ["source_url", "ref1_url", "ref2_url"],
  "image_size": "auto_2K",
  "num_images": 1
}
```

---

## GPT-Image 1.5 (Edit)

**ID:** `fal-ai/gpt-image-1.5/edit`
**Provider:** fal.ai (OpenAI)
**Modes:** Generate, Refine (Edit)
**Price:** $0.009–$0.20 / image (depending on quality and size)

A fal.ai wrapper around OpenAI GPT-Image 1 with quality and fidelity controls.

### Image Size

| API value | Proportions |
|-----------|-------------|
| `auto` | Match source |
| `1024x1024` | 1:1 |
| `1536x1024` | 3:2 |
| `1024x1536` | 2:3 |

### Quality

| Value | Description | Price (1024x1024) |
|-------|-------------|-------------------|
| `low` | Fast, cheap | $0.009 |
| `medium` | Balanced | $0.034 |
| `high` | Best quality | $0.133 |

### Input Fidelity

| Value | Description |
|-------|-------------|
| `low` | Lower fidelity (fewer tokens, cheaper) |
| `high` | Maximum fidelity to source (default) |

### Capabilities

- **Quality control** — 3 quality tiers
- **Input fidelity** — control over source fidelity
- **Reference Images** — up to 4 via `image_urls`
- Generate up to 4 variants
- Output: JPEG, PNG, WebP
- No support for: seed, strength, negative prompt
- Native masks (`mask_image_url`) exist in the API but are unused in RIS

### API Payload

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

---

## Qwen Multi-Angle

**ID:** `fal-ai/qwen-image-edit-2511-multiple-angles`
**Provider:** fal.ai
**Mode:** Pan (Move Camera)

A model for generating views of an object from different camera angles. Maintains visual consistency across perspectives.

### Parameters

| Parameter | UI range | API range | Default |
|-----------|----------|-----------|---------|
| Camera Rotation (H) | -180° to +180° | 0° to 360° | 0° (Front) |
| Camera Elevation (V) | -30° to +90° | -30° to +90° | 0° (Eye Level) |
| Camera Distance | 0 to 10 | 0 to 10 | 5 (Medium) |
| LoRA Scale | 0 to 1 | 0 to 1 | 0.8 |

### Angle Conversion

The UI uses an intuitive -180° to +180° range (left/right) which is automatically converted to the API format (0° to 360°).

| Position | UI | API |
|----------|-----|-----|
| Front | 0° | 0° |
| Right | 90° | 90° |
| Back | 180° or -180° | 180° |
| Left | -90° | 270° |

### Quick Presets

Predefined camera positions:

- **Front** — front view (0°, 0°)
- **Right** — right view (90°, 0°)
- **Back** — back view (180°, 0°)
- **Left** — left view (-90°, 0°)
- **3/4 Right** — right-front perspective (45°, 20°)
- **3/4 Left** — left-front perspective (-45°, 20°)
- **Top Down** — top view (0°, 90°)
- **Low Angle** — low view (0°, -30°)

---

## Topaz Upscale

**ID:** `fal-ai/topaz/upscale/image`
**Provider:** fal.ai
**Mode:** Upscale

Professional image upscaling powered by Topaz Labs technology. Adds detail while preserving sharpness.

### Available Models

| Model | Use case |
|-------|----------|
| Standard V2 | Universal, default |
| High Fidelity V2 | Maximum detail fidelity |
| Graphics | Graphics, illustrations |
| Low Resolution V2 | Very low source |
| CG | 3D renders, CGI |

### Parameters

| Parameter | Values | Default |
|-----------|--------|---------|
| Factor | 2x, 4x | 2x |
| Face Enhancement | On/Off | Off |
| Output Format | JPEG, PNG | JPEG |

---

## Reference Images

Both Gemini models (Flash and Pro) support **reference images** — additional images that the AI model uses as visual context during generation.

### Use cases
- Materials and textures (e.g. wood, marble)
- Objects to insert (e.g. furniture, vehicles)
- Visual style (e.g. inspiration photo)
- Architectural elements (e.g. facade details)

### How to use
1. Pick a Gemini model (Flash or Pro).
2. Click the **Reference Images** button (`ImagePlus` icon) in the canvas toolbar.
3. Add reference images (drag & drop or click `+`).
4. In the prompt, describe how the model should use the references, e.g.:
   *"Modern office interior with the wooden texture from reference applied to walls and the car placed in the center"*
5. Click Generate.

### Limits
- Limits depend on the model (4–14 references)
- Max 10 MB per file
- Supported formats: JPG, PNG, WebP
- References persist when switching models

### Technical details
- Upload: `POST /api/projects/{projectId}/references` (multipart)
- References are sent as `inline_data` parts[] in the Gemini API request
- Files are stored under `%LOCALAPPDATA%/RhinoImageStudio/data/references/`

---

## Multi-Mask Inpainting

Both Gemini models support **mask drawing** — selecting specific image regions for editing. Each mask has its own instruction, and Gemini only edits the masked regions.

### How to use

1. Pick a Gemini model (Flash or Pro).
2. Pick a capture or generation as the source.
3. In the **Mask Layers** section of the Editor panel, click **Add** to add a mask layer.
4. Click **Draw** to enter draw mode.
5. Paint a mask on the image (white = edit, transparent = keep).
6. Type an instruction for the mask, e.g. *"Replace with wooden texture"*.
7. Add more masks for other regions (optional).
8. In the main prompt, describe the overall context.
9. Click **Generate**.

### Mask Limits

| Model | Max masks | Max total images | Formula (2-image overlay) |
|-------|-----------|------------------|---------------------------|
| Gemini 3.1 Flash | 2 | 16 | source(1) + overlay(1) + refs ≤ 16 |
| Gemini 3 Pro | 8 | 14 | source(1) + overlay(1) + refs ≤ 14 |
| Seedream | 0 | – | Masks not supported |
| GPT-Image 1.5 | 0 | – | Masks not supported (prompt engineering) |
| fal.ai (other) | 0 | – | Masks not supported |

In the 2-image overlay pipeline, masks always occupy **2 slots** (source + overlay) regardless of how many mask layers there are. References share the budget with this fixed cost. The function `getAvailableMaskSlots(modelId, refCount)` returns `maxMaskLayers` when `2 + refCount ≤ maxTotalImages`, otherwise `0`.

### Drawing Tools

- **Brush** — paint a mask (round brush, 5–200 px)
- **Eraser** — erase parts of the mask
- **Undo/Redo** — Ctrl+Z / Ctrl+Shift+Z (20 steps for 1K, 10 for 4K)
- **Layer colors** — 8 colors (red, blue, green, yellow, purple, orange, cyan, pink)

### Interaction with other modes

- Mask mode and Compare mode are mutually exclusive
- Masks are cleared when the selected item (capture/generation) changes
- Masks are trimmed when a model/reference change reduces the available slots

### Technical Details (2-image colored overlay pipeline)

- The frontend exports masks as a **colored overlay** — original + masks at 65 % fill opacity + 3 px outline (`exportMasksAsOverlay()`).
- The backend sends **two separate images** to Gemini:
  - **IMAGE 1:** clean original (no markings) — base for editing
  - **IMAGE 2:** colored overlay — visual guide WHERE to edit
- Mask data is sent in `GenerateRequest.MaskPayload` (`MaskPayloadData`):
  - `OverlayImageBase64` — overlay PNG (original + colored masks)
  - `Layers` — list of `MaskOverlayLayerData(Color, ColorName, Instruction)`
- The backend builds the augmented prompt as:
  ```
  IMAGE 1 is the ORIGINAL clean photograph/render (no markings).
  IMAGE 2 is the SAME image with colored overlay annotations.
  EDITING INSTRUCTIONS BY COLOR:
  - RED (#e74c3c) regions: [mask 1 instruction]
  - BLUE (#3498db) regions: [mask 2 instruction]
  Overall scene context: [user prompt]
  ```
- The legacy B&W format (`GenerateRequest.MaskLayers`) is kept for backwards compatibility.
- Masks are not persisted in the DB — they're ephemeral, per request (read from `Job.RequestJson`).
- Drawing happens on an offscreen canvas at source resolution (not screen resolution).

---

## API Key Requirements

### Google Gemini
- Required for: Generate, Refine
- Get a key: [Google AI Studio](https://aistudio.google.com/)
- Save under: Settings → Gemini API Key

### fal.ai
- Required for: Generate/Refine (Seedream, GPT-Image), Pan (Multi-Angle), Upscale
- Get a key: [fal.ai Console](https://fal.ai/dashboard)
- Save under: Settings → fal.ai API Key

---

## Adding New Models

Developers can add new models in `src/RhinoImageStudio.UI/src/lib/models.ts`:

```typescript
export const MODELS: Record<string, ModelInfo> = {
  'new-model-id': {
    id: 'new-model-id',
    provider: 'fal',
    name: 'New Model Name',
    shortName: 'new-model',
    description: 'Description of the model',
    capabilities: {
      supportsNegativePrompt: false,
      supportsSeed: true,
      supportsAspectRatio: true,
      supportsNumImages: false,
      supportsStrength: false,
      supportsReferences: false,
      supportsMasks: false,
    },
    aspectRatios: [...], // optional
    resolutions: [...],  // optional
    maxReferences: 4,    // optional — max reference images
    maxMaskLayers: 2,    // optional — max mask layers
    maxTotalImages: 3,   // optional — max images per request
    qualityOptions: [...],   // optional — quality options (GPT-Image)
    fidelityOptions: [...],  // optional — fidelity options (GPT-Image)
  },
};
```

Then add the model to the appropriate mode in `AVAILABLE_MODELS`.
