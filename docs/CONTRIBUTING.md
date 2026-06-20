# Contributing to AI Image Studio

Thank you for considering a contribution. This project is a cross-platform desktop integration (Rhino 8 + .NET + React) — small, focused PRs are easier to review than large rewrites.

## Before you start

1. Read [Engineering overview](engineering/overview.md) for architecture context.
2. Check open [GitHub Issues](https://github.com/Bochyn/AI-Image-Studio/issues) for duplicates.
3. For large changes, open an issue or discussion first.

## Development setup

### Prerequisites

| Tool | Version | Notes |
|------|---------|-------|
| Rhino 8 | Latest | Windows or macOS |
| .NET SDK | 8.0.x | Backend + macOS plugin |
| .NET Framework | 4.8 dev pack | Windows plugin only |
| Node.js | 22.x | Matches CI |
| pnpm | 11.x | UI package manager |

### Clone and build

```bash
git clone https://github.com/Bochyn/AI-Image-Studio.git
cd AI-Image-Studio

# Frontend → wwwroot
cd src/RhinoImageStudio.UI
pnpm install
pnpm run build

# Backend + plugin (pick your platform)
cd ../..
cd src && dotnet build RhinoImageStudio.sln        # Windows
cd src && dotnet build RhinoImageStudio.Mac.sln    # macOS
```

Detailed guides:

- [Getting started](getting-started.md)
- [macOS plugin](macos.md)

## Project conventions

### Code

| Area | Convention |
|------|------------|
| User-facing docs | Polish (`docs/pl/`) |
| Code, comments, commits | English |
| Commit messages | [Conventional Commits](https://www.conventionalcommits.org/) — `feat(scope):`, `fix(scope):`, `refactor(scope):`, `docs:` |
| Model capabilities | **Only** edit `src/RhinoImageStudio.UI/src/lib/models.ts` — backend validates separately |
| API contracts | Shared DTOs in `RhinoImageStudio.Shared/Contracts` |
| Display modes | Use `DisplayModeMapping` — do not hardcode Rhino English names |

### Editing rules

- Read a file before changing it.
- Prefer **surgical edits** over full-file rewrites.
- Do not edit `src/RhinoImageStudio.Backend/wwwroot/` by hand — run `pnpm run build` in UI.
- Do not commit API keys, databases, or local captures.

### Branch naming

```text
feat/short-description
fix/short-description
refactor/short-description
docs/short-description
```

## Pull request process

1. Create a branch from `master`.
2. Make changes with tests when behavior changes.
3. Run locally:

```bash
dotnet test src/RhinoImageStudio.Backend.Tests
pnpm --dir src/RhinoImageStudio.UI run lint
pnpm --dir src/RhinoImageStudio.UI run build
git diff --exit-code src/RhinoImageStudio.Backend/wwwroot
```

4. Open a PR with:
   - Conventional title: `type(scope): Summary in imperative mood`
   - Summary + test plan (see [PR template](../.github/pull_request_template.md))
5. Ensure CI passes (Windows plugin, macOS plugin, frontend).

## Security

- Never commit secrets — see [SECURITY.md](../SECURITY.md).
- Run the candidate secret scan before pushing sensitive branches.
- Report vulnerabilities privately — do not open public issues with exploit details.

## Documentation

When your change affects behavior:

| Change type | Update |
|-------------|--------|
| API endpoint | `api/architecture.md` |
| Bridge / plugin | `engineering/cross-platform-bridge.md` |
| Security | `engineering/security.md` |
| User workflow | `guides/` + `pl/przewodniki/` |
| Engineering decisions | `engineering/code-quality.md` or new engineering doc |

## Code review expectations

Reviewers will check:

- Windows **and** macOS impact for shared code
- Contract stability (`Shared/Contracts`)
- No secrets or generated junk in diff
- Tests or clear manual verification steps
- wwwroot rebuilt if UI changed

## Questions

Open a GitHub Issue with the `question` label or refer to [Troubleshooting](guides/troubleshooting.md).

## License

External contributions are accepted only when the contributor agrees that the contribution is provided under the project license and may also be included in separately licensed commercial editions. If a change is substantial, maintainers may request a signed CLA or DCO sign-off before merge. See [LICENSE](../LICENSE) for details.
