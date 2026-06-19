# Bridge cross-platform

Jak ten sam UI React woła API Rhino na Windows (WebView2) i macOS (HTTP przez backend).

Pełna wersja EN: [Cross-platform bridge](../../engineering/cross-platform-bridge.md).

## Problem

macOS nie ma WebView2. UI działa w przeglądarce, Rhino w osobnym procesie — potrzebna **kolejka RPC** w backendzie.

## Windows

`window.chrome.webview.hostObjects.rhino` → `RhinoUiThread` → `ViewportCaptureService` → upload `/api/captures`.

## macOS

1. UI → `GET/POST /api/rhino/*`
2. `RhinoBridgeService` kolejkuje pracę
3. Plugin long-poll `GET /api/rhino/bridge/next` + token
4. Wykonanie na wątku UI Rhino → `POST .../complete`

## Wspólny kod

`RhinoImageStudio.Plugin.RhinoCommon` — capture, upload, display modes (`DisplayModeMapping` w Shared).

## Frontend

`rhino.ts` — jawne `webview2` vs `http`, bez cichego fallbacku.

## Smoke test macOS

```text
ImageStudioMacStatus
ImageStudioStartBackend
ImageStudioOpen
```

Szczegóły: [Plugin macOS](../macos.md).
