# Rozwiązywanie Problemów

Rozwiązania najczęstszych problemów napotykanych podczas pracy z Rhino Image Studio.

## Problemy z Uruchomieniem

### Backend nie startuje (Port in use)
**Objaw**: Komunikat błędu `System.IO.IOException: Failed to bind to address http://127.0.0.1:17532`.
**Przyczyna**: Inna instancja backendu już działa lub port jest zajęty.
**Rozwiązanie**:
1. Sprawdź, czy nie masz otwartego innego okna terminala z uruchomionym `dotnet run`.
2. Zamknij proces `RhinoImageStudio.Backend.exe` przez Menedżer Zadań.
3. Na macOS sprawdź i zatrzymaj sidecar process:
   ```bash
   pgrep -fl 'RhinoImageStudio.Backend'
   pkill -f 'RhinoImageStudio.Backend --port=17532'
   ```

### Plugin nie ładuje się w Rhino
**Objaw**: Błąd przy instalacji lub brak komendy `RhinoImageStudio`.
**Przyczyna**: Zła wersja Rhino lub brak .NET Framework 4.8.
**Rozwiązanie**:
- Upewnij się, że używasz **Rhino 8** na Windows.
- Sprawdź we właściwościach pliku `.rhp` (prawy przycisk myszy -> Właściwości), czy system Windows go nie zablokował ("Unblock").
- Na macOS przeinstaluj plugin przez `scripts/install-mac-plugin.sh`, zrestartuj Rhino i uruchom:
  ```bash
  /Applications/Rhino\ 8.app/Contents/Resources/bin/rhinocode command ImageStudioMacStatus
  cat "$HOME/Library/Application Support/RhinoImageStudio/mac-plugin-status.json"
  ```

### macOS bridge jest disconnected
**Objaw**: `/api/rhino/status` zwraca `connected: false` albo viewport capture nie działa z UI w przeglądarce.
**Przyczyna**: Rhino nie działa, plugin nie uruchomił backendu albo long-poll bridge nie jest podłączony.
**Rozwiązanie**:
1. Uruchom Rhino 8.
2. W Rhino uruchom `ImageStudioStartBackend`.
3. Zweryfikuj:
   ```bash
   curl -fsS http://localhost:17532/api/health
   curl -fsS http://localhost:17532/api/rhino/status
   ```
4. Jeśli status nadal jest disconnected, zrestartuj Rhino i uruchom `ImageStudioStartBackend` ponownie.

### Pusty biały ekran w panelu
**Objaw**: Panel się otwiera, ale jest pusty.
**Przyczyna**: Brak WebView2 Runtime lub backend nie działa.
**Rozwiązanie**:
1. Upewnij się, że backend działa (otwórz `http://localhost:17532` w przeglądarce - powinieneś zobaczyć stronę UI).
2. Zainstaluj [WebView2 Runtime](https://developer.microsoft.com/en-us/microsoft-edge/webview2/).

## Problemy z Generowaniem

### Błąd "Authentication failed"
**Przyczyna**: Nieprawidłowy lub brakujący klucz API.
**Rozwiązanie**: Sprawdź zakładkę Settings i upewnij się, że klucz nie ma spacji na początku/końcu. Wygeneruj nowy klucz na fal.ai.

### Generacja trwa w nieskończoność
**Przyczyna**: Problem z połączeniem SSE lub przeciążenie serwerów AI.
**Rozwiązanie**:
- Sprawdź pasek postępu - powinien płynnie rosnąć od 20% do 85% podczas generacji.
- Jeśli pasek stoi w miejscu, sprawdź logi w konsoli backendu.
- Spróbuj zrestartować backend.

### Pasek postępu nie aktualizuje się
**Przyczyna**: Utracone połączenie SSE (Server-Sent Events).
**Rozwiązanie**:
- Aplikacja automatycznie próbuje wznowić połączenie (do 10 prób).
- Jeśli problem się powtarza, odśwież stronę (F5).
- Sprawdź w narzędziach deweloperskich przeglądarki (F12 → Network) czy połączenie `/api/projects/{id}/events` jest aktywne.

### Wynik jest bardzo słabej jakości / rozmazany
**Przyczyna**: Zbyt niska rozdzielczość Capture lub błędny model AI.
**Rozwiązanie**:
- Zwiększ rozdzielczość w ustawieniach Capture.
- Użyj funkcji **Upscale** na gotowym obrazku.
