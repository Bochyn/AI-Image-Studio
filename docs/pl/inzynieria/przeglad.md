# Przegląd projektu

**AI Image Studio** to wtyczka do **Rhinoceros 8** (Windows + macOS), która wpina AI-assisted image generation w workflow architekta. Użytkownik robi capture viewportu 3D, maluje maski inpaintingu, dodaje reference images i dostaje wizualizacje, warianty oraz precyzyjne edycje — bez wychodzenia z Rhino.

Ten dokument jest dla **recenzentów technicznych**: rekruterów z backgroundem inżynierskim, staff engineerów, architektów.

## Problem

Typowy workflow wizualizacji rozrywa kontekst:

1. Eksport viewportu → Photoshop lub web UI → klucze API → import z powrotem do Rhino.
2. Narzędzia inpaintingu nie rozumieją **trybów wyświetlania Rhino** (Shaded, Rendered, Arctic…).
3. Pluginy CAD są **platform-specific** — bridge WebView2 z Windowsa nie przenosi się na macOS.

Cel: **local-first**, jedno okno — viewport Rhino na wejściu, obraz AI na wyjściu, historia na dysku, klucze w szyfrowanym storage OS.

## Rozwiązanie

System **hybrydowy** w trzech warstwach:

| Warstwa | Technologia | Rola |
|---------|-------------|------|
| Plugin Windows | .NET 4.8, WebView2, COM | Panel dokowany, Rhino API na wątku UI |
| Plugin macOS | .NET 8, HTTP long-poll | Ten sam UI w przeglądarce; praca Rhino przez backend |
| RhinoCommon | net48 + net8 | Jeden kod capture, upload, display queries |
| Backend | ASP.NET Core 8, SQLite | Kolejka jobów, SSE, proxy Gemini/fal.ai |
| Frontend | React 18, TypeScript, Vite | Inspector model-aware, maski, compare, galeria |

Oba systemy operacyjne dzielą **jeden backend**, **jeden build Reacta** i **jeden assembly kontraktów** (`Shared`).

## Highlighty inżynierskie

### 1. Bridge cross-platform

Windows: `RhinoBridge` w JavaScript przez **WebView2 host objects**.

macOS: brak WebView2 → **kolejka RPC w backendzie** + long-poll z pluginu + **token uwierzytelniający**.

Szczegóły: [Most cross-platform](most-cross-platform.md)

### 2. Inpainting Gemini bez natywnych masek

Pipeline **2 obrazów**: źródło + **kolorowy overlay** (każda maska innym kolorem + instrukcje w prompcie).

### 3. UI świadomy modelu

`models.ts` = single source of truth dla capabilities. Backend waliduje te same limity.

### 4. Bezpieczeństwo local-first

- Klucze API → **Data Protection** (+ migracja DPAPI na Windows)
- Bridge → `X-Rhino-Bridge-Token`
- Ochrona przed path traversal w storage

Szczegóły: [Bezpieczeństwo](bezpieczenstwo.md)

### 5. Program jakości kodu

Audyt + refaktor: SSE pub/sub, `RhinoCommon`, testy xUnit, ESLint, CI.

Szczegóły: [Jakość kodu](jakosc-kodu.md)

## O czym rozmawiać na rozmowie

| Temat | Gdzie w repo |
|-------|----------------|
| Integracja desktop cross-platform | `Plugin.RhinoCommon`, `MacRhinoBridgeClient` |
| Design API | `Shared/Contracts` |
| Async jobs + SSE | `JobProcessor`, `EventBroadcaster` |
| Security mindset | token bridge, migracja secretów |
| Dyscyplina inżynierska | audyt, testy, CI |

## Mapa dokumentacji

| Potrzeba | Dokument |
|----------|----------|
| Instalacja | [Pierwsze kroki](../pierwsze-kroki.md) |
| Pełne API | [Architektura](../api/architektura.md) |
| Audyt | [Jakość kodu](jakosc-kodu.md) |
| Kontrybucja | [CONTRIBUTING (EN)](../../CONTRIBUTING.md) |

## Status

Aktywny development. CI buduje Windows i macOS przy każdym PR.
