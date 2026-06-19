# Rhino Image Studio

Wtyczka Rhinoceros 8 (Windows) integrująca AI image generation z workflow architektonicznym. Backend proxy do Gemini/fal.ai + dockowany panel WebView2 z React UI.

## Stack
- **Runtime:** .NET 8 (Backend), .NET Framework 4.8 (Plugin), Node 18+ (UI)
- **Backend:** ASP.NET Core 8.0, EF Core + SQLite, DPAPI secrets
- **Frontend:** React 18, Vite 5, TypeScript 5.4, Tailwind CSS 3.4
- **Key deps:** lucide-react, react-router-dom, clsx, tailwind-merge, date-fns
- **AI:** Google Gemini API (generate/refine/inpainting), fal.ai (multi-angle, upscale)
- **Package managers:** dotnet (C#), pnpm (UI) — vite build → Backend/wwwroot

## Structure

| Path | Role |
|------|------|
| `src/RhinoImageStudio.Backend/` | ASP.NET Core API — proxy AI, job queue, storage |
| `src/RhinoImageStudio.Plugin/` | Rhino 8 plugin (.NET 4.8) — viewport capture, WebView2 host |
| `src/RhinoImageStudio.Shared/` | Shared models, contracts, constants, enums |
| `src/RhinoImageStudio.UI/` | React SPA — canvas, inspector, masks, compare |
| `src/RhinoImageStudio.UI/src/lib/models.ts` | Model config — Source of Truth for model capabilities |
| `src/RhinoImageStudio.Backend/wwwroot/` | Built frontend — auto-generated, nie edytować |
| `docs/` | User & dev documentation (PL) |
| `changelog/` | Claude-to-Claude notes (per-day, .gitignore) |

## Workflows
```bash
dev:      cd src/RhinoImageStudio.UI && pnpm run dev            # Frontend dev (needs backend)
build-ui: cd src/RhinoImageStudio.UI && tsc && pnpm run build   # Build UI → wwwroot
backend:  cd src/RhinoImageStudio.Backend && dotnet run        # localhost:17532
solution: cd src && dotnet build RhinoImageStudio.sln          # Build all C#
```

## Key Architecture
- **2-image Gemini pipeline:** Masks as colored overlay (source + overlay), no native mask param
- **Model-aware UI:** InspectorPanel adapts AR/Resolution/mask limits per model (`models.ts`)
- **Soft-delete:** Generations archived (IsArchived), hard delete requires archived first
- **SSE progress:** Real-time job status via `/api/projects/{id}/events`
- **DPAPI secrets:** API keys via Windows Data Protection, not in code

## Design System
- **Palette:** Mono-Theme — achromatyczny + teal accent — CSS vars (`--text`, `--primary`, `--border`, etc.)
- **Font:** Geist Mono | **Theme:** Light + Dark mode | **Radius:** 0 (ostre krawędzie)
- **Full spec:** `docs/api/architektura.md` → sekcja "Design System"

## Conventions
- **Language:** PL user-facing docs, EN code + comments
- **Commits:** `git commit --author="Claude <claude@anthropic.com>"`
- **Style:** Conventional Commits (`feat(scope):`, `fix(scope):`, `docs:`)

## Constraints
- MUST read file before editing
- MUST NOT commit without explicit user approval
- MUST NOT push without explicit user approval
- MUST NOT rewrite entire files — surgical edits only
- MUST NOT hardcode API keys in source code
- MUST keep `models.ts` as single source of truth for model capabilities
