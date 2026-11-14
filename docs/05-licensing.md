# 05 — Licencja i pochodzenie kodu

Data: 2026-11-13

## 1. Decyzja

- Nasz kod: **MIT**.
- Kod piszemy samodzielnie. Z innych modów (Expand World Size, Better Continents) **nie kopiujemy kodu** — patrzymy tylko, jak rozwiązują problem.
- Nie kopiujemy kodu gry (`assembly_valheim.dll` itd.). Dekompilacja służy wyłącznie do poznania API i zachowania.
- Zależności runtime: **BepInEx** (LGPL-2.1) i **Harmony** (MIT) — linkowane, nie kopiowane. Użytkownik instaluje BepInEx osobno (Thunderstore dependency).

## 2. Dlaczego MIT

- Najprostsza licencja permisywna.
- Wymaga tylko zachowania noty copyright i tekstu licencji w kopiach/substantial portions.
- Pozwala innym używać kodu, także w projektach zamkniętych.
- Nie wymaga pliku `NOTICE` ani oznaczania zmian (inaczej niż Apache-2.0).
- Brak grantu patentowego — dla małego moda to zwykle akceptowalne.

Alternatywa: **Apache-2.0**, jeśli zależy nam na formalnym grantcie patentowym i pliku `NOTICE`. Wtedy trzeba oznaczać zmiany i dołączać `NOTICE`.

## 3. Licencje innych modów

### Expand World Size — Unlicense

- Public domain dedication.
- Brak obowiązku attribution.
- Można kopiować, modyfikować, sprzedawać, distribuować.
- Mimo to podajemy kredyt w `THIRD-PARTY-NOTICES.md` i README jako inspirację.
- Nasza decyzja: **nie kopiujemy kodu**, tylko się inspirujemy.

### Better Continents — LGPL-2.1

- Kod objęty LGPL-2.1.
- Nie kopiujemy go do projektu MIT.
- Możemy się uczyć wzorców (IMGUI, sidecar, handshake), ale implementacja jest własna.
- Gdybyśmy kiedyś skopiowali kod LGPL, ta część musiałaby pozostać LGPL-2.1, z dostępem do źródeł i notą licencyjną.

### BepInEx — LGPL-2.1

- BepInEx jest zależnością runtime, nie kopiujemy jego kodu.
- Nie bundlujemy BepInEx w naszej paczce; użytkownik instaluje BepInExPack Valheim.
- Jeśli kiedykolwiek bundlowalibyśmy BepInEx, trzeba dołączyć licencję i źródła.

### Harmony — MIT

- Zależność runtime; wymaga zachowania noty copyright przy redistribucji kodu.
- Nie bundlujemy Harmony.

## 4. Pliki w repo

Wymagane:

- `LICENSE` — pełny tekst MIT z rokiem i autorem.
- `THIRD-PARTY-NOTICES.md` — kredyty i zależności.
- `README.md` — sekcja `Credits` / `Licenses`.

Szablon `THIRD-PARTY-NOTICES.md`:

```text
Open Waters
Copyright (c) 2026 <autor>

Ten projekt jest objęty licencją MIT (patrz LICENSE).

Inspiracje i wiedza (bez kopiowania kodu):
- Expand World Size by JereKuusela — Unlicense
  https://github.com/JereKuusela/valheim-expand_world_size
- Better Continents by billw2012 / JereKuusela — LGPL-2.1 (tylko wgląd w rozwiązania)
  https://github.com/JereKuusela/BetterContinents

Zależności runtime (nie bundlowane):
- BepInEx — LGPL-2.1
- Harmony (HarmonyX) — MIT
```

## 5. Zasady na przyszłość

1. Nie wklejaj kodu z innych modów bez sprawdzenia licencji.
2. Nie wklejaj kodu z dekompilacji gry w ilości większej niż niezbędna do zrozumienia API.
3. Nowe zależności (NuGet, DLL) — sprawdź licencję i dopisz do `THIRD-PARTY-NOTICES.md`.
4. Ikona i assety: własne lub CC0; nie bierz grafiki z gry ani z innych modów bez zgody.
5. Przy publikacji na Thunderstore/Nexus: dołącz `LICENSE` i link do repo.

## 6. Checklista przed publikacją

- [ ] `LICENSE` (MIT) w repo i w paczce.
- [ ] `THIRD-PARTY-NOTICES.md` zaktualizowany.
- [ ] README: sekcja Credits.
- [ ] Brak DLL-i gry i BepInEx w paczce.
- [ ] Manifest Thunderstore: dependency BepInExPack (i inne, jeśli użyte).
- [ ] Link do repo w `website_url`.
