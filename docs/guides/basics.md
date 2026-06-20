# Basics & Workflow

Learn how to turn a Rhino viewport into an AI visualization in a few simple steps.

## Interface Overview

The AI Image Studio panel has these main sections:
1. **Canvas (Preview)** — main area showing the captured viewport or generated image.
2. **Controls (Inspector panel)** — on the right (or bottom), where you type prompts and tweak parameters.
3. **History** — strip of thumbnails of past generations.
4. **ThemeSwitch** — theme toggle (System → Dark → Light) in the top nav bar — cyclic button.

---

## Home Page

When you launch AI Image Studio you land on the home page with two tabs: **My Projects** and **Generations**.

### "My Projects" Tab

- **Project list** — each project is a card with a thumbnail (latest generation), name and date. Projects can be filtered with the search field. Pinned projects show on top, the rest sorted newest first.
- **Create a project** — click **"+ New Project"** in the top right. In the modal, give it a name and an optional description.
- **Rename** — **double-click** a project name on its card. An edit field appears: **Enter** or click outside to confirm, **Escape** to cancel. Max 100 characters.
- **Pin** — hover a card and click the **pin** icon to pin a project at the top.
- **Delete a project** — hover a card and click the **trash** icon. A ConfirmDialog appears — deletion is irreversible.
- **Open Studio** — click a project card (single click) to open Studio with canvas, editor and history.

### "Generations" Tab

A global gallery of every generation across every project, displayed in a **masonry** layout (variable-height tiles). Sorted newest first. Each tile shows the thumbnail, project name, date and prompt. The **Load more** button fetches another batch (50 at a time). Clicking a tile opens Studio with that specific generation selected (deep link).

### Notifications (Toast)

Home-page operations (pin, rename, delete, load errors) are signaled with **toast** notifications — short messages in the corner of the screen that fade away automatically.

---

## Settings

To use AI Image Studio, you must configure API keys for the AI models you want to use.

### Opening Settings

Click the **gear** icon in the top right of the nav bar. The Settings page opens.

### API Keys

The Settings page has fields for the API keys:

- **Gemini API Key** — required for Gemini 3.1 Flash and Gemini 3 Pro. Type the key, click **Save**, then **Verify** to validate it.
- **fal.ai API Key** — required for Seedream, GPT Image 1.5/2, Qwen Multi-Angle and Topaz Upscale. Type the key and save.

Keys are stored locally in the application's encrypted storage. They are never committed to the repository and are sent only to the target provider API.

### Data Path

At the bottom of Settings the **Data Path** is shown — the location where the backend stores projects, generations and temporary files.

---

## Your First Render

### 1. Set up the Rhino view
Position the camera in Rhino the way you want the final image framed.
- The display mode can be picked right before capturing (see step 2) — you don't need to switch modes inside Rhino.
- Avoid Wireframe for complex models (too many lines can confuse the model).

### 2. Capture
The Assets panel has a **Display Mode** dropdown and a **📷 New Capture** button next to each other.

**Display Mode** controls which Rhino display mode is used to capture the viewport:

| Option | Description |
|--------|-------------|
| **Viewport** (default) | Captures exactly what you see in the active viewport — 1:1 with the current Rhino mode |
| **Shaded** | Forces Shaded regardless of viewport settings |
| **Rendered** | Forces Rendered |
| **Arctic** | Forces Arctic (neutral, flat lighting — good for AI) |
| **Ghosted** | Forces Ghosted |
| **Pen** | Forces Pen (line-based) |

Pick the desired Display Mode, then click **📷 New Capture**. Your geometry shows up in the preview window. This is the "base" the AI will work on.

### 3. Describe the vision (Prompting)
In the "Prompt" field, describe what you want to see.
- **Good example**: *"Modern concrete villa in a pine forest, rainy mood, cinematic lighting, photorealistic, 8k"*
- **Tip**: Focus on materials, lighting and mood. The geometry comes from Rhino, so you don't need to spell it out (e.g. "house with a flat roof").

### 4. Generate
Click:
> **✨ Generate**

A progress bar shows job status. After a few-to-fifteen seconds you'll see the result.

### 5. Iterate
Don't like the result?
- Tweak the prompt (e.g. add *"sunny day"* instead of *"rainy"*).
- Adjust the AI influence strength (**Strength** in advanced settings).
- Click **Generate** again.

Every version is saved in History. You can come back to any of them at any time.

---

## Managing Generations

### Archiving
Hover a generation thumbnail in the Assets panel and click the **trash** icon — the generation gets archived (not deleted). Files stay on disk. A branded confirmation dialog shows up before the action.

### Archived Tab
Click the **Archive** icon (box) in the Assets tabs to see archived generations. For each, you have two options:
- **Restore** (green icon) — restores the generation to the main list
- **Permanent Delete** (red icon) — permanently removes the generation and its files (irreversible)

When operations fail (e.g. file permission, network issue) the app shows a toast notification with the backend message instead of a generic error. All destructive actions (delete, permanent delete) require confirmation through a ConfirmDialog with accessibility support (focus trap, aria-labels).

---

## A/B Compare

### Activation
Click the **Columns** icon in the toolbar above the canvas. The button appears once you have at least 2 images in the project.

### Choosing Images
Two rows of thumbnails appear under the slider:
- **Row A** — click a thumbnail to set it as Image A (left side / base)
- **Row B** — click a thumbnail to set it as Image B (right side / overlay)

Thumbnails are tagged with **C** (Capture) or **G** (Generation).

### Opacity Control
A **B Opacity** slider (0–100 %) sits above the slider, controlling Image B's transparency over Image A:
- **100 %** — standard comparison (left: A, right: B, sharp cut by the slider)
- **50 %** — right side shows a blend of A and B
- **0 %** — both sides show only Image A

### Exit Compare Mode
Click the columns icon in the toolbar again.

---

## Inpainting (Masks)

Inpainting lets you edit **specific regions** of an image with masks. Each mask has its own instruction — Gemini only edits the masked regions, the rest stays untouched.

### Requirements
- A Gemini model (3.1 Flash or 3 Pro) — fal.ai image-edit models (Seedream, GPT Image 1.5/2) don't support masks
- A capture or generation as the source

### Mask Limits

| Model | Max masks | Max total images | Formula |
|-------|-----------|------------------|---------|
| Gemini 3.1 Flash | 2 | 16 | source(1) + overlay(1) + refs ≤ 16 → max 14 references with masks |
| Gemini 3 Pro | 8 | 14 | source(1) + overlay(1) + refs ≤ 14 → max 11 references with masks |
| fal.ai (Seedream, GPT Image 1.5/2) | 0 | – | Masks not supported |

All masks are composited into a single overlay image (original with colored masks) — they don't take separate slots. The image budget is: `2 (source + overlay) + references ≤ maxTotalImages`.

### How to use

1. Pick a capture or generation as the source.
2. In the Editor panel, **Mask Layers** section, click **Add** to add a mask layer.
3. Click the **Paintbrush** icon in the canvas toolbar to enter draw mode.
4. Paint the mask on the image:
   - Each mask layer has an assigned **color** (red, blue, green, yellow…) — paint the area to edit in that layer's color.
   - Unpainted regions stay unchanged.
5. Type the instruction for the mask, e.g. *"Replace with wooden texture"*.
6. Add more masks for other regions (optional).
7. Use the main prompt to describe the overall context.
8. Click **Generate**.

### Drawing Tools

- **Brush** — paint a mask (round brush, 5–200 px)
- **Eraser** — erase mask fragments (toggle with right click or the toolbar button)
- **Undo/Redo** — Ctrl+Z / Ctrl+Shift+Z (20 steps for 1K, 10 for 4K)
- **Layer colors** — 8 auto-assigned colors (red, blue, green, yellow, purple, orange, cyan, pink)

### Interaction with other modes

- Mask mode and Compare mode are **mutually exclusive** — turning one on turns the other off
- Masks are cleared when the selected item (capture/generation) changes
- Masks are automatically trimmed when a model or reference change reduces the available slots

### Mask History

After generating an image with masks, the mask data (drawing + instructions) is saved with the generation request. When you revisit that generation in history:
- Masks automatically reload onto the canvas with their original layer colors and instructions
- You can edit them and regenerate

### Diagnostics (Debug)

Hover a generation thumbnail and click the **Bug** icon — a modal opens with the details of the request sent to the AI:
- Prompt, model, aspect ratio, resolution
- Source (capture/generation), references
- Masks (count, size, instructions)
- A **Copy JSON** button copies the full payload to the clipboard

### Mask Readiness Indicators

A status dot appears next to each mask layer:
- **Green** — mask ready (has both drawing and instruction)
- **Amber** — incomplete (missing drawing or instruction)

A *"X/Y masks ready"* counter appears under the Generate button.

### Tips

- Write mask instructions precisely — every mask is sent to the AI with a number and description
- Use the main prompt for the overall scene context, and masks for the local changes

---

## Reference Images

The **ReferencePanel** in the Studio view lets you add reference images that the AI uses as visual context (materials, style, surrounding objects). The panel appears automatically when the chosen model supports references.

### How to add references

- Click the **Upload** button or **drag & drop** files onto the reference panel.
- Each added image shows up as a **thumbnail** preview.
- To remove a reference, click the **X** icon on its thumbnail.

### Per-model limits

The maximum number of reference images depends on the chosen model (source: `models.ts`):

| Model | Max references |
|-------|----------------|
| Gemini 3.1 Flash | 14 |
| Gemini 3 Pro | 11 |
| Seedream v5 Lite | 9 |
| GPT-Image 1.5 | 4 |
| GPT Image 2 | 4 |

> **Note:** When using inpainting masks, the image budget becomes `source(1) + overlay(1) + references ≤ maxTotalImages`, so the effective number of references can be smaller.

### Storage

Reference images are saved **per project** and **persist between sessions** — you don't need to re-add them after restarting the app.

---

## Available AI Models

AI Image Studio supports several AI models for image generation and editing. The model is picked in the **Inspector** panel (ModelSelector) of the Studio view.

| Model | Provider | Description | Masks | References |
|-------|----------|-------------|-------|------------|
| **Gemini 3.1 Flash** | Google | Fast generation, extended AR and resolutions | Yes (max 2) | Yes (max 14) |
| **Gemini 3 Pro (Preview)** | Google | High quality, supports 2K/4K | Yes (max 8) | Yes (max 11) |
| **Seedream v5 Lite** | ByteDance (fal.ai) | High-quality image editing (up to 3K) | No | Yes (max 9) |
| **GPT-Image 1.5** | OpenAI (fal.ai) | Editing with quality and fidelity controls | No | Yes (max 4) |
| **GPT Image 2** | OpenAI route (fal.ai) | Editing with quality control and image-size presets | No | Yes (max 4) |

### GPT Image — extra options

GPT Image models expose extra parameters in the Inspector:
- **Quality** (Low / Medium / High) — quality of the generated image
- **Fidelity** (Low / High) — fidelity to the source in GPT-Image 1.5

### Inspector Panel

The Inspector on the right side of Studio is split into separate components:
- **ModeSelector** — choose mode (Generate, Refine, Pan, Upscale)
- **ModelSelector** — pick the AI model with dynamic adjustment of parameters (AR, resolution, masks) to the chosen model

---

## Advanced Features

### Pan (Move Camera)
Generates views of an object from different camera angles while keeping visual consistency.

1. Pick a capture or generated image as the source.
2. Open the **Pan** tab in the Editor panel.
3. Use **Quick Presets** (Front, Right, Back, Left, 3/4, Top, Low) or set them manually:
   - **Camera Rotation** (-180° to +180°): rotates the camera around the object (left/right)
   - **Camera Elevation** (-30° to +90°): camera height (low/high)
   - **Camera Distance** (0–10): distance (Wide/Medium/Close)
4. Click **Move Camera**.

> **Tip:** The **Reset** button restores defaults (Front, Eye Level, Medium distance).

### Upscaling
To prep an image for presentation:
1. Pick the best version.
2. Click **Upscale**.
3. The image is processed at higher resolution (e.g. 4K) with added detail.
