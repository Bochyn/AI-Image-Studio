# Wspierane Modele AI

Rhino Image Studio wykorzystuje różne modele AI do generowania i przetwarzania obrazów. Każdy model ma swoje specyficzne zastosowanie i parametry.

## Przegląd Modeli

| Model | Provider | Zastosowanie | Główne parametry | Referencje | Maski |
|-------|----------|--------------|------------------|------------|-------|
| Gemini 3.1 Flash | Google | Generowanie / Edycja / Inpainting | Aspect Ratio, Resolution | Max 14 | Max 2 |
| Gemini 3 Pro | Google | Generowanie / Edycja / Inpainting | Aspect Ratio, Resolution | Max 11 | Max 8 |
| Seedream v5 Lite | fal.ai (ByteDance) | Edycja obrazów | Image Size presets | Max 9 | - |
| GPT-Image 1.5 | fal.ai (OpenAI) | Edycja obrazów | Quality, Fidelity | Max 4 | - |
| Qwen Multi-Angle | fal.ai | Zmiana kąta kamery | Rotation, Elevation, Zoom | - | - |
| Topaz Upscale | fal.ai | Powiększanie | Factor, Model type | - | - |

---

## Gemini 3.1 Flash

**ID:** `gemini-3.1-flash-image-preview`
**Provider:** Google DeepMind
**Tryby:** Generate, Refine (Edit)

Domyślny model do generowania — szybki z rozszerzonym wsparciem rozdzielczości i aspect ratios.

### Dostępne Aspect Ratios

| Wartość | Proporcje |
|---------|-----------|
| `1:1` | Kwadrat |
| `1:4` | Ultra-pionowy |
| `1:8` | Ekstremalnie pionowy |
| `2:3` | Pionowy |
| `3:2` | Poziomy |
| `3:4` | Pionowy |
| `4:1` | Ultra-poziomy |
| `4:3` | Poziomy |
| `4:5` | Pionowy |
| `5:4` | Poziomy |
| `8:1` | Ekstremalnie poziomy |
| `9:16` | Stories, Reels |
| `16:9` | Widescreen |
| `21:9` | Cinematic |

### Dostępne Rozdzielczości

| Wartość | Piksele | Zastosowanie |
|---------|---------|--------------|
| `512` | 512px | Szybki draft (0.5K) |
| `1K` | 1024px | Podgląd, iteracje |
| `2K` | 2048px | Prezentacje, web |
| `4K` | 4096px | Druk, finalne rendery |

### Możliwości

- Generowanie wielu wariantów
- Kontrola wpływu input image (strength)
- **Reference Images** — do 14 obrazów referencyjnych
- **Mask Inpainting** — do 2 warstw masek (2-image overlay)
- Brak obsługi negative prompt

---

## Gemini 3 Pro (Preview)

**ID:** `gemini-3-pro-image-preview`
**Provider:** Google DeepMind
**Tryby:** Generate, Refine (Edit)

Główny model do generowania wizualizacji architektonicznych. Przekształca viewport capture w fotorealistyczne rendery.

### Dostępne Aspect Ratios

| Wartość | Proporcje | Typowe zastosowanie |
|---------|-----------|---------------------|
| `1:1` | Kwadrat | Instagram, ikony |
| `2:3` | Pionowy | Portret, plakaty |
| `3:2` | Poziomy | Fotografia klasyczna |
| `3:4` | Pionowy | Portrait photo |
| `4:3` | Poziomy | Prezentacje |
| `4:5` | Pionowy | Instagram portrait |
| `5:4` | Poziomy | Print photo |
| `9:16` | Pionowy | Stories, Reels |
| `16:9` | Poziomy | Widescreen, YouTube |
| `21:9` | Ultra-wide | Cinematic |

### Dostępne Rozdzielczości

| Wartość | Piksele | Zastosowanie |
|---------|---------|--------------|
| `1K` | 1024px | Szybki podgląd, iteracje |
| `2K` | 2048px | Prezentacje, web |
| `4K` | 4096px | Druk, finalne rendery |

### Możliwości

- Negative prompt (wykluczanie elementów)
- Generowanie wielu wariantów (1-4 obrazy)
- Kontrola wpływu input image (strength)
- **Reference Images** — do 11 obrazów referencyjnych (materiały, obiekty, styl)
- **Mask Inpainting** — do 8 warstw masek (2-image overlay: source + overlay + max 4 ref)

---

## Seedream v5 Lite (Edit)

**ID:** `fal-ai/bytedance/seedream/v5/lite/edit`
**Provider:** fal.ai (ByteDance)
**Tryby:** Generate, Refine (Edit)
**Cena:** $0.035 / obraz

Model ByteDance do edycji obrazów z wieloma inputami. Obsługuje do 10 obrazów wejściowych (source + referencje) i rozdzielczość do 3K.

### Image Size Presets

| Wartość API | Label | Proporcje |
|-------------|-------|-----------|
| `auto_2K` | Auto 2K | Automatyczne |
| `auto_3K` | Auto 3K | Automatyczne |
| `square_hd` | 1:1 HD | 1:1 |
| `square` | 1:1 | 1:1 |
| `portrait_4_3` | 3:4 | 3:4 |
| `portrait_16_9` | 9:16 | 9:16 |
| `landscape_4_3` | 4:3 | 4:3 |
| `landscape_16_9` | 16:9 | 16:9 |

### Możliwości

- **Reference Images** — do 9 obrazów (10 total minus source). W prompcie referencje opisane jako "Figure 1", "Figure 2" itd.
- Generowanie do 6 wariantów (`num_images`)
- Brak obsługi: seed (input), strength, negative prompt, maski
- Output: tylko PNG

### Payload API

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
**Tryby:** Generate, Refine (Edit)
**Cena:** $0.009–$0.20 / obraz (zależnie od quality i rozmiaru)

Wrapper fal.ai na OpenAI GPT-Image 1 z kontrolą jakości i fidelity.

### Image Size

| Wartość API | Proporcje |
|-------------|-----------|
| `auto` | Dopasowanie do źródła |
| `1024x1024` | 1:1 |
| `1536x1024` | 3:2 |
| `1024x1536` | 2:3 |

### Quality

| Wartość | Opis | Cena (1024x1024) |
|---------|------|-------------------|
| `low` | Szybka, tania | $0.009 |
| `medium` | Balans | $0.034 |
| `high` | Najwyższa jakość | $0.133 |

### Input Fidelity

| Wartość | Opis |
|---------|------|
| `low` | Mniejsza wierność (mniej tokenów, tańsze) |
| `high` | Maksymalna wierność źródła (domyślna) |

### Możliwości

- **Quality control** — 3 poziomy jakości
- **Input fidelity** — kontrola wierności źródła
- **Reference Images** — do 4 obrazów via `image_urls`
- Generowanie do 4 wariantów
- Output: JPEG, PNG, WebP
- Brak obsługi: seed, strength, negative prompt
- Maski natywne (`mask_image_url`) dostępne w API, ale nieużywane w RIS

### Payload API

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
**Tryb:** Pan (Move Camera)

Model do generowania widoków obiektu z różnych kątów kamery. Zachowuje spójność wizualną między różnymi perspektywami.

### Parametry

| Parametr | Zakres UI | Zakres API | Domyślna |
|----------|-----------|------------|----------|
| Camera Rotation (H) | -180° do +180° | 0° do 360° | 0° (Front) |
| Camera Elevation (V) | -30° do +90° | -30° do +90° | 0° (Eye Level) |
| Camera Distance | 0 do 10 | 0 do 10 | 5 (Medium) |
| LoRA Scale | 0 do 1 | 0 do 1 | 0.8 |

### Konwersja kątów

UI używa intuicyjnego zakresu -180° do +180° (lewo/prawo), który jest automatycznie konwertowany do formatu API (0° do 360°).

| Pozycja | UI | API |
|---------|-----|-----|
| Front | 0° | 0° |
| Right | 90° | 90° |
| Back | 180° lub -180° | 180° |
| Left | -90° | 270° |

### Quick Presets

Dostępne predefiniowane pozycje kamery:

- **Front** - widok z przodu (0°, 0°)
- **Right** - widok z prawej (90°, 0°)
- **Back** - widok z tyłu (180°, 0°)
- **Left** - widok z lewej (-90°, 0°)
- **3/4 Right** - perspektywa prawy przód (45°, 20°)
- **3/4 Left** - perspektywa lewy przód (-45°, 20°)
- **Top Down** - widok z góry (0°, 90°)
- **Low Angle** - widok z dołu (0°, -30°)

---

## Topaz Upscale

**ID:** `fal-ai/topaz/upscale/image`
**Provider:** fal.ai
**Tryb:** Upscale

Profesjonalne powiększanie obrazów z wykorzystaniem technologii Topaz Labs. Dodaje detale przy zachowaniu ostrości.

### Dostępne Modele

| Model | Zastosowanie |
|-------|--------------|
| Standard V2 | Uniwersalny, domyślny |
| High Fidelity V2 | Maksymalna wierność detali |
| Graphics | Grafika, ilustracje |
| Low Resolution V2 | Bardzo niskie źródło |
| CG | Rendery 3D, CGI |

### Parametry

| Parametr | Wartości | Domyślna |
|----------|----------|----------|
| Factor | 2x, 4x | 2x |
| Face Enhancement | On/Off | Off |
| Output Format | JPEG, PNG | JPEG |

---

## Reference Images (Referencje)

Oba modele Gemini (Flash i Pro) obsługują **obrazy referencyjne** — dodatkowe obrazy, które model AI wykorzystuje jako kontekst wizualny podczas generowania.

### Zastosowania
- Materiały i tekstury (np. drewno, marmur)
- Obiekty do wstawienia (np. meble, pojazdy)
- Styl wizualny (np. zdjęcie inspiracyjne)
- Elementy architektury (np. detale elewacji)

### Jak używać
1. Wybierz model Gemini (Flash lub Pro)
2. Kliknij przycisk **Reference Images** (ikona `ImagePlus`) w toolbarze canvasu
3. Dodaj obrazy referencyjne (drag & drop lub kliknij `+`)
4. W prompcie opisz jak model ma użyć referencji, np.:
   *"Modern office interior with the wooden texture from reference applied to walls and the car placed in the center"*
5. Kliknij Generate

### Limity
- Limity zależą od modelu (4-14 referencji)
- Max 10MB per plik
- Obsługiwane formaty: JPG, PNG, WebP
- Referencje są zachowywane przy przełączaniu modeli

### Techniczne detale
- Upload: `POST /api/projects/{projectId}/references` (multipart)
- Referencje wysyłane jako `inline_data` parts[] w Gemini API request
- Pliki przechowywane w `%LOCALAPPDATA%/RhinoImageStudio/data/references/`

---

## Multi-Mask Inpainting (Maski)

Oba modele Gemini obsługują **rysowanie masek** — zaznaczanie konkretnych obszarów obrazu do edycji. Każda maska ma własną instrukcję, a Gemini edytuje tylko zamaskowane regiony.

### Jak używać

1. Wybierz model Gemini (Flash lub Pro)
2. Wybierz capture lub generację jako źródło
3. W sekcji **Mask Layers** w panelu Editor kliknij **Add** aby dodać warstwę maski
4. Kliknij **Draw** aby wejść w tryb rysowania
5. Narysuj maskę na obrazie (biały = edytuj, przeźroczysty = zachowaj)
6. Wpisz instrukcję dla maski, np. *"Replace with wooden texture"*
7. Dodaj kolejne maski dla innych regionów (opcjonalnie)
8. W głównym prompcie opisz ogólny kontekst
9. Kliknij **Generate**

### Limity masek

| Model | Max masek | Max obrazów total | Formuła (2-image overlay) |
|-------|-----------|-------------------|---------------------------|
| Gemini 3.1 Flash | 2 | 16 | source(1) + overlay(1) + refs ≤ 16 |
| Gemini 3 Pro | 8 | 14 | source(1) + overlay(1) + refs ≤ 14 |
| Seedream | 0 | - | Maski nieobsługiwane |
| GPT-Image 1.5 | 0 | - | Maski nieobsługiwane (prompt engineering) |
| fal.ai (inne) | 0 | - | Maski nieobsługiwane |

W pipeline 2-image overlay, maski zajmują stale **2 sloty** (source + overlay) niezależnie od liczby warstw masek. Referencje dzielą budżet z tym stałym kosztem. Funkcja `getAvailableMaskSlots(modelId, refCount)` zwraca `maxMaskLayers` gdy `2 + refCount ≤ maxTotalImages`, w przeciwnym razie `0`.

### Narzędzia rysowania

- **Brush** — rysowanie maski (pędzel okrągły, rozmiar 5-200px)
- **Eraser** — wymazywanie fragmentów maski
- **Undo/Redo** — Ctrl+Z / Ctrl+Shift+Z (20 kroków dla 1K, 10 dla 4K)
- **Kolory warstw** — 8 kolorów (czerwony, niebieski, zielony, żółty, fioletowy, pomarańczowy, cyjan, różowy)

### Interakcja z innymi trybami

- Mask mode i Compare mode wzajemnie się wykluczają
- Maski są czyszczone przy zmianie wybranego elementu (capture/generacja)
- Maski są przycinane gdy zmiana modelu/referencji zmniejsza dostępne sloty

### Techniczne detale (2-image colored overlay pipeline)

- Frontend eksportuje maski jako **kolorowy overlay** — oryginał + maski z 65% opacity fill + 3px obrys (`exportMasksAsOverlay()`)
- Backend wysyła **2 osobne obrazy** do Gemini:
  - **IMAGE 1:** czysty oryginał (bez masek) — baza do edycji
  - **IMAGE 2:** kolorowy overlay — wizualny guide WHERE to edit
- Dane masek wysyłane w `GenerateRequest.MaskPayload` (`MaskPayloadData`):
  - `OverlayImageBase64` — overlay PNG (oryginał + kolorowe maski)
  - `Layers` — lista `MaskOverlayLayerData(Color, ColorName, Instruction)`
- Backend buduje augmented prompt w formacie:
  ```
  IMAGE 1 is the ORIGINAL clean photograph/render (no markings).
  IMAGE 2 is the SAME image with colored overlay annotations.
  EDITING INSTRUCTIONS BY COLOR:
  - RED (#e74c3c) regions: [instrukcja maski 1]
  - BLUE (#3498db) regions: [instrukcja maski 2]
  Overall scene context: [prompt użytkownika]
  ```
- Stary format B&W (`GenerateRequest.MaskLayers`) zachowany dla backwards compatibility
- Maski nie są persystowane w DB — efemeryczne, per-request (odczytywane z `Job.RequestJson`)
- Rysowanie na offscreen canvas w rozdzielczości źródłowej (nie ekranowej)

---

## Wymagania API Keys

### Google Gemini
- Wymagany do trybów: Generate, Refine
- Uzyskaj klucz: [Google AI Studio](https://aistudio.google.com/)
- Zapisz w: Settings → Gemini API Key

### fal.ai
- Wymagany do trybów: Generate/Refine (Seedream, GPT-Image), Pan (Multi-Angle), Upscale
- Uzyskaj klucz: [fal.ai Console](https://fal.ai/dashboard)
- Zapisz w: Settings → fal.ai API Key

---

## Dodawanie Nowych Modeli

Deweloperzy mogą dodawać nowe modele w pliku `src/RhinoImageStudio.UI/src/lib/models.ts`:

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
    aspectRatios: [...], // opcjonalne
    resolutions: [...],  // opcjonalne
    maxReferences: 4,    // opcjonalne - max obrazów referencyjnych
    maxMaskLayers: 2,    // opcjonalne - max warstw masek
    maxTotalImages: 3,   // opcjonalne - max obrazów w jednym request
    qualityOptions: [...],   // opcjonalne - opcje jakości (GPT-Image)
    fidelityOptions: [...],  // opcjonalne - opcje wierności (GPT-Image)
  },
};
```

Następnie dodaj model do odpowiedniego trybu w `AVAILABLE_MODELS`.
