# Display Mode Selector — Design Spec

**Date:** 2026-03-31
**Status:** Approved

## Problem

Viewport capture is hardcoded to `Shaded` display mode (`StudioPage.tsx:185`). User sees one thing in Rhino viewport, capture produces different result. No way to match capture 1:1 with active viewport or quickly switch between display modes.

## Solution

Compact dropdown next to the "New Capture" button. Default = "Viewport" (captures with whatever display mode is active in Rhino). 5 fixed override options for quick switching.

## UI Layout

```
┌──────────────────────────────────────────┐
│ [📷] New Capture          [Viewport ▾]  │
└──────────────────────────────────────────┘
```

Capture button (left, flex-grow) + display mode dropdown (right, w-auto) in the same row.

### Dropdown Options

| Value | Label | Behavior |
|-------|-------|----------|
| `viewport` | Viewport | Capture with current Rhino viewport display mode (default) |
| `Shaded` | Shaded | Override to Shaded |
| `Rendered` | Rendered | Override to Rendered |
| `Arctic` | Arctic | Override to Arctic |
| `Ghosted` | Ghosted | Override to Ghosted |
| `Pen` | Pen | Override to Pen |

Separator after "Viewport" option.

### Style (Mono-Theme)

- `border-radius: 0` (sharp corners)
- `bg-card`, `border border-border/50`, `text-sm`, `font-medium`
- `h-10` matching capture button height
- `ChevronDown` icon (lucide, 12px)
- Dark mode compatible via existing CSS vars

## Architecture

### Type Definition (`types.ts`)

```typescript
type CaptureDisplayMode = 'viewport' | 'Shaded' | 'Rendered' | 'Arctic' | 'Ghosted' | 'Pen';
```

### Frontend Changes

**`StudioPage.tsx`:**
- New state: `captureDisplayMode: CaptureDisplayMode` (default `'viewport'`)
- `handleCapture()`: pass `captureDisplayMode` instead of hardcoded `'Shaded'`
- When `'viewport'` → send string `"Current"` to RhinoBridge
- When specific mode → send mode name string as before

**`AssetsPanel.tsx`:**
- New props: `displayMode: CaptureDisplayMode`, `onDisplayModeChange: (mode: CaptureDisplayMode) => void`
- Dropdown rendered inline with capture button in the same `div.p-4.pb-2` container
- Layout changes from single button to flex row: `[Button flex-1] [Dropdown w-auto]`

### Plugin Changes (C#)

**`RhinoBridge.cs`:**
- New method `GetActiveDisplayMode()` → returns `view.ActiveViewport.DisplayMode.EnglishName`
- `CaptureViewport()`: when `displayModeStr == "Current"` → pass `null` to ViewportCaptureService

**`ViewportCaptureService.cs`:**
- Change `displayMode` param to nullable: `DisplayMode? displayMode = null`
- When `null` → use `view.CaptureToBitmap(captureSize)` (current viewport mode)
- When set → use existing `view.CaptureToBitmap(captureSize, rhinoDisplayMode)` (override)
- Read `view.ActiveViewport.DisplayMode.EnglishName` into `CaptureResult` metadata regardless

### No Enum Changes

`Shared/Enums/DisplayMode` stays unchanged. The `"Current"` convention is handled as a string in RhinoBridge before reaching the enum layer.

## Data Flow

```
User clicks "New Capture" with dropdown = "Viewport"
  → StudioPage.handleCapture() sends displayMode = "Current"
  → RhinoBridge.CaptureViewport(..., "Current")
  → ViewportCaptureService.CaptureActiveViewport(w, h, displayMode: null)
  → view.CaptureToBitmap(captureSize)  // uses current viewport mode
  → capture is 1:1 with what user sees

User clicks "New Capture" with dropdown = "Arctic"
  → StudioPage.handleCapture() sends displayMode = "Arctic"
  → RhinoBridge.CaptureViewport(..., "Arctic")
  → ViewportCaptureService.CaptureActiveViewport(w, h, DisplayMode.Arctic)
  → view.CaptureToBitmap(captureSize, arcticMode)  // override
```

## Files Changed

1. `src/RhinoImageStudio.UI/src/lib/types.ts` — add `CaptureDisplayMode` type
2. `src/RhinoImageStudio.UI/src/pages/StudioPage.tsx` — state + handleCapture logic
3. `src/RhinoImageStudio.UI/src/components/Studio/AssetsPanel.tsx` — dropdown UI + props
4. `src/RhinoImageStudio.Plugin/ViewportCaptureService.cs` — nullable displayMode, dual path
5. `src/RhinoImageStudio.Plugin/RhinoBridge.cs` — "Current" handling + GetActiveDisplayMode()
