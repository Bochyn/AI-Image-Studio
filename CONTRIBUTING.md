# Contributing

Thanks for taking the time to look at AI Image Studio. The canonical contributor guide lives in [`docs/CONTRIBUTING.md`](docs/CONTRIBUTING.md).

Use that guide for:

- supported Windows and macOS development setup
- required tool versions
- build, test and lint commands
- pull request expectations
- documentation and security requirements
- contribution licensing notes

Quick verification commands:

```bash
dotnet test src/RhinoImageStudio.Backend.Tests
pnpm --dir src/RhinoImageStudio.UI run lint
pnpm --dir src/RhinoImageStudio.UI run build
git diff --exit-code src/RhinoImageStudio.Backend/wwwroot
```

Security issues should not be reported in public issues. See [`SECURITY.md`](SECURITY.md).
