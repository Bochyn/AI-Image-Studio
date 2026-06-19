# Model bezpieczeństwa

Local-first: backend na localhost, klucze API tylko po stronie serwera.

Pełna wersja EN: [Security model](../../engineering/security.md).

## Klucze API

- Storage: Data Protection (`IDataProtector`)
- Windows: migracja starych blobów DPAPI przy pierwszym odczycie
- UI: Settings — nigdy w repo

## Bridge

Token w `%LOCALAPPDATA%/RhinoImageStudio/bridge.token` — wymagany nagłówek `X-Rhino-Bridge-Token` na poll/complete. Ochrona przed innymi procesami na localhost.

## Pliki

`StorageService` — `Path.GetFullPath` + odrzucenie ścieżek wychodzących poza root.

## Zgłaszanie

[SECURITY.md](../../../SECURITY.md) — prywatnie, bez kluczy w issue.
