# Gemini Flash Model Upgrade + API Key Verification

**Date:** 2026-03-21
**Status:** Approved

## Summary

Replace `gemini-2.5-flash-image` with `gemini-3.1-flash-image-preview` across the stack. Add API key verification endpoint. Update `gemini-3-pro-image-preview` reference limits.

## 1. Model Replacement in `models.ts`

Remove `gemini-2.5-flash-image` entry. Add `gemini-3.1-flash-image-preview`:

| Parameter | Old (2.5 Flash) | New (3.1 Flash) |
|---|---|---|
| **ID** | `gemini-2.5-flash-image` | `gemini-3.1-flash-image-preview` |
| **Aspect ratios** | 10 (1:1 to 21:9) | 14 — added: `1:4`, `4:1`, `1:8`, `8:1` |
| **Resolutions** | 1K only | 0.5K, 1K, 2K, 4K |
| **Max references** | 4 | 14 (10 object + 4 character) |
| **Max mask layers** | 2 | 2 (unchanged) |
| **Max total images** | 3 | 16 (source + overlay + 14 refs) |

New aspect ratios constant: `GEMINI_3_1_FLASH_ASPECT_RATIOS` — full list:
`1:1, 1:4, 1:8, 2:3, 3:2, 3:4, 4:1, 4:3, 4:5, 5:4, 8:1, 9:16, 16:9, 21:9`

New resolutions constant: `GEMINI_3_1_FLASH_RESOLUTIONS`:
- `512` (0.5K) — value sent to API is `"512"` not `"0.5K"`
- `1K` (default)
- `2K`
- `4K`

Update `MODE_DEFAULTS` — replace `gemini-2.5-flash-image` with `gemini-3.1-flash-image-preview` for `generate` and `refine`.

Update `AVAILABLE_MODELS` — replace `gemini-2.5-flash-image` with `gemini-3.1-flash-image-preview` in `generate` and `refine` arrays.

### Pro Model Update

| Parameter | Old | Updated |
|---|---|---|
| **Max references** | 4 | 11 (6 object + 5 character) |
| **Max total images** | 14 | 14 (unchanged) |

## 2. Backend — `GeminiClient.cs`

### supportsImageSize logic (line ~133)

```csharp
// Old:
var supportsImageSize = model.Contains("3-pro") || model.Contains("2.5-pro");
// New:
var supportsImageSize = model.Contains("3-pro") || model.Contains("2.5-pro") || model.Contains("3.1-flash");
```

### imageSize value mapping

New Flash model uses `"512"` for 0.5K resolution (not `"0.5K"`). The `imageSize` values `"1K"`, `"2K"`, `"4K"` remain as-is. Only `"512"` is different.

No mapping needed in GeminiClient — the UI sends the value as-is from `ResolutionOption.value`. The `models.ts` resolution option for 0.5K will have `value: "512"`.

### DefaultModel constant

Update from `gemini-2.5-flash-image` to `gemini-3.1-flash-image-preview`.

## 3. Backend — `JobEndpoints.cs` validation

Update model name checks in `/generate` endpoint:
- Line ~84: `selectedModel` default value
- Line ~91: `isProModel` check — keep as-is (checks for `3-pro` and `2.5-pro`)
- Add logic for 3.1 Flash image budget: `maxTotalImages = 16`

The existing check uses `model.Contains("3-pro")` which won't match `3.1-flash`. Need to update the image budget logic to check the new model.

## 4. API Key Verification Endpoint

### `POST /api/config/verify-gemini-key`

**Request:** empty body (reads key from DPAPI storage)

**Logic:**
1. Read `gemini_api_key` from `ISecretStorage`
2. If not set, return `{ valid: false, error: "No API key configured" }`
3. GET `https://generativelanguage.googleapis.com/v1beta/models?key={key}` with 5s timeout
4. If 200: return `{ valid: true }`
5. If 401/403: return `{ valid: false, error: "Invalid API key" }`
6. If timeout/other: return `{ valid: false, error: "Connection failed: {details}" }`

### `DELETE /api/config/secrets/gemini`

Calls `ISecretStorage.DeleteSecretAsync("gemini_api_key")`. Returns 204.

### Location

Add to `ConfigEndpoints.cs` alongside existing config endpoints.

## 5. Files to Modify

| File | Changes |
|---|---|
| `UI/src/lib/models.ts` | Replace 2.5 Flash with 3.1 Flash, update Pro refs, new AR/resolution constants |
| `Backend/Services/GeminiClient.cs` | Update `supportsImageSize` logic, update `DefaultModel` |
| `Backend/Endpoints/JobEndpoints.cs` | Update model name in validation, image budget for new model |
| `Backend/Endpoints/ConfigEndpoints.cs` | Add verify + delete key endpoints |

## 6. Out of Scope

- Thinking support (`thinkingConfig`) — future enhancement
- Image Search Grounding — future enhancement
- UI changes to Settings modal for key verification button — can be added later
- Removing old `GEMINI_ASPECT_RATIOS` / `GEMINI_FLASH_RESOLUTIONS` constants
