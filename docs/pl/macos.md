# Plugin macOS

Ten przewodnik opisuje natywną wersję AI Image Studio dla Rhino 8 na macOS.

## Status

Wersja macOS jest osobnym projektem pluginu Rhino:

- Plugin: `src/RhinoImageStudio.Plugin.Mac`
- Solution: `src/RhinoImageStudio.Mac.sln`
- Instalator: `scripts/install-mac-plugin.sh`
- Lokalizacja instalacji: `~/Library/Application Support/McNeel/Rhinoceros/8.0/MacPlugIns/RhinoImageStudio.rhp`

Obecny build macOS został zweryfikowany w Rhino 8 na Apple Silicon:

- Rhino ładuje plugin.
- Plugin uruchamia backend jako self-contained sidecar process.
- `ImageStudioOpen` otwiera React UI z `http://localhost:17532`.
- Viewport capture działa przez macOS HTTP bridge i zapisuje capture oraz thumbnail w lokalnym storage.

Generowanie AI nadal wymaga poprawnych kluczy API skonfigurowanych w aplikacji.

## Wymagania

- macOS z Rhino 8 zainstalowanym w `/Applications/Rhino 8.app`
- .NET 8 SDK
- Node.js 22.x
- pnpm
- jq do testów smoke z terminala

Na tej maszynie .NET 8 SDK jest zainstalowany przez Homebrew jako `dotnet@8`, dlatego komendy build używają:

```bash
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec
PATH=/opt/homebrew/opt/dotnet@8/libexec:$PATH
```

## Build i instalacja

Z root repozytorium:

```bash
cd AI-Image-Studio
```

Zbuduj React UI do folderu backendu `wwwroot`:

```bash
cd src/RhinoImageStudio.UI
pnpm run build
```

Zbuduj i zainstaluj plugin macOS:

```bash
cd ../..

DOTNET_BIN=/opt/homebrew/opt/dotnet@8/libexec/dotnet \
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec \
PATH=/opt/homebrew/opt/dotnet@8/libexec:$PATH \
scripts/install-mac-plugin.sh
```

Instalator:

1. Buduje `src/RhinoImageStudio.Mac.sln`.
2. Publikuje `src/RhinoImageStudio.Backend` jako self-contained `osx-arm64` sidecar.
3. Kopiuje plugin i backend do katalogu pluginów Rhino na macOS.

## Uruchomienie

Uruchom Rhino:

```bash
open -n "/Applications/Rhino 8.app" --args -nosplash
```

W Rhino uruchom komendę statusu:

```text
ImageStudioMacStatus
```

Albo z terminala:

```bash
/Applications/Rhino\ 8.app/Contents/Resources/bin/rhinocode command ImageStudioMacStatus
```

Potwierdź marker statusu:

```bash
cat "$HOME/Library/Application Support/RhinoImageStudio/mac-plugin-status.json"
```

Oczekiwany wynik zawiera:

```json
{"loaded":true}
```

## Backend i UI

W Rhino uruchom:

```text
ImageStudioStartBackend
```

Albo z terminala:

```bash
/Applications/Rhino\ 8.app/Contents/Resources/bin/rhinocode command ImageStudioStartBackend
```

Zweryfikuj backend i bridge:

```bash
curl -fsS http://localhost:17532/api/health
curl -fsS http://localhost:17532/api/rhino/status
```

Oczekiwany status bridge:

```json
{"connected":true}
```

Otwórz UI:

```text
ImageStudioOpen
```

Przeglądarka powinna otworzyć:

```text
http://localhost:17532
```

## Smoke test: viewport capture

Ten test sprawdza pełną ścieżkę:

```text
UI/backend -> Rhino macOS plugin -> viewport capture -> /api/captures
```

Uruchom:

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

Capture powinien mieć:

- `width: 640`
- `height: 480`
- `displayMode: "Shaded"`
- niepuste `imageUrl` i `thumbnailUrl`

Sprawdź pliki:

```bash
find "$HOME/Library/Application Support/RhinoImageStudio/data" \
  -type f \
  -name "*$capture_id*" \
  -exec file {} \;
```

Oczekiwane pliki:

- `captures/{capture_id}.png`
- `thumbnails/{capture_id}_thumb.png`

## Komendy macOS

| Komenda | Cel |
|---------|-----|
| `ImageStudioMacStatus` | Potwierdza, że plugin macOS się załadował i zapisuje marker statusu. |
| `ImageStudioStartBackend` | Uruchamia albo podłącza backend sidecar AI Image Studio. |
| `ImageStudioOpen` | Uruchamia backend, jeśli trzeba, i otwiera UI w domyślnej przeglądarce. |

## Notatki architektoniczne

Plugin Windows używa WebView2 i COM host objects jako bridge JavaScript-to-C#. macOS nie może użyć tej ścieżki.

Build macOS używa backend-mediated bridge:

1. React woła endpointy HTTP pod `/api/rhino/*`.
2. Backend kolejkuje work items Rhino w `RhinoBridgeService`.
3. `MacRhinoBridgeClient` long-polluje `/api/rhino/bridge/next` z procesu Rhino.
4. Plugin wykonuje viewport capture na Rhino UI thread; RPC dla display/query obecnie wołają współdzielone helpery RhinoCommon bezpośrednio.
5. Plugin uploaduje captures przez istniejący endpoint `/api/captures`.

Dzięki temu React UI pozostaje w większości wspólne dla Windows i macOS, a wymieniona jest platform-specific bridge layer.

## Znane ograniczenia

- macOS obecnie otwiera UI w systemowej przeglądarce, nie jako docked panel Rhino.
- Bridge obejmuje obecnie status, display modes i viewport capture. Dodatkowe akcje Rhino z Windows bridge można dodać tym samym queue pattern.
- Backend sidecar domyślnie publikuje się dla `osx-arm64`.
- Generowanie AI zależy od poprawnych kluczy Gemini i/lub fal.ai skonfigurowanych w aplikacji. Modele obrazowe z nazwą OpenAI są routowane przez fal.ai.
