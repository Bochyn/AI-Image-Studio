# Security Model

Rhino Image Studio is **local-first**: it runs a backend on `localhost`, stores projects on disk, and proxies AI provider APIs so keys never ship to the React bundle. This document describes the threat model and controls — for reporters, see [SECURITY.md](../../SECURITY.md).

## Threat model (practical)

| Actor | Capability | Concern |
|-------|------------|---------|
| User | Full machine access | Misconfigure keys; expected |
| Other local processes | Can call `localhost` | **Bridge queue hijacking** (mitigated) |
| Remote attacker | No direct exposure unless port forwarded | Out of scope for default install |
| Repository reader | Public source | **No secrets in git** |

The backend binds to localhost. It is not designed as a multi-tenant internet service.

## API keys and secrets

### Storage

| Property | Implementation |
|----------|----------------|
| Location | `%LOCALAPPDATA%/RhinoImageStudio/secrets/` (Windows) / `~/Library/Application Support/...` (macOS) |
| Encryption | ASP.NET Core **Data Protection** (`IDataProtector`) |
| Legacy Windows | **DPAPI** blobs migrated on first read (`ProtectedData.Unprotect` → re-save with Data Protection) |
| In UI | Settings modal — keys never in `appsettings.json` or env in repo |

Key names centralized in `SecretKeyNames` (Shared) — avoids string drift across endpoints.

### Verification

`POST /api/config/verify-gemini-key` calls Gemini via `IGeminiClient.VerifyApiKeyAsync` using the `x-goog-api-key` header (never in the URL).

### What not to commit

See [SECURITY.md](../../SECURITY.md): `.env`, local databases, captures, `bridge.token`, agent configs.

## Bridge authentication

**Problem:** On macOS, any process could previously call:

- `GET /api/rhino/bridge/next` — steal or complete Rhino work items
- `POST /api/rhino/bridge/{id}/complete` — spoof capture results

**Control:**

1. Backend generates a random token at startup (`BridgeTokenService`)
2. Token written to `LocalApplicationData/RhinoImageStudio/bridge.token`
3. Plugin reads token via `BridgeTokenReader`
4. Poll and complete require header: `X-Rhino-Bridge-Token: <token>`
5. `GET /api/rhino/status` returns **only** `connected` and `lastSeenUtc` — never the token

This is **localhost boundary hardening**, not user authentication. It stops casual cross-process abuse (other dev tools, stray scripts) on poll/complete endpoints.

### UI session token

Mutating API routes (`POST`/`PUT`/`DELETE` on projects, jobs, config, captures, etc.) require the same `X-Rhino-Bridge-Token` header.

1. `GET /api/bootstrap` returns the localhost token for the React UI (macOS browser + Vite dev).
2. WebView2 panel injects `window.__RHINO_LOCAL_TOKEN` before navigation.
3. UI attaches the header on all mutating `fetch` calls via `apiFetch()`.

This does not replace user accounts — it ensures only callers that obtained the shared localhost token can enqueue paid jobs or overwrite API keys.

UI-initiated bridge calls (`/api/rhino/capture`, display queries) remain open to any localhost caller while the macOS plugin is connected. That is acceptable for the single-seat desktop threat model but allows local queue flooding.

→ Design context: [Cross-platform bridge](cross-platform-bridge.md)

## Filesystem storage

`StorageService` serves images from a configured root under user data.

**Path traversal fix:**

```csharp
var root = Path.GetFullPath(BasePath);
if (!root.EndsWith(Path.DirectorySeparatorChar))
    root += Path.DirectorySeparatorChar;
var absolutePath = Path.GetFullPath(Path.Combine(root, normalizedPath));
if (!absolutePath.StartsWith(root, StringComparison.OrdinalIgnoreCase))
    throw new UnauthorizedAccessException(...);
```

User-supplied paths in `/images/{**path}` cannot escape the storage directory via `..` segments or sibling-directory prefix tricks (`data` vs `data-evil`).

## AI provider proxy

| Property | Behavior |
|----------|----------|
| Keys | Read from secret storage server-side only |
| Requests | Built in `JobProcessor` / `GeminiClient` / `FalAiClient` |
| Client exposure | Frontend never receives provider keys |
| Cancel | fal cancel uses stored `ProviderModelId` on the job row |

Prompts and images leave the machine only to configured providers (Google, fal.ai).

## Transport

| Link | Protocol | Notes |
|------|----------|-------|
| UI ↔ Backend | HTTP localhost | Same machine |
| Backend ↔ Gemini / fal | HTTPS | Standard TLS |
| SSE | `text/event-stream` | Job progress only; no secrets in events |

## Data at rest

| Data | Location | Sensitivity |
|------|----------|-------------|
| SQLite DB | User data dir | Project metadata, prompts |
| Captures / generations | User data dir | User designs |
| Thumbnails | User data dir | Derived images |
| Secrets | Encrypted files | **High** |
| Bridge token | Plain file in user data | **Medium** (localhost only) |

Soft-delete (`IsArchived`) retains generations until permanent delete.

## Pre-merge checklist

From [SECURITY.md](../../SECURITY.md):

```bash
git status --short
rg -l -i --hidden --glob '!**/.git/**' --glob '!**/node_modules/**' \
  '(api[_ -]?key|secret|password|token|FAL_KEY|GEMINI_API_KEY)' .
```

Inspect matches manually. Do not paste real key values into issues or PRs.

## Known limitations

| Limitation | Rationale |
|------------|-----------|
| No user accounts | Single-seat desktop plugin |
| Bridge token on disk | Shared secret between co-located processes |
| Browser UI on macOS | No embedded WebView — larger attack surface than WebView2 panel |
| HTTP localhost | Not HTTPS — acceptable for loopback |

Future hardening could include per-session tokens rotated on plugin connect and certificate pinning for provider APIs (low priority for localhost tool).

## Related documents

- [Code quality — security findings](code-quality.md#critical-findings-and-fixes)
- [Architecture — config endpoints](../api/architecture.md#config)
- [Contributing — security section](../CONTRIBUTING.md#security)
