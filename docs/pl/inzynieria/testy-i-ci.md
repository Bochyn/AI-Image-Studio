# Testy i CI

Pełna wersja EN: [Testing & CI](../../engineering/testing-and-ci.md).

## GitHub Actions

| Job | Platforma | Co robi |
|-----|-----------|---------|
| Frontend UI | ubuntu | `pnpm lint`, `pnpm build`, diff `wwwroot` |
| Windows plug-in | windows-latest | `dotnet build` + `dotnet test` |
| macOS plug-in | macos-14 | build Mac.sln, testy, publish `osx-arm64` |

## Lokalnie

```bash
dotnet test src/RhinoImageStudio.Backend.Tests
pnpm --dir src/RhinoImageStudio.UI run lint
pnpm --dir src/RhinoImageStudio.UI run build
```

## Testy jednostkowe

- `DisplayModeMappingTests`
- `FalInputBuilderTests`

Rhino nie jest uruchamiane w CI — smoke testy manualne (Windows / macOS).
