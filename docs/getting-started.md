# Installation & Getting Started

This guide walks you through installing and launching AI Image Studio.

## 1. System Requirements

To run the application, your machine must meet the following requirements:

- **Operating system**: Windows 10 or 11. A macOS Rhino 8 build is available separately; see [macOS Plugin Setup](macos.md).
- **Software**: [Rhinoceros 8](https://www.rhino3d.com/) (up to date).
- **Gemini API key**: An account at [Google AI Studio](https://aistudio.google.com/) (required for Gemini 3.1 Flash and Gemini 3 Pro).
- **fal.ai API key**: An account at [fal.ai](https://fal.ai/) (required for Seedream, GPT Image 1.5/2, Pan and Upscale).

### For developers (building from source)
You'll additionally need:
- **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (for the backend and the macOS plug-in; the Windows plug-in builds against .NET Framework 4.8).
- **[Node.js 22.x](https://nodejs.org/)** (for the React UI; matches CI).
- **pnpm 11.x** (recommended package manager for the UI).
- **Git** (to clone the repository).

---

## 2. Installation (For End Users)

*Note: If you downloaded a pre-built release, skip the build section and jump straight to launching.*

The currently recommended path is to build from source (see section 3). A `.rhi` or `.yak` installer will be provided in the future.

---

## 3. Building from Source (For Developers)

### Step 1: Clone the repository
```bash
git clone <repo-url>
cd AI-Image-Studio
```

### Step 2: Build Backend and Windows Plugin (C#)
```bash
cd src
dotnet restore RhinoImageStudio.sln
dotnet build RhinoImageStudio.sln
```
This produces:
- Plugin: `build\Debug\net48\RhinoImageStudio.rhp`
- Backend: `build\Debug\net8.0-windows\RhinoImageStudio.Backend.dll`

For the macOS Rhino 8 plug-in, use the separate macOS solution and installer:

```bash
cd AI-Image-Studio

DOTNET_BIN=/opt/homebrew/opt/dotnet@8/libexec/dotnet \
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec \
PATH=/opt/homebrew/opt/dotnet@8/libexec:$PATH \
scripts/install-mac-plugin.sh
```

Full macOS launch and smoke-test steps are in [macOS Plugin Setup](macos.md).

### Step 3: Build the UI (React)
```bash
cd ../src/RhinoImageStudio.UI
pnpm install
pnpm run build
```
This compiles the frontend and copies it into the backend folder so it can be served locally.

---

## 4. Running the Application

The system has two parts that must run at the same time.

### Step 1: Start the Backend
The backend handles all AI communication. It must run in the background.
```powershell
# In a new terminal window from the repository root:
dotnet build .\src\RhinoImageStudio.sln
.\build\Debug\net8.0-windows\RhinoImageStudio.Backend.exe
```
You should see: `Now listening on http://127.0.0.1:17532`. **Keep this window open.**

### Step 2: Install the Plugin in Rhino
1. Open Rhino 8.
2. Run the `PlugInManager` command.
3. Click **Install...** and point to `build\Debug\net48\RhinoImageStudio.rhp`.
4. Make sure the plugin appears in the list with status "Loaded".

### Step 3: Open the Panel
Run the command:
```
ImageStudio
```
The application's side panel will appear.

---

## 5. Configuring API Keys

AI Image Studio requires provider keys for the model families you want to use. The Settings UI stores only Gemini and fal.ai keys.

### Gemini API Key (required for Gemini 3.1 Flash / Gemini 3 Pro)
1. Sign in to [Google AI Studio](https://aistudio.google.com/) and generate an API key.
2. In the AI Image Studio panel, open the **Settings** tab (gear icon).
3. Paste the key into **Gemini API Key**.
4. Click **Save**.

### fal.ai API Key (required for Seedream / GPT Image 1.5 / GPT Image 2 / Pan / Upscale)
1. Sign in to [fal.ai](https://fal.ai/) and generate an API key.
2. In the AI Image Studio panel, open **Settings**.
3. Paste the key into **fal.ai API Key**.
4. Click **Save**.

OpenAI-branded image models in AI Image Studio are routed through fal.ai endpoints. There is no separate OpenAI API key field in the application.

You're set. Continue with the [Basics Guide](guides/basics.md).
