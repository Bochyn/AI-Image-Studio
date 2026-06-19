# Instalacja i Pierwsze Kroki

Ten przewodnik przeprowadzi Cię przez proces instalacji i uruchomienia AI Image Studio.

## 1. Wymagania Systemowe

Aby korzystać z aplikacji, Twój komputer musi spełniać następujące wymagania:

- **System operacyjny**: Windows 10 lub 11. Osobny build dla Rhino 8 na macOS jest opisany w [Plugin macOS](macos.md).
- **Oprogramowanie**: [Rhinoceros 8](https://www.rhino3d.com/) (zaktualizowany).
- **Klucz API Gemini**: Konto na [Google AI Studio](https://aistudio.google.com/) (wymagany do Gemini 3.1 Flash, Gemini 3 Pro, Seedream v5 Lite).
- **Klucz API fal.ai**: Konto na [fal.ai](https://fal.ai/) (wymagany do Pan i Upscale).
- **Klucz API OpenAI**: Konto na [OpenAI Platform](https://platform.openai.com/) (wymagany do GPT-Image 1.5 Edit).

### Dla deweloperów (budowanie ze źródeł)
Dodatkowo wymagane są:
- **[.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)** (do backendu i pluginu macOS; plugin Windows buduje się z targetem .NET Framework 4.8).
- **[Node.js 18+](https://nodejs.org/)** (do interfejsu React).
- **pnpm** (zalecany package manager dla UI).
- **Git** (do pobrania kodu).

---

## 2. Instalacja (Dla Użytkowników Końcowych)

*Uwaga: Jeśli pobrałeś gotową paczkę (release), pomiń sekcję budowania i przejdź do uruchamiania.*

Obecnie zalecaną metodą jest zbudowanie projektu ze źródeł (zobacz sekcję 3). W przyszłości udostępnimy gotowy instalator `.rhi` lub `.yak`.

---

## 3. Budowanie ze Źródeł (Dla Developerów)

### Krok 1: Pobranie kodu
```bash
git clone <adres-repozytorium>
cd "AI Image Studio"
```

### Krok 2: Budowanie Backendu i Pluginu Windows (C#)
```bash
cd src
dotnet restore RhinoImageStudio.sln
dotnet build RhinoImageStudio.sln
```
Spowoduje to utworzenie plików:
- Plugin: `build\Debug\net48\RhinoImageStudio.rhp`
- Backend: `build\Debug\net8.0-windows\RhinoImageStudio.Backend.dll`

Dla pluginu Rhino 8 na macOS użyj osobnego solution i instalatora:

```bash
cd /Users/mateuszbochynski/Developer/AI-Image-Studio

DOTNET_BIN=/opt/homebrew/opt/dotnet@8/libexec/dotnet \
DOTNET_ROOT=/opt/homebrew/opt/dotnet@8/libexec \
PATH=/opt/homebrew/opt/dotnet@8/libexec:$PATH \
scripts/install-mac-plugin.sh
```

Pełne kroki uruchomienia i smoke test są w [Plugin macOS](macos.md).

### Krok 3: Budowanie Interfejsu (React)
```bash
cd ../src/RhinoImageStudio.UI
pnpm install
pnpm run build
```
To skompiluje frontend i skopiuje go do folderu backendu, aby mógł być serwowany lokalnie.

---

## 4. Uruchamianie Aplikacji

System składa się z dwóch części, które muszą działać jednocześnie.

### Krok 1: Uruchom Backend
Backend zarządza komunikacją z AI. Musi działać w tle.
```bash
# W nowym oknie terminala:
cd "D:\Rhino Image Studio\build\Debug\net8.0-windows"
dotnet RhinoImageStudio.Backend.dll
```
Powinieneś zobaczyć: `Now listening on http://127.0.0.1:17532`. **Nie zamykaj tego okna.**

### Krok 2: Zainstaluj Plugin w Rhino
1. Otwórz Rhino 8.
2. Wpisz komendę `PlugInManager`.
3. Kliknij **Install...** i wskaż plik `build\Debug\net48\RhinoImageStudio.rhp`.
4. Upewnij się, że plugin jest na liście i ma status "Loaded".

### Krok 3: Otwórz Panel
Wpisz komendę:
```
RhinoImageStudio
```
Pojawi się panel boczny aplikacji.

---

## 5. Konfiguracja Kluczy API

AI Image Studio wymaga kluczy API dla każdego dostawcy AI, z którego chcesz korzystać.

### Gemini API Key (wymagany dla Gemini 3.1 Flash / Gemini 3 Pro / Seedream v5 Lite)
1. Zaloguj się na [Google AI Studio](https://aistudio.google.com/) i wygeneruj klucz API.
2. W panelu AI Image Studio przejdź do zakładki **Settings** (ikona ⚙️).
3. Wklej klucz w pole **Gemini API Key**.
4. Kliknij **Save**.

### fal.ai API Key (wymagany dla Pan / Upscale)
1. Zaloguj się na [fal.ai](https://fal.ai/) i wygeneruj klucz API.
2. W panelu AI Image Studio przejdź do zakładki **Settings** (ikona ⚙️).
3. Wklej klucz w pole **fal.ai API Key**.
4. Kliknij **Save**.

### OpenAI API Key (wymagany dla GPT-Image 1.5 Edit)
1. Zaloguj się na [OpenAI Platform](https://platform.openai.com/) i wygeneruj klucz API.
2. W panelu AI Image Studio przejdź do zakładki **Settings** (ikona ⚙️).
3. Wklej klucz w pole **OpenAI API Key**.
4. Kliknij **Save**.

Gotowe! Możesz przejść do [Przewodnika Podstawowego](przewodniki/podstawy.md).
