# Security Policy

AI Image Studio is source-available for noncommercial use. Treat everything committed to this repository as public.

## Reporting Security Issues

Do not open a public issue containing secrets, API keys, exploit details or private user data.

Preferred reporting path:

1. Use GitHub's private security advisory flow for this repository: `https://github.com/Bochyn/AI-Image-Studio/security/advisories/new`.
2. If advisories are unavailable, open a public issue with a minimal title such as "Security contact requested" and no exploit details, then move the discussion to a private channel with the maintainer.

## Secrets and Local Data

Never commit:

- `.env` files
- `appsettings*.json` with local values
- API keys or provider tokens
- SQLite databases
- generated captures, thumbnails or AI outputs
- local agent configuration (`.agents/`, `.codex/`, `.worktrees/`, `skills/`)
- machine-specific Rhino settings or plug-in install directories

Provider keys must be entered through the application UI and stored only in local encrypted storage.

## Supported Security Checks

Before merging a release branch, run:

```bash
git status --short
rg -l -i --hidden --glob '!**/.git/**' --glob '!**/node_modules/**' --glob '!**/bin/**' --glob '!**/obj/**' --glob '!build/**' '(api[_ -]?key|secret|password|token|bearer|BEGIN (RSA|OPENSSH|PRIVATE) KEY|FAL_KEY|GEMINI_API_KEY|OPENAI_API_KEY)' .
```

The second command is a candidate-file scan. Inspect matches carefully and do not print real secret values in public logs.

## Supported Versions

This project is in active pre-release development. Security fixes are applied to the default branch and release branches that are explicitly marked as supported.
