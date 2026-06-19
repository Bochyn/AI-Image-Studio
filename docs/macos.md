# macOS Plugin Setup

This guide covers the native macOS build of Rhino Image Studio for Rhino 8.

## Status

The macOS version is implemented as a separate Rhino plug-in project:

- Plug-in: `src/RhinoImageStudio.Plugin.Mac`
- Solution: `src/RhinoImageStudio.Mac.sln`
- Installer script: `scripts/install-mac-plugin.sh`
- Installed location: `~/Library/Application Support/McNeel/Rhinoceros/8.0/MacPlugIns/RhinoImageStudio.rhp`

The current macOS build has been verified with Rhino 8 on Apple Silicon:

- Rhino loads the plug-in.
- The plug-in starts the backend as a self-contained sidecar process.
- `ImageStudioOpen` opens the React UI from `http://localhost:17532`.
- Viewport capture works through the macOS HTTP bridge and writes captures/thumbnails to local storage.

AI generation still requires valid provider API keys configured in the app.

## Requirements

- macOS with Rhino 8 installed at `/Applications/Rhino 8.app`
- .NET 8 SDK
- Node.js 18+
- pnpm
- jq for command-line smoke tests

On this workstation the .NET 8 SDK is installed through Homebrew as `dotnet@8`, so build commands use:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec
PATH=/opt/homebrew/opt/dotnet@8/libexec:$PATH
```

## Build and Install

From the repository root:

```bash
cd /Users/mateuszbochynski/Developer/Rhino-Image-Studio
git switch feature/rhino-macos-plugin
```

Build the React UI into the backend `wwwroot` folder:

```bash
cd /Users/mateuszbochynski/Developer/Rhino-Image-Studio/src/RhinoImageStudio.UI
pnpm run build
```

Build and install the macOS plug-in:

```bash
cd /Users/mateuszbochynski/Developer/Rhino-Image-Studio

DOTNET_BIN=/opt/homebrew/opt/dotnet@8/libexec/dotnet \
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec \
PATH=/opt/homebrew/opt/dotnet@8/libexec:$PATH \
scripts/install-mac-plugin.sh
```

The installer:

1. Builds `src/RhinoImageStudio.Mac.sln`.
2. Publishes `src/RhinoImageStudio.Backend` as a self-contained `osx-arm64` sidecar.
3. Copies the plug-in and backend to Rhino's macOS plug-in directory.

## Launch

Start Rhino:

```bash
open -n "/Applications/Rhino 8.app" --args -nosplash
```

Run the status command in Rhino:

```text
ImageStudioMacStatus
```

Or from Terminal:

```bash
/Applications/Rhino\ 8.app/Contents/Resources/bin/rhinocode command ImageStudioMacStatus
```

Confirm the status marker:

```bash
cat "$HOME/Library/Application Support/RhinoImageStudio/mac-plugin-status.json"
```

Expected result includes:

```json
{"loaded":true}
```

## Start Backend and UI

Run in Rhino:

```text
ImageStudioStartBackend
```

Or from Terminal:

```bash
/Applications/Rhino\ 8.app/Contents/Resources/bin/rhinocode command ImageStudioStartBackend
```

Verify backend and bridge:

```bash
curl -fsS http://localhost:17532/api/health
curl -fsS http://localhost:17532/api/rhino/status
```

Expected bridge status:

```json
{"connected":true}
```

Open the UI:

```text
ImageStudioOpen
```

The browser should open:

```text
http://localhost:17532
```

## Smoke Test: Viewport Capture

This tests the complete path:

```text
UI/backend -> Rhino macOS plug-in -> viewport capture -> /api/captures
```

Run:

```bash
project_id=$(curl -fsS \
  -H 'Content-Type: application/json' \
  -d '{"name":"Mac plugin test"}' \
  http://localhost:17532/api/projects | jq -r '.id')

capture_id=$(curl -fsS \
  -H 'Content-Type: application/json' \
  -d "{\"projectId\":\"$project_id\",\"width\":640,\"height\":480,\"displayMode\":\"Shaded\"}" \
  http://localhost:17532/api/rhino/capture | jq -r '.captureId')

echo "project_id=$project_id"
echo "capture_id=$capture_id"

curl -fsS "http://localhost:17532/api/projects/$project_id/captures" | jq .
```

The capture should report:

- `width: 640`
- `height: 480`
- `displayMode: "Shaded"`
- non-empty `imageUrl` and `thumbnailUrl`

Verify files:

```bash
find "$HOME/Library/Application Support/RhinoImageStudio/data" \
  -type f \
  -name "*$capture_id*" \
  -exec file {} \;
```

Expected files:

- `captures/{capture_id}.png`
- `thumbnails/{capture_id}_thumb.png`

## macOS Commands

| Command | Purpose |
|---------|---------|
| `ImageStudioMacStatus` | Confirms that the macOS plug-in loaded and writes a status marker. |
| `ImageStudioStartBackend` | Starts or connects to the Rhino Image Studio backend sidecar. |
| `ImageStudioOpen` | Starts the backend if needed and opens the UI in the default browser. |

## Architecture Notes

The Windows plug-in uses WebView2 and COM host objects for the JavaScript-to-C# bridge. macOS cannot use that path.

The macOS build uses a backend-mediated bridge instead:

1. React calls HTTP endpoints under `/api/rhino/*`.
2. The backend queues Rhino work items in `RhinoBridgeService`.
3. `MacRhinoBridgeClient` long-polls `/api/rhino/bridge/next` from inside Rhino.
4. The plug-in executes RhinoCommon operations on the Rhino UI thread.
5. The plug-in uploads captures through the existing `/api/captures` endpoint.

This keeps the React app mostly shared between Windows and macOS while replacing the platform-specific bridge layer.

## Known Limitations

- macOS currently opens the UI in the system browser, not a docked Rhino panel.
- The bridge currently covers status, display modes and viewport capture. Additional Rhino UI actions from the Windows bridge can be added through the same queue pattern.
- The backend sidecar is published for `osx-arm64` by default.
- AI generation depends on valid Gemini/fal.ai/OpenAI API keys configured through the app.
