# Engineering Documentation

Technical documentation for developers, architects, and reviewers evaluating **AI Image Studio** as a portfolio or hiring artifact.

> Polish: [Inżynieria (PL)](../pl/inzynieria/README.md)

## Who should read what

| Audience | Start here | Why |
|----------|------------|-----|
| **Recruiter / hiring manager** | [Project overview](overview.md) | Problem, solution, stack, and engineering highlights in ~5 minutes |
| **Senior engineer / reviewer** | [Code quality & audit](code-quality.md) | What was wrong, what changed, and how quality is enforced |
| **Architect** | [Cross-platform bridge](cross-platform-bridge.md) + [Architecture](../api/architecture.md) | The hardest integration problem in the project |
| **Security reviewer** | [Security model](security.md) | Secrets, local threat model, bridge authentication |
| **Contributor** | [Contributing guide](../CONTRIBUTING.md) + [Testing & CI](testing-and-ci.md) | How to build, test, and open a PR |

## Documents in this section

| Document | Summary |
|----------|---------|
| [Project overview](overview.md) | Executive technical summary — product, architecture, decisions, metrics |
| [Code quality & audit](code-quality.md) | Structured audit findings and remediation (backend, frontend, plugins) |
| [Cross-platform bridge](cross-platform-bridge.md) | Windows WebView2 vs macOS HTTP bridge — design and implementation |
| [Security model](security.md) | API keys, Data Protection, bridge token, storage hardening |
| [Testing & CI](testing-and-ci.md) | Unit tests, lint, GitHub Actions matrix |

## Related documentation

- [System architecture (full reference)](../api/architecture.md) — endpoints, DTOs, database, design system
- [AI models](../ai-models.md) — model capabilities and UI configuration
- [macOS plugin setup](../macos.md) — build, install, smoke tests
- [Security policy](../../SECURITY.md) — reporting and pre-merge checks

## Repository at a glance

```text
~13k lines of application source (excluding dependencies)
6 .NET / TypeScript projects
3 CI jobs: Windows plugin, macOS plugin, frontend + wwwroot sync
2 AI providers proxied locally: Google Gemini, fal.ai
```

| Module | Role | ~LOC |
|--------|------|------|
| `RhinoImageStudio.UI` | React SPA — canvas, inspector, masks, gallery | 7,100 |
| `RhinoImageStudio.Backend` | ASP.NET Core API — jobs, storage, AI proxy | 3,500 |
| `RhinoImageStudio.Shared` | Contracts, enums, cross-cutting utilities | 700 |
| `RhinoImageStudio.Plugin` | Windows Rhino 8 host (WebView2) | 800 |
| `RhinoImageStudio.Plugin.Mac` | macOS Rhino 8 host (HTTP bridge) | 560 |
| `RhinoImageStudio.Plugin.RhinoCommon` | Shared RhinoCommon capture & bridge helpers | 290 |
