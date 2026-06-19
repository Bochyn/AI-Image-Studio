# Jakość kodu i audyt

Ustrukturyzowany audyt Rhino Image Studio i remediacja — wersja skrócona. Pełna wersja EN: [Code quality & audit](../../engineering/code-quality.md).

## Podsumowanie

| Obszar | Przed | Po (PR #22) |
|--------|-------|-------------|
| Backend | God file `Program.cs` | `Endpoints/` + `Infrastructure/` |
| SSE | Jeden kanał — gubił eventy | Pub/sub per subscriber |
| Bridge macOS | Bez auth, stubowe dane | Token + prawdziwe RPC Rhino |
| Capture | Duplikat Win/Mac | `Plugin.RhinoCommon` |
| Sekrety | DPAPI, problem na macOS | Data Protection + migracja |
| Testy | Brak | xUnit + ESLint + CI |

**Skala:** ~13 000 LOC w 6 projektach.

## Krytyczne naprawy

- **Path traversal** w `/images/` — walidacja `GetAbsolutePath`
- **Otwarty bridge** — token `X-Rhino-Bridge-Token`
- **SSE** — każdy klient ma własny kanał
- **fal.ai payload** — `FalInputBuilder`, `ProviderModelId` przy cancel
- **Windows UI thread** — `RhinoUiThread.RunAsync` zamiast `.Result`

## Fazy refaktoru

F0 (blockers) → F1 (kontrakty Shared) → F2 (bridge) → F3 (backend hygiene) → F4 (frontend) → F5 (RhinoCommon) → F6 (testy + CI).

## Otwarty dług techniczny

| Element | Priorytet |
|---------|-----------|
| Podział `InspectorPanel` / `StudioPage` | Średni |
| Więcej testów | Średni |
| Accessibility | Średni |
| `any` w ESLint (9 warningów) | Niski |

## Jak to opisać w CV

> Wtyczka Rhino 8 cross-platform (.NET + React), własny HTTP bridge na macOS, kolejka jobów, Gemini/fal.ai. Audyt kodu: SSE, security bridge, RhinoCommon, testy i CI — ~13k LOC.

Raw audyty: `docs/plans/2026-02-17-*-code-audit.md`.
