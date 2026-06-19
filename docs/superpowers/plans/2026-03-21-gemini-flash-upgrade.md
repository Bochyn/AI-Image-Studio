# Gemini 3.1 Flash Upgrade + API Key Verification

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace `gemini-2.5-flash-image` with `gemini-3.1-flash-image-preview`, update Pro model limits, add API key verification/deletion endpoints.

**Architecture:** Config-level changes in `models.ts` (frontend SoT) + backend model detection logic + two new endpoints in `ConfigEndpoints.cs`. No new files, no schema changes.

**Tech Stack:** TypeScript/React (models.ts), C#/.NET 8 (GeminiClient.cs, JobEndpoints.cs, ConfigEndpoints.cs)

**Spec:** `docs/superpowers/specs/2026-03-21-gemini-flash-upgrade-design.md`

---

### Task 1: Update `models.ts` — Replace Flash model + update Pro limits

**Files:**
- Modify: `src/RhinoImageStudio.UI/src/lib/models.ts`

- [ ] **Step 1: Add new aspect ratios constant for 3.1 Flash**

After `GEMINI_ASPECT_RATIOS` (line 27), add:

```typescript
// Gemini 3.1 Flash supports extended aspect ratios
const GEMINI_3_1_FLASH_ASPECT_RATIOS: AspectRatioOption[] = [
  { value: '1:1', label: '1:1', ratio: 1 },
  { value: '1:4', label: '1:4', ratio: 1/4 },
  { value: '1:8', label: '1:8', ratio: 1/8 },
  { value: '2:3', label: '2:3', ratio: 2/3 },
  { value: '3:2', label: '3:2', ratio: 3/2 },
  { value: '3:4', label: '3:4', ratio: 3/4 },
  { value: '4:1', label: '4:1', ratio: 4/1 },
  { value: '4:3', label: '4:3', ratio: 4/3 },
  { value: '4:5', label: '4:5', ratio: 4/5 },
  { value: '5:4', label: '5:4', ratio: 5/4 },
  { value: '8:1', label: '8:1', ratio: 8/1 },
  { value: '9:16', label: '9:16', ratio: 9/16 },
  { value: '16:9', label: '16:9', ratio: 16/9 },
  { value: '21:9', label: '21:9', ratio: 21/9 },
];
```

- [ ] **Step 2: Add new resolutions constant for 3.1 Flash**

After `GEMINI_FLASH_RESOLUTIONS` (line 48), add:

```typescript
// Gemini 3.1 Flash supports multiple resolutions (0.5K uses "512" API value)
const GEMINI_3_1_FLASH_RESOLUTIONS: ResolutionOption[] = [
  { value: '512', label: '0.5K', pixels: 512 },
  { value: '1K', label: '1K', pixels: 1024 },
  { value: '2K', label: '2K', pixels: 2048 },
  { value: '4K', label: '4K', pixels: 4096 },
];
```

- [ ] **Step 3: Replace `gemini-2.5-flash-image` entry with `gemini-3.1-flash-image-preview`**

Replace the entire `'gemini-2.5-flash-image': { ... }` block (lines 110-130) with:

```typescript
'gemini-3.1-flash-image-preview': {
  id: 'gemini-3.1-flash-image-preview',
  provider: 'gemini',
  name: 'Gemini 3.1 Flash',
  shortName: 'gemini-flash',
  description: 'Fast image generation with extended AR and resolution support',
  capabilities: {
    supportsNegativePrompt: false,
    supportsSeed: false,
    supportsAspectRatio: true,
    supportsNumImages: true,
    supportsStrength: true,
    supportsReferences: true,
    supportsMasks: true,
  },
  aspectRatios: GEMINI_3_1_FLASH_ASPECT_RATIOS,
  resolutions: GEMINI_3_1_FLASH_RESOLUTIONS,
  maxReferences: 14,
  maxMaskLayers: 2,
  maxTotalImages: 16,
},
```

- [ ] **Step 4: Update Pro model reference limits**

In the `'gemini-3-pro-image-preview'` entry, change:

```typescript
// Old:
maxReferences: 4,
// New:
maxReferences: 11,
```

- [ ] **Step 5: Update `MODE_DEFAULTS`**

```typescript
export const MODE_DEFAULTS: Record<ModeType, string> = {
  generate: 'gemini-3.1-flash-image-preview',
  refine: 'gemini-3.1-flash-image-preview',
  multiangle: 'fal-ai/qwen-image-edit-2511-multiple-angles',
  upscale: 'fal-ai/topaz/upscale/image',
};
```

- [ ] **Step 6: Update `AVAILABLE_MODELS`**

```typescript
export const AVAILABLE_MODELS: Record<ModeType, string[]> = {
  generate: ['gemini-3.1-flash-image-preview', 'gemini-3-pro-image-preview'],
  refine: ['gemini-3.1-flash-image-preview', 'gemini-3-pro-image-preview'],
  multiangle: ['fal-ai/qwen-image-edit-2511-multiple-angles'],
  upscale: ['fal-ai/topaz/upscale/image'],
};
```

- [ ] **Step 7: Build UI to verify no TypeScript errors**

Run: `cd src/RhinoImageStudio.UI && npx tsc --noEmit`
Expected: No errors

- [ ] **Step 8: Commit**

```bash
git add src/RhinoImageStudio.UI/src/lib/models.ts
git commit --author="Claude <claude@anthropic.com>" -m "feat(models): replace gemini-2.5-flash with 3.1-flash-image-preview

New model adds extended ARs (1:4, 4:1, 1:8, 8:1), multi-resolution
(0.5K-4K), and higher reference limits (14). Pro model refs updated to 11."
```

---

### Task 2: Update `GeminiClient.cs` — model detection + default

**Files:**
- Modify: `src/RhinoImageStudio.Backend/Services/GeminiClient.cs`

- [ ] **Step 1: Update `DefaultModel` constant**

Line 30, change:

```csharp
// Old:
private const string DefaultModel = "gemini-2.5-flash-image";
// New:
private const string DefaultModel = "gemini-3.1-flash-image-preview";
```

- [ ] **Step 2: Update `supportsImageSize` logic**

Line ~133, change:

```csharp
// Old:
var supportsImageSize = model.Contains("3-pro") || model.Contains("2.5-pro");
// New:
var supportsImageSize = model.Contains("3-pro") || model.Contains("2.5-pro") || model.Contains("3.1-flash");
```

- [ ] **Step 3: Build backend to verify**

Run: `cd src/RhinoImageStudio.Backend && dotnet build`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add src/RhinoImageStudio.Backend/Services/GeminiClient.cs
git commit --author="Claude <claude@anthropic.com>" -m "feat(backend): update GeminiClient for 3.1-flash-image-preview

Update DefaultModel and supportsImageSize detection to support
new Flash model's resolution options."
```

---

### Task 3: Update `JobEndpoints.cs` — image budget validation

**Files:**
- Modify: `src/RhinoImageStudio.Backend/Endpoints/JobEndpoints.cs`

- [ ] **Step 1: Update default model name in MaskPayload validation (line 84)**

```csharp
// Old:
var selectedModel = request.Model ?? "gemini-2.5-flash-image";
// New:
var selectedModel = request.Model ?? "gemini-3.1-flash-image-preview";
```

- [ ] **Step 2: Update image budget logic in MaskPayload block (line 91-92)**

```csharp
// Old:
var isProModel = selectedModel.Contains("3-pro") || selectedModel.Contains("2.5-pro");
var maxTotalImages = isProModel ? 14 : 3;
// New:
var maxTotalImages = selectedModel.Contains("3-pro") || selectedModel.Contains("2.5-pro") ? 14
    : selectedModel.Contains("3.1-flash") ? 16
    : 3;
```

- [ ] **Step 3: Update default model name in MaskLayers validation (line 113)**

```csharp
// Old:
var selectedModel = request.Model ?? "gemini-2.5-flash-image";
// New:
var selectedModel = request.Model ?? "gemini-3.1-flash-image-preview";
```

- [ ] **Step 4: Update image budget logic in MaskLayers block (line 122-123)**

```csharp
// Old:
var isProModel = selectedModel.Contains("3-pro") || selectedModel.Contains("2.5-pro");
var maxTotalImages = isProModel ? 14 : 3;
// New:
var maxTotalImages = selectedModel.Contains("3-pro") || selectedModel.Contains("2.5-pro") ? 14
    : selectedModel.Contains("3.1-flash") ? 16
    : 3;
```

- [ ] **Step 5: Build to verify**

Run: `cd src/RhinoImageStudio.Backend && dotnet build`
Expected: Build succeeded

- [ ] **Step 6: Commit**

```bash
git add src/RhinoImageStudio.Backend/Endpoints/JobEndpoints.cs
git commit --author="Claude <claude@anthropic.com>" -m "fix(backend): update image budget validation for new Flash model

Default model changed to 3.1-flash-image-preview, max total images
updated to 16 for the new model."
```

---

### Task 4: Add API key verification + deletion endpoints

**Files:**
- Modify: `src/RhinoImageStudio.Backend/Endpoints/ConfigEndpoints.cs`

- [ ] **Step 1: Add `POST /api/config/verify-gemini-key` endpoint**

Add before `return api;` in `MapConfigEndpoints`:

```csharp
api.MapPost("/config/verify-gemini-key", async (ISecretStorage secrets, IHttpClientFactory httpClientFactory, CancellationToken ct) =>
{
    var apiKey = await secrets.GetSecretAsync("gemini_api_key");
    if (string.IsNullOrEmpty(apiKey))
        return Results.Ok(new { valid = false, error = "No Gemini API key configured" });

    try
    {
        using var httpClient = httpClientFactory.CreateClient();
        httpClient.Timeout = TimeSpan.FromSeconds(5);
        var response = await httpClient.GetAsync(
            $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}", ct);

        if (response.IsSuccessStatusCode)
            return Results.Ok(new { valid = true, error = (string?)null });

        var body = await response.Content.ReadAsStringAsync(ct);
        return Results.Ok(new { valid = false, error = $"API returned {response.StatusCode}: {body}" });
    }
    catch (TaskCanceledException)
    {
        return Results.Ok(new { valid = false, error = "Connection timed out (5s)" });
    }
    catch (HttpRequestException ex)
    {
        return Results.Ok(new { valid = false, error = $"Connection failed: {ex.Message}" });
    }
});
```

- [ ] **Step 2: Add `DELETE /api/config/secrets/gemini` endpoint**

Add after the verify endpoint:

```csharp
api.MapDelete("/config/secrets/gemini", async (ISecretStorage secrets) =>
{
    await secrets.DeleteSecretAsync("gemini_api_key");
    return Results.NoContent();
});
```

- [ ] **Step 3: Add `IHttpClientFactory` using directive if needed**

Check if `ConfigEndpoints.cs` already has access to `IHttpClientFactory`. It's registered in DI via `AddHttpClient` calls in `Program.cs`, so it's available for injection.

- [ ] **Step 4: Build to verify**

Run: `cd src/RhinoImageStudio.Backend && dotnet build`
Expected: Build succeeded

- [ ] **Step 5: Test verification endpoint manually**

Run backend, then:
```bash
curl -s -X POST http://localhost:17532/api/config/verify-gemini-key | python3 -m json.tool
```
Expected: `{ "valid": true }` or `{ "valid": false, "error": "..." }`

- [ ] **Step 6: Commit**

```bash
git add src/RhinoImageStudio.Backend/Endpoints/ConfigEndpoints.cs
git commit --author="Claude <claude@anthropic.com>" -m "feat(backend): add API key verification and deletion endpoints

POST /api/config/verify-gemini-key - tests key against Gemini API
DELETE /api/config/secrets/gemini - removes stored key from DPAPI"
```

---

### Task 5: Build UI + full integration test

**Files:**
- Modify: `src/RhinoImageStudio.UI/` (build only, no code changes)

- [ ] **Step 1: Install UI dependencies (if needed) and build**

Run: `cd src/RhinoImageStudio.UI && pnpm install && npx tsc --noEmit && pnpm run build`
Expected: Build succeeds, output in `../RhinoImageStudio.Backend/wwwroot/`

- [ ] **Step 2: Build full solution**

Run: `cd src && dotnet build RhinoImageStudio.sln`
Expected: Build succeeded

- [ ] **Step 3: Commit built UI assets**

```bash
git add src/RhinoImageStudio.Backend/wwwroot/
git commit --author="Claude <claude@anthropic.com>" -m "build: rebuild UI with new model configuration"
```
