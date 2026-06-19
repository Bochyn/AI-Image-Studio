# Podstawy Użytkowania

Dowiedz się, jak przekształcić widok z Rhino w wizualizację AI w kilku prostych krokach.

## Przegląd Interfejsu

Panel Rhino Image Studio składa się z głównych sekcji:
1.  **Canvas (Podgląd)**: Główny obszar wyświetlający przechwycony widok lub wygenerowany obraz.
2.  **Controls (Panel sterowania)**: Po prawej stronie (lub na dole), gdzie wpisujesz prompty i ustawiasz parametry.
3.  **History (Historia)**: Pasek z miniaturami poprzednich generacji.
4.  **ThemeSwitch**: Przełącznik motywu (System → Dark → Light) w górnym pasku nawigacji — cykliczny przycisk.

---

## Strona Główna

Po uruchomieniu Rhino Image Studio zobaczysz stronę główną z dwoma zakładkami: **My Projects** i **Generations**.

### Zakładka "My Projects"

- **Lista projektów** — każdy projekt wyświetlany jest jako karta z miniaturką (ostatnia generacja), nazwą i datą. Projekty można filtrować polem wyszukiwania. Przypięte projekty wyświetlane są na górze, pozostałe sortowane od najnowszych.
- **Tworzenie projektu** — kliknij przycisk **"+ New Project"** w prawym górnym rogu. W modalu podaj nazwę i opcjonalny opis.
- **Zmiana nazwy** — kliknij **podwójnie** (double-click) na nazwę projektu na karcie. Pojawi się pole edycji: **Enter** lub kliknięcie poza pole zatwierdza, **Escape** anuluje. Max 100 znaków.
- **Przypinanie** — najedź na kartę i kliknij ikonę **pin**, aby przypiąć projekt na górze listy.
- **Usuwanie projektu** — najedź na kartę i kliknij ikonę **kosza**. Wyświetli się dialog potwierdzenia (ConfirmDialog) — usunięcie jest nieodwracalne.
- **Przejście do Studio** — kliknij kartę projektu (pojedyncze kliknięcie), aby otworzyć widok Studio z canvasem, edytorem i historią.

### Zakładka "Generations"

Globalna galeria wszystkich generacji ze wszystkich projektów wyświetlana w układzie **masonry** (kafelki o zmiennej wysokości). Sortowanie od najnowszej. Każdy kafelek pokazuje miniaturkę, nazwę projektu, datę i prompt. Przycisk **Load more** ładuje kolejne porcje (po 50). Kliknięcie kafelka otwiera Studio z tą konkretną generacją zaznaczoną (deep-link).

### Powiadomienia (Toast)

Operacje na stronie głównej (pin, rename, delete, błędy ładowania) sygnalizowane są powiadomieniami typu **toast** — krótkie komunikaty pojawiające się w rogu ekranu i znikające automatycznie.

---

## Konfiguracja (Settings)

Aby korzystać z Rhino Image Studio, musisz skonfigurować klucze API dla używanych modeli AI.

### Jak otworzyć ustawienia

Kliknij ikonę **koła zębatego** (gear) w prawym górnym rogu paska nawigacji. Otworzy się strona Settings.

### Klucze API

Na stronie Settings znajdują się pola do wprowadzenia kluczy API:

- **Gemini API Key** — wymagany do modeli Gemini 3.1 Flash i Gemini 3 Pro. Wpisz klucz, kliknij **Save**, a następnie **Verify** aby sprawdzić poprawność.
- **fal.ai API Key** — wymagany do modeli Seedream, Qwen Multi-Angle i Topaz Upscale. Wpisz klucz i zapisz.
- **OpenAI API Key** — wymagany do modelu GPT-Image 1.5 (obsługiwanego przez fal.ai). Wpisz klucz i zapisz.

Każdy klucz jest przechowywany lokalnie na maszynie za pomocą Windows DPAPI — nigdy nie jest wysyłany do zewnętrznych serwisów poza docelowym API.

### Ścieżka danych

Na dole strony Settings wyświetlana jest **Data Path** — lokalizacja folderu, w którym backend przechowuje projekty, generacje i pliki tymczasowe.

---

## Twój Pierwszy Render

### 1. Przygotuj widok w Rhino
Ustaw kamerę w Rhino tak, jak chcesz widzieć finalny obraz.
- Tryb wyświetlania można wybrać bezpośrednio przed przechwyceniem (patrz krok 2) — nie musisz zmieniać trybu w Rhino.
- Unikaj trybu Wireframe dla skomplikowanych modeli (zbyt wiele linii może zmylić model).

### 2. Capture (Przechwytywanie)
W panelu Assets znajdziesz dropdown **Display Mode** oraz przycisk **📷 New Capture** — umieszczone obok siebie.

**Display Mode** określa, w jakim trybie wyświetlania Rhino zostanie przechwycony viewport:

| Opcja | Opis |
|-------|------|
| **Viewport** (domyślny) | Przechwytuje dokładnie to, co widzisz w aktywnym viewporcie — 1:1 z bieżącym trybem Rhino |
| **Shaded** | Nadpisuje tryb na Shaded niezależnie od ustawień viewportu |
| **Rendered** | Nadpisuje tryb na Rendered |
| **Arctic** | Nadpisuje tryb na Arctic (neutralne, płaskie oświetlenie — dobre dla AI) |
| **Ghosted** | Nadpisuje tryb na Ghosted |
| **Pen** | Nadpisuje tryb na Pen (liniarowy) |

Ustaw żądany Display Mode, a następnie kliknij **📷 New Capture**. Twoja geometria pojawi się w oknie podglądu. To jest "baza", na której AI będzie pracować.

### 3. Opisz wizję (Prompting)
W polu tekstowym "Prompt" opisz, co chcesz zobaczyć.
- **Dobry przykład**: *"Modern concrete villa in a pine forest, rainy mood, cinematic lighting, photorealistic, 8k"*
- **Wskazówka**: Skup się na materiałach, oświetleniu i nastroju. Geometria pochodzi z Rhino, więc nie musisz jej dokładnie opisywać (np. "dom z płaskim dachem").

### 4. Generuj
Kliknij przycisk:
> **✨ Generate**

Pasek postępu pokaże status zadania. Po kilku-kilkunastu sekundach zobaczysz wynik.

### 5. Iteracja (Poprawki)
Nie podoba Ci się wynik?
- Zmień prompt (np. dodaj *"sunny day"* zamiast *"rainy"*).
- Zmień siłę wpływu AI (parametr **Strength** w ustawieniach zaawansowanych).
- Kliknij ponownie **Generate**.

Wszystkie wersje są zapisywane w Historii. Możesz do nich wrócić w każdej chwili.

---

## Zarządzanie generacjami

### Archiwizowanie
Najedź na thumbnail generacji w panelu Assets i kliknij ikonę **kosza** — generacja zostanie zarchiwizowana (nie usunięta). Pliki pozostają na dysku. Przed wykonaniem akcji wyświetlany jest branded dialog potwierdzenia.

### Zakładka Archived
Kliknij ikonę **Archive** (pudełko) w zakładkach Assets, aby zobaczyć zarchiwizowane generacje. Dla każdej masz dwie opcje:
- **Restore** (zielona ikona) — przywraca generację do głównej listy
- **Permanent Delete** (czerwona ikona) — trwale usuwa generację i pliki z dysku (nieodwracalne)

W przypadku błędów operacji (np. brak uprawnień do pliku, problem sieci) aplikacja pokazuje powiadomienie toast z komunikatem backendu zamiast ogólnego błędu. Wszystkie destrukcyjne akcje (usuwanie, trwałe kasowanie) wymagają potwierdzenia w dialogu (ConfirmDialog) z obsługą dostępności (focus trap, aria-labels).

---

## Porównanie A/B (Compare)

### Aktywacja
Kliknij ikonę **kolumn** (Columns) w pasku narzędzi nad canvasem. Przycisk pojawia się gdy masz co najmniej 2 obrazy w projekcie.

### Wybór obrazów
Pod sliderem pojawiają się dwa rzędy miniaturek:
- **Rząd A** — kliknij miniaturkę, aby ustawić ją jako Image A (lewa strona / baza)
- **Rząd B** — kliknij miniaturkę, aby ustawić ją jako Image B (prawa strona / overlay)

Miniaturki oznaczone są literami **C** (Capture) lub **G** (Generation).

### Regulacja przezroczystości
Nad sliderem widoczny jest suwak **B Opacity** (0-100%). Pozwala regulować przezroczystość Image B nakładanego na Image A:
- **100%** — standardowe porównanie (lewa: A, prawa: B, ostre cięcie sliderem)
- **50%** — po prawej stronie widoczny blend A i B
- **0%** — po obu stronach widoczny tylko Image A

### Wyjście z trybu porównania
Kliknij ponownie ikonę kolumn w pasku narzędzi.

---

## Inpainting (Maski)

Inpainting pozwala edytować **konkretne obszary** obrazu za pomocą masek. Każda maska ma własną instrukcję — Gemini edytuje tylko zamaskowane regiony, reszta pozostaje nienaruszona.

### Wymagania
- Model Gemini (3.1 Flash lub 3 Pro) — modele fal.ai (Seedream, GPT-Image) nie obsługują masek
- Capture lub generacja jako źródło

### Limity masek

| Model | Max masek | Max obrazów total | Formuła |
|-------|-----------|-------------------|---------|
| Gemini 3.1 Flash | 2 | 16 | source(1) + overlay(1) + refs ≤ 16 → max 14 referencji z maskami |
| Gemini 3 Pro | 8 | 14 | source(1) + overlay(1) + refs ≤ 14 → max 11 referencji z maskami |
| fal.ai (Seedream, GPT-Image) | 0 | - | Maski nieobsługiwane |

Wszystkie maski są kompozytowane w jeden obraz overlay (oryginał z kolorowymi maskami) — nie zajmują osobnych slotów. Budżet obrazów to: `2 (source + overlay) + referencje ≤ maxTotalImages`.

### Jak używać

1. Wybierz capture lub generację jako źródło
2. W panelu Editor, sekcja **Mask Layers**, kliknij **Add** aby dodać warstwę maski
3. Kliknij ikonę **pędzla** (Paintbrush) w toolbar canvasu aby wejść w tryb rysowania
4. Narysuj maskę na obrazie:
   - Każda warstwa maski ma przypisany **kolor** (czerwony, niebieski, zielony, żółty...) — maluj obszar do edycji w kolorze danej warstwy
   - Niezamalowane fragmenty pozostają bez zmian
5. Wpisz instrukcję dla maski, np. *"Replace with wooden texture"*
6. Dodaj kolejne maski dla innych regionów (opcjonalnie)
7. W głównym prompcie opisz ogólny kontekst
8. Kliknij **Generate**

### Narzędzia rysowania

- **Brush** — rysowanie maski (pędzel okrągły, rozmiar 5-200px)
- **Eraser** — wymazywanie fragmentów maski (przełączenie prawym przyciskiem lub przyciskiem w toolbarze)
- **Undo/Redo** — Ctrl+Z / Ctrl+Shift+Z (20 kroków dla 1K, 10 dla 4K)
- **Kolory warstw** — 8 automatycznie przypisanych kolorów (czerwony, niebieski, zielony, żółty, fioletowy, pomarańczowy, cyjan, różowy)

### Interakcja z innymi trybami

- Tryb masek i tryb porównania (Compare) **wzajemnie się wykluczają** — włączenie jednego wyłącza drugi
- Maski są czyszczone przy zmianie wybranego elementu (capture/generacja)
- Maski są automatycznie przycinane gdy zmiana modelu lub referencji zmniejsza dostępne sloty

### Historia masek

Po wygenerowaniu obrazu z maskami, dane masek (rysunek + instrukcje) są zapisywane w żądaniu generacji. Gdy wrócisz do tej generacji w historii:
- Maski automatycznie załadują się na canvas z oryginalnymi kolorami warstw i instrukcjami
- Możesz je edytować i ponownie wygenerować obraz

### Diagnostyka (Debug)

Najedź na thumbnail generacji i kliknij ikonę **Bug** — otworzy się modal ze szczegółami żądania wysłanego do AI:
- Prompt, model, aspect ratio, rozdzielczość
- Źródło (capture/generacja), referencje
- Maski (liczba, rozmiar, instrukcje)
- Przycisk **Copy JSON** kopiuje pełne dane do schowka

### Wskaźniki gotowości masek

Przy każdej warstwie maski widoczna jest kropka statusu:
- **Zielona** — maska gotowa (ma rysunek + instrukcję)
- **Amber** — niekompletna (brak rysunku lub instrukcji)

Pod przyciskiem Generate wyświetlany jest licznik *"X/Y masks ready"*.

### Wskazówki

- Opisuj instrukcje masek precyzyjnie — każda maska jest wysyłana do AI z numerem i opisem
- W głównym prompcie opisz kontekst całej sceny, a w maskach — zmiany lokalne

---

## Obrazy Referencyjne (Reference Images)

Panel **ReferencePanel** w widoku Studio pozwala dodać obrazy referencyjne, które AI wykorzysta jako kontekst wizualny (materiały, styl, obiekty otoczenia). Panel pojawia się automatycznie gdy wybrany model obsługuje referencje.

### Jak dodać referencje

- Kliknij przycisk **Upload** lub **przeciągnij pliki** (drag & drop) na panel referencji.
- Każdy dodany obraz wyświetlany jest jako **miniaturka** z podglądem.
- Aby usunąć referencję, kliknij ikonę **X** na miniaturce.

### Limity per model

Maksymalna liczba obrazów referencyjnych zależy od wybranego modelu (źródło: `models.ts`):

| Model | Max referencji |
|-------|---------------|
| Gemini 3.1 Flash | 14 |
| Gemini 3 Pro | 11 |
| Seedream v5 Lite | 9 |
| GPT-Image 1.5 | 4 |

> **Uwaga:** Przy użyciu masek inpaintingowych budżet obrazów to `source(1) + overlay(1) + referencje ≤ maxTotalImages`, więc efektywna liczba referencji może być mniejsza.

### Przechowywanie

Obrazy referencyjne są zapisywane **per projekt** i **persystują między sesjami** — nie trzeba ich dodawać ponownie po restarcie aplikacji.

---

## Dostępne Modele AI

Rhino Image Studio obsługuje kilka modeli AI do generowania i edycji obrazów. Wybór modelu odbywa się w panelu **Inspector** (ModelSelector) w widoku Studio.

| Model | Dostawca | Opis | Maski | Referencje |
|-------|----------|------|-------|------------|
| **Gemini 3.1 Flash** | Google | Szybka generacja, rozszerzone AR i rozdzielczości | Tak (max 2) | Tak (max 14) |
| **Gemini 3 Pro (Preview)** | Google | Wysoka jakość, obsługa 2K/4K | Tak (max 8) | Tak (max 11) |
| **Seedream v5 Lite** | ByteDance (fal.ai) | Edycja obrazów wysokiej jakości (do 3K) | Nie | Tak (max 9) |
| **GPT-Image 1.5** | OpenAI (fal.ai) | Edycja z kontrolą jakości i wierności | Nie | Tak (max 4) |

### GPT-Image 1.5 — dodatkowe opcje

Model GPT-Image 1.5 udostępnia dwa dodatkowe parametry w panelu Inspector:
- **Quality** (Low / Medium / High) — kontrola jakości generowanego obrazu
- **Fidelity** (Low / High) — stopień wierności względem źródła

### Panel Inspector

Panel Inspector po prawej stronie widoku Studio został zrefaktoryzowany na osobne komponenty:
- **ModeSelector** — wybór trybu pracy (Generate, Refine, Pan, Upscale)
- **ModelSelector** — wybór modelu AI z dynamicznym dostosowaniem parametrów (AR, rozdzielczość, maski) do wybranego modelu

---

## Funkcje Zaawansowane

### Pan (Move Camera)
Ta funkcja pozwala wygenerować widoki obiektu z różnych kątów kamery, zachowując spójność wizualną.

1. Wybierz capture lub wygenerowany obraz jako źródło.
2. Przejdź do zakładki **Pan** w panelu Editor.
3. Użyj **Quick Presets** (Front, Right, Back, Left, 3/4, Top, Low) lub dostosuj ręcznie:
   - **Camera Rotation** (-180° do +180°): obrót kamery wokół obiektu (lewo/prawo)
   - **Camera Elevation** (-30° do +90°): wysokość kamery (nisko/wysoko)
   - **Camera Distance** (0-10): odległość kamery (Wide/Medium/Close)
4. Kliknij **Move Camera**.

> **Tip:** Przycisk **Reset** przywraca domyślne ustawienia (Front, Eye Level, Medium distance).

### Upscaling (Powiększanie)
Aby przygotować obraz do prezentacji:
1. Wybierz najlepszą wersję.
2. Kliknij **Upscale**.
3. Obraz zostanie przetworzony do wyższej rozdzielczości (np. 4K) z dodaniem detali.
