# Display Mode Selector — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace hardcoded `Shaded` viewport capture with a selector that defaults to the current viewport display mode and allows quick override to 5 preset modes.

**Architecture:** New `CaptureDisplayMode` type drives a compact dropdown in AssetsPanel next to the capture button. Frontend sends `"Current"` string for viewport-match mode. Plugin's `ViewportCaptureService` uses `CaptureToBitmap(Size)` (no override) when display mode is null, preserving existing override path for named modes.

**Tech Stack:** React 18, TypeScript, Tailwind CSS, .NET 4.8 (Plugin), lucide-react

---

### Task 1: Add CaptureDisplayMode type

**Files:**
- Modify: `src/RhinoImageStudio.UI/src/lib/types.ts:198` (after Capture interface)

- [ ] **Step 1: Add the type**

After line 198 (`}` closing the `Capture` interface), add:

```typescript
export type CaptureDisplayMode = 'viewport' | 'Shaded' | 'Rendered' | 'Arctic' | 'Ghosted' | 'Pen';

export const DISPLAY_MODE_OPTIONS: { value: CaptureDisplayMode; label: string }[] = [
  { value: 'viewport', label: 'Viewport' },
  { value: 'Shaded', label: 'Shaded' },
  { value: 'Rendered', label: 'Rendered' },
  { value: 'Arctic', label: 'Arctic' },
  { value: 'Ghosted', label: 'Ghosted' },
  { value: 'Pen', label: 'Pen' },
];
```

- [ ] **Step 2: Add GetActiveDisplayMode to RhinoBridge interface**

In the `RhinoBridge` interface (~line 200), add after `GetApiUrl`:

```typescript
GetActiveDisplayMode(): Promise<string>;
```

- [ ] **Step 3: Update mock bridge**

In `src/RhinoImageStudio.UI/src/lib/rhino.ts`, add to `mockRhinoBridge`:

```typescript
GetActiveDisplayMode: async () => {
  return 'Shaded';
},
```

- [ ] **Step 4: Verify TypeScript compiles**

Run: `cd src/RhinoImageStudio.UI && npx tsc --noEmit`
Expected: No errors

- [ ] **Step 5: Commit**

```bash
git add src/RhinoImageStudio.UI/src/lib/types.ts src/RhinoImageStudio.UI/src/lib/rhino.ts
git commit --author="Claude <claude@anthropic.com>" -m "feat(ui): add CaptureDisplayMode type and RhinoBridge.GetActiveDisplayMode"
```

---

### Task 2: Add display mode state and update handleCapture in StudioPage

**Files:**
- Modify: `src/RhinoImageStudio.UI/src/pages/StudioPage.tsx`

- [ ] **Step 1: Import the new type**

Add `CaptureDisplayMode` to the imports from `@/lib/types` (line 4).

- [ ] **Step 2: Add state**

After `const [captureToDeleteId, setCaptureToDeleteId]` (~line 46), add:

```typescript
const [captureDisplayMode, setCaptureDisplayMode] = useState<CaptureDisplayMode>('viewport');
```

Import `CaptureDisplayMode` from types (step 1 already covers this).

- [ ] **Step 3: Update handleCapture to use state instead of hardcoded 'Shaded'**

Replace lines 185 in `handleCapture`:

```typescript
// OLD:
const displayMode = 'Shaded';

// NEW:
const displayMode = captureDisplayMode === 'viewport' ? 'Current' : captureDisplayMode;
```

- [ ] **Step 4: Pass new props to AssetsPanel**

In the `<AssetsPanel>` JSX (~line 509), add two new props:

```tsx
<AssetsPanel
  captures={captures}
  generations={generations}
  selectedItem={selectedItem}
  onSelect={setSelectedItem}
  onCapture={handleCapture}
  onDelete={handleDelete}
  isCapturing={isCapturing}
  rhinoAvailable={rhinoAvailable}
  isCollapsed={assetsCollapsed}
  onToggleCollapse={() => setAssetsCollapsed(!assetsCollapsed)}
  archivedGenerations={archivedGenerations}
  onRestore={handleRestore}
  onPermanentDelete={handlePermanentDelete}
  onDebug={handleDebug}
  captureDisplayMode={captureDisplayMode}
  onCaptureDisplayModeChange={setCaptureDisplayMode}
/>
```

- [ ] **Step 5: Commit**

```bash
git add src/RhinoImageStudio.UI/src/pages/StudioPage.tsx
git commit --author="Claude <claude@anthropic.com>" -m "feat(ui): wire captureDisplayMode state into StudioPage"
```

---

### Task 3: Add display mode dropdown to AssetsPanel

**Files:**
- Modify: `src/RhinoImageStudio.UI/src/components/Studio/AssetsPanel.tsx`

- [ ] **Step 1: Add imports and props**

Add to imports:

```typescript
import { CaptureDisplayMode, DISPLAY_MODE_OPTIONS } from '@/lib/types';
import { ChevronDown } from 'lucide-react';
```

Add to `AssetsPanelProps` interface:

```typescript
captureDisplayMode: CaptureDisplayMode;
onCaptureDisplayModeChange: (mode: CaptureDisplayMode) => void;
```

Add to destructured props:

```typescript
captureDisplayMode,
onCaptureDisplayModeChange,
```

- [ ] **Step 2: Replace capture button section with button + dropdown row**

Replace the `{/* Capture Action */}` section (lines 204-218):

```tsx
{/* Capture Action */}
<div className="p-4 pb-2 flex gap-1.5">
  <Button
    className="flex-1 bg-card hover:bg-card-hover text-primary border border-border/50 h-10 gap-2 justify-start px-3"
    onClick={onCapture}
    disabled={!rhinoAvailable || isCapturing}
  >
    <div className="p-1 bg-primary">
      <Camera className="h-3 w-3 text-background" />
    </div>
    <span className="text-sm font-medium">
      {isCapturing ? 'Capturing...' : 'Capture'}
    </span>
  </Button>
  <div className="relative">
    <select
      value={captureDisplayMode}
      onChange={(e) => onCaptureDisplayModeChange(e.target.value as CaptureDisplayMode)}
      disabled={!rhinoAvailable || isCapturing}
      className="h-10 pl-2 pr-6 bg-card hover:bg-card-hover text-primary border border-border/50 text-xs font-medium appearance-none cursor-pointer disabled:opacity-50 disabled:cursor-not-allowed focus:outline-none focus:border-accent"
      aria-label="Capture display mode"
    >
      {DISPLAY_MODE_OPTIONS.map((opt, i) => (
        <option key={opt.value} value={opt.value}>
          {opt.label}
        </option>
      ))}
    </select>
    <ChevronDown className="absolute right-1.5 top-1/2 -translate-y-1/2 h-3 w-3 text-secondary pointer-events-none" />
  </div>
</div>
```

Key design decisions:
- Native `<select>` — lightweight, accessible, keyboard-friendly, no extra component needed
- `appearance-none` + absolute ChevronDown icon — custom look, Mono-Theme compatible
- `border-radius: 0` inherited from Mono-Theme (no rounded classes)
- `text-xs` for compact label, `h-10` matches capture button
- Button label shortened to "Capture" (from "New Capture") to save horizontal space

- [ ] **Step 3: Update collapsed view (if needed)**

Check the collapsed sidebar rendering (~line 176-192). The collapsed view shows icon-only buttons — no dropdown needed there. No changes required.

- [ ] **Step 4: Verify TypeScript compiles**

Run: `cd src/RhinoImageStudio.UI && npx tsc --noEmit`
Expected: No errors

- [ ] **Step 5: Commit**

```bash
git add src/RhinoImageStudio.UI/src/components/Studio/AssetsPanel.tsx
git commit --author="Claude <claude@anthropic.com>" -m "feat(ui): add display mode dropdown next to capture button"
```

---

### Task 4: Update ViewportCaptureService for nullable DisplayMode

**Files:**
- Modify: `src/RhinoImageStudio.Plugin/ViewportCaptureService.cs`

- [ ] **Step 1: Change displayMode parameter to nullable**

Change method signature (line 22):

```csharp
// OLD:
public static CaptureResult? CaptureActiveViewport(
    int width = 1024,
    int height = 1024,
    Shared.Enums.DisplayMode displayMode = Shared.Enums.DisplayMode.Shaded,
    bool transparentBackground = false)

// NEW:
public static CaptureResult? CaptureActiveViewport(
    int width = 1024,
    int height = 1024,
    Shared.Enums.DisplayMode? displayMode = null,
    bool transparentBackground = false)
```

- [ ] **Step 2: Update capture logic for null = current viewport mode**

Replace the capture logic block (lines 41-70):

```csharp
Bitmap? bitmap = null;
var captureSize = new Size(width, height);

if (displayMode.HasValue)
{
    // Override: use specific display mode
    var rhinoDisplayMode = GetRhinoDisplayMode(displayMode.Value);
    RhinoApp.WriteLine($"[ViewportCapture] Override mode: {displayMode.Value}, Found Rhino mode: {rhinoDisplayMode?.EnglishName ?? "NULL"}");

    if (rhinoDisplayMode != null)
    {
        bitmap = view.CaptureToBitmap(captureSize, rhinoDisplayMode);
    }
    else
    {
        // Requested mode not found, fall back to current viewport mode
        RhinoApp.WriteLine($"[ViewportCapture] Override mode not found, using current viewport mode");
        bitmap = view.CaptureToBitmap(captureSize);
    }
}
else
{
    // No override: capture with current viewport display mode (1:1)
    var currentMode = view.ActiveViewport.DisplayMode;
    RhinoApp.WriteLine($"[ViewportCapture] Using current viewport mode: {currentMode?.EnglishName ?? "unknown"}");
    bitmap = view.CaptureToBitmap(captureSize);
}
```

- [ ] **Step 3: Update CaptureResult to store actual display mode name**

Replace the `DisplayMode` assignment in the return block (~line 92):

```csharp
return new CaptureResult
{
    Bitmap = bitmap,
    Width = bitmap.Width,
    Height = bitmap.Height,
    ViewName = view.ActiveViewport.Name,
    DisplayModeName = view.ActiveViewport.DisplayMode?.EnglishName ?? displayMode?.ToString() ?? "Unknown",
    CameraPosition = $"{cameraLocation.X:F2},{cameraLocation.Y:F2},{cameraLocation.Z:F2}",
    CameraTarget = $"{cameraTarget.X:F2},{cameraTarget.Y:F2},{cameraTarget.Z:F2}",
    CameraLens = viewport.Camera35mmLensLength
};
```

Update `CaptureResult` class — replace `DisplayMode` property:

```csharp
// OLD:
public Shared.Enums.DisplayMode DisplayMode { get; set; }

// NEW:
public string DisplayModeName { get; set; } = "Unknown";
```

- [ ] **Step 4: Check for other references to CaptureResult.DisplayMode**

Search for `CaptureResult.DisplayMode` or `.DisplayMode` usage. In `RhinoBridge.cs` line 83 it sends `displayMode.ToString()` — this will be updated in Task 5.

- [ ] **Step 5: Commit**

```bash
git add src/RhinoImageStudio.Plugin/ViewportCaptureService.cs
git commit --author="Claude <claude@anthropic.com>" -m "feat(plugin): support nullable DisplayMode for current-viewport capture"
```

---

### Task 5: Update RhinoBridge for "Current" handling and GetActiveDisplayMode

**Files:**
- Modify: `src/RhinoImageStudio.Plugin/RhinoBridge.cs`

- [ ] **Step 1: Update CaptureViewport to handle "Current"**

Replace the display mode parsing in `CaptureViewport` (lines 54-58):

```csharp
// Parse display mode — "Current" means use active viewport mode
Shared.Enums.DisplayMode? displayMode = null;
if (!string.Equals(displayModeStr, "Current", StringComparison.OrdinalIgnoreCase))
{
    if (!Enum.TryParse<DisplayMode>(displayModeStr, true, out var parsed))
    {
        parsed = DisplayMode.Shaded;
    }
    displayMode = parsed;
}

RhinoApp.WriteLine($"Capturing viewport: {width}x{height}, mode={displayModeStr}");
```

Update the `ViewportCaptureService.CaptureActiveViewport` call (line 63) — pass nullable:

```csharp
var captureResult = ViewportCaptureService.CaptureActiveViewport(
    width, height, displayMode, false);
```

- [ ] **Step 2: Update the upload content to use DisplayModeName**

Replace line 83:

```csharp
// OLD:
content.Add(new StringContent(displayMode.ToString()), "displayMode");

// NEW:
content.Add(new StringContent(captureResult.DisplayModeName), "displayMode");
```

- [ ] **Step 3: Add GetActiveDisplayMode method**

After the `RunCommand` method (~line 229), add:

```csharp
/// <summary>
/// Gets the display mode of the active viewport
/// </summary>
public string GetActiveDisplayMode()
{
    string modeName = "Shaded";

    RhinoApp.InvokeOnUiThread(() =>
    {
        var view = RhinoDoc.ActiveDoc?.Views.ActiveView;
        if (view != null)
        {
            modeName = view.ActiveViewport.DisplayMode?.EnglishName ?? "Shaded";
        }
    });

    return modeName;
}
```

- [ ] **Step 4: Verify C# builds**

Run: `cd src && dotnet build RhinoImageStudio.sln`
Expected: Build succeeded (warnings OK, zero errors)

- [ ] **Step 5: Commit**

```bash
git add src/RhinoImageStudio.Plugin/RhinoBridge.cs
git commit --author="Claude <claude@anthropic.com>" -m "feat(plugin): handle 'Current' display mode and add GetActiveDisplayMode"
```

---

### Task 6: Build UI and verify

**Files:** None new — verification only.

- [ ] **Step 1: Build frontend**

Run: `cd src/RhinoImageStudio.UI && npx tsc --noEmit && pnpm run build`
Expected: Build succeeds, output in `src/RhinoImageStudio.Backend/wwwroot/`

- [ ] **Step 2: Build full solution**

Run: `cd src && dotnet build RhinoImageStudio.sln`
Expected: Build succeeded

- [ ] **Step 3: Commit spec and plan docs**

```bash
git add docs/superpowers/specs/2026-03-31-display-mode-selector-design.md docs/superpowers/plans/2026-03-31-display-mode-selector.md
git commit --author="Claude <claude@anthropic.com>" -m "docs: display mode selector spec and implementation plan"
```
