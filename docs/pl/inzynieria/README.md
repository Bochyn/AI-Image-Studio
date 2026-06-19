# Dokumentacja inżynierska

Materiały techniczne dla deweloperów, architektów i osób oceniających **Rhino Image Studio** w kontekście rekrutacji lub portfolio.

> English: [Engineering docs](../engineering/README.md)

## Dla kogo co

| Odbiorca | Zacznij od | Po co |
|----------|------------|-------|
| **Rekruter / hiring manager** | [Przegląd projektu](przeglad.md) | Problem, rozwiązanie, stack i highlighty w ~5 minut |
| **Senior engineer** | [Jakość kodu i audyt](jakosc-kodu.md) | Co było nie tak, co naprawiono, jak pilnujemy jakości |
| **Architekt** | [Bridge cross-platform](most-cross-platform.md) + [Architektura](../api/architektura.md) | Najtrudniejszy problem integracyjny |
| **Security** | [Model bezpieczeństwa](bezpieczenstwo.md) | Sekrety, token bridge, storage |
| **Kontrybutor** | [Contributing (EN)](../CONTRIBUTING.md) | Build, testy, PR |

## Spis dokumentów

| Dokument | Opis |
|----------|------|
| [Przegląd projektu](przeglad.md) | Executive summary — produkt, architektura, decyzje |
| [Jakość kodu i audyt](jakosc-kodu.md) | Ustrukturyzowany audyt i remediacja |
| [Bridge cross-platform](most-cross-platform.md) | WebView2 vs HTTP bridge na macOS |
| [Model bezpieczeństwa](bezpieczenstwo.md) | Klucze API, Data Protection, token |
| [Testy i CI](testy-i-ci.md) | xUnit, ESLint, GitHub Actions |

## Skala repozytorium

```text
~13 000 linii kodu aplikacji (bez zależności)
6 projektów .NET / TypeScript
CI: Windows, macOS, frontend
```

| Moduł | Rola | ~LOC |
|-------|------|------|
| `RhinoImageStudio.UI` | React — canvas, inspector, maski | 7 100 |
| `RhinoImageStudio.Backend` | ASP.NET Core — kolejka, proxy AI | 3 500 |
| `RhinoImageStudio.Shared` | Kontrakty, enumy, utility | 700 |
| `RhinoImageStudio.Plugin` | Windows — WebView2 | 800 |
| `RhinoImageStudio.Plugin.Mac` | macOS — HTTP bridge | 560 |
| `RhinoImageStudio.Plugin.RhinoCommon` | Wspólny capture Rhino | 290 |
