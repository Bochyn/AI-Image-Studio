# AI Image Studio Documentation

Welcome to the **AI Image Studio** documentation — a cross-platform Rhinoceros 8 plugin for AI-assisted architectural visualization workflows (Google Gemini and fal.ai).

> Polish: [`docs/pl/`](pl/index.md)

---

## I want to…

| Goal | Start here |
|------|------------|
| **Install and use the plugin** | [Getting Started](getting-started.md) → [Basics](guides/basics.md) |
| **Set up macOS** | [macOS Plugin Setup](macos.md) |
| **Fix something broken** | [Troubleshooting](guides/troubleshooting.md) |
| **Understand the architecture** | [System Architecture](api/architecture.md) |
| **Review the project (hiring / portfolio)** | [Engineering overview](engineering/overview.md) |
| **Contribute code** | [Contributing](CONTRIBUTING.md) |

---

## User guides

- **[Getting Started](getting-started.md)** — requirements, installation, API keys
- **[macOS Plugin Setup](macos.md)** — build, install, bridge smoke tests
- **[Basics & Workflow](guides/basics.md)** — Capture → Generate → Upscale
- **[Supported AI Models](ai-models.md)** — models, parameters, capabilities
- **[Troubleshooting](guides/troubleshooting.md)** — backend, plugin, generation errors

---

## Engineering & architecture

For developers, architects, and technical reviewers:

| Document | Description |
|----------|-------------|
| **[Engineering hub](engineering/README.md)** | Navigation, metrics, module map |
| **[Project overview](engineering/overview.md)** | Executive summary — problem, solution, interview talking points |
| **[Code quality & audit](engineering/code-quality.md)** | Audit findings and remediation story |
| **[Cross-platform bridge](engineering/cross-platform-bridge.md)** | Windows WebView2 vs macOS HTTP RPC |
| **[Security model](engineering/security.md)** | Secrets, bridge token, storage |
| **[Testing & CI](engineering/testing-and-ci.md)** | Unit tests, ESLint, GitHub Actions |
| **[System architecture (reference)](api/architecture.md)** | Full API, DTOs, database, design system |

---

## Contributing

The project is source-available for noncommercial use. See **[Contributing](CONTRIBUTING.md)** for setup, conventions, and PR checklist.

Report bugs via GitHub Issues. Security issues: [SECURITY.md](../SECURITY.md).
