# AI Image Studio

AI Image Studio is a cross-platform Rhinoceros 8 integration for AI-assisted architectural visualization. It combines Rhino plug-in hosts, a shared ASP.NET Core backend and a React UI.

## Stack

- **Runtime:** .NET 8 backend and macOS plug-in, .NET Framework 4.8 Windows plug-in, Node.js 22.x UI
- **Backend:** ASP.NET Core 8.0, EF Core + SQLite, ASP.NET Core Data Protection secrets
- **Frontend:** React 18, Vite 6, TypeScript 5.4, Tailwind CSS 3.4, Geist Mono
- **AI providers:** Google Gemini API and fal.ai routes for Seedream, GPT Image, Qwen and Topaz
- **Package managers:** dotnet for C#, pnpm 11.x for UI

## Structure

| Path | Role |
|------|------|
| `src/RhinoImageStudio.Backend/` | ASP.NET Core API, AI proxy, job queue, storage |
| `src/RhinoImageStudio.Plugin/` | Windows Rhino 8 plug-in, WebView2 host, docked panel |
| `src/RhinoImageStudio.Plugin.Mac/` | macOS Rhino 8 plug-in, backend sidecar and HTTP bridge |
| `src/RhinoImageStudio.Plugin.RhinoCommon/` | Shared RhinoCommon capture, upload and display-query helpers |
| `src/RhinoImageStudio.Shared/` | Shared models, contracts, constants and enums |
| `src/RhinoImageStudio.UI/` | React SPA: canvas, inspector, masks, compare, gallery |
| `src/RhinoImageStudio.UI/src/lib/models.ts` | Source of truth for model capabilities |
| `src/RhinoImageStudio.Backend/wwwroot/` | Generated frontend build output; do not edit manually |
| `docs/` | English primary docs plus Polish mirrors under `docs/pl/` |

## Workflows

```bash
# Frontend dev
cd src/RhinoImageStudio.UI && pnpm run dev

# Build UI into Backend/wwwroot
cd src/RhinoImageStudio.UI && pnpm run build

# Backend
cd src/RhinoImageStudio.Backend && dotnet run

# Windows solution
dotnet build src/RhinoImageStudio.sln

# macOS solution
dotnet build src/RhinoImageStudio.Mac.sln
```

## Key Architecture

- **2-image Gemini pipeline:** masks are sent as a colored overlay with per-layer instructions.
- **Model-aware UI:** `models.ts` drives available AR, resolution, reference and mask controls.
- **Soft-delete:** generations are archived first; permanent delete requires archived state.
- **SSE progress:** job status streams through per-subscriber `/api/events` and project event endpoints.
- **Cross-platform bridge:** Windows uses WebView2 host objects; macOS uses backend-mediated HTTP RPC with a local bridge token.
- **Local encrypted secrets:** provider keys are stored through local encrypted storage, not source config.

## Conventions

- Public docs are English-first; Polish mirrors live under `docs/pl/`.
- Code, comments, commits and durable technical docs are in English.
- Use Conventional Commits (`feat(scope):`, `fix(scope):`, `docs:`).
- Keep `models.ts` as the UI source of truth for model capabilities.

## Constraints

- Read files before editing.
- Prefer small, reviewable edits.
- Do not edit `src/RhinoImageStudio.Backend/wwwroot/` manually; rebuild the UI.
- Do not hardcode API keys or local secrets.
- Do not commit or push unless the user explicitly asks for a commit/PR workflow.
