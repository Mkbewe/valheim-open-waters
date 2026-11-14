# 04 — Next steps i otwarte decyzje

Data: 2026-11-13 (aktualizacja: 2026-11-14 — pivot na modyfikację szumu + limit biomów per wyspa)

## 1. Otwarte decyzje

Przed kodowaniem warto potwierdzić:

1. **Rozmiar świata** — DECYZJA: własna implementacja (bez EWS), zakres 6000–10000 m.
2. **Zakres suwaków** — DECYZJA: gęstość lądu, rozmiar świata, limit biomów na wyspie, osobny limit wyspy startowej, checkbox „góry nie liczą się do limitu". Do potwierdzenia domyślne wartości: 15%, 10000 m, bez limitu, bez limitu, checkbox zaznaczony.
3. **Ochrona progresji** — propozycja: `AshLands`/`DeepNorth` nigdy nie są nadpisywane; żaden biom lądowy nie może zniknąć z całego świata (cofanie wymuszania na najmniejszej wyspie z tym biomem). Potwierdzić.
4. **Podgląd** — przybliżony (szybki, replikacja modyfikatora i analizy na siatce 256²/512²) czy dokładny (tymczasowy `WorldGenerator`)?
5. **UI** — IMGUI w stylu Valheima czy natywne Unity UI?
6. **Języki** — UI tylko polski, tylko angielski, czy oba?
7. **Nazwa autora / paczka Thunderstore** — np. `Kacper-Open_Waters`; potrzebny ostateczny nick.
8. **Czy mod ma być opcjonalny dla istniejących światów** (sidecar + ostrzeżenie) i jak obsłużyć usuwanie świata.
9. **Stałe modyfikatora gęstości** — `Patchiness`, `PeakPreservation`, promień fade centrum (800–1600 m), siatka kalibracji (512²). Strojenie wizualne na kilku seedach.
10. **Szczegóły wymuszania biomów** — wybór „zachowanych" biomów: top-N po powierzchni (obecny design) czy priorytet biomów progresji; nadpisywanie na dominantę czy na najbliższy zachowany biom; siatka 1024² czy 2048² (artefakty granic).
11. **Zakres checkboxa gór** — gdy odznaczony, góry są liczone i mogą zostać nadpisane (obecny design). Potwierdzić, że to OK (duża zmiana rzeźby).

## 2. Plan wdrożenia

### Faza 0 — szkielet (po akceptacji)

- Utworzyć `D:\Projekty\valheim-open-waters`:
  - `OpenWaters.sln`
  - `src/OpenWaters/OpenWaters.csproj`
  - `Directory.Build.props` z `ValheimDir`
  - `.gitignore`
  - `LICENSE` (MIT), `THIRD-PARTY-NOTICES.md`
- Target: `netstandard2.1`.
- Referencje: BepInEx, Harmony, assembly_valheim, UnityEngine.*.
- Post-build: kopia DLL do `BepInEx\plugins\OpenWaters\`.
- Plugin „hello world" + log, weryfikacja w `LogOutput.log`.

### Faza 1 — gęstość lądu (modyfikacja szumu)

- `Settings.cs`: `LandCoverage`, `WorldRadius`, `Patchiness`, `PeakPreservation` + wersja/seed hash.
- `TerrainModifier.cs`:
  - maska kontynentalna z seeda (częstotliwość skalowana `WorldRadius/10000`),
  - `Bias` kalibrowany binary searchem pod `LandCoverage` (siatka 512² surowych próbek),
  - fade centrum 800–1600 m,
  - łagodna ochrona szczytów (`PeakPreservation`).
- `Patches.cs`:
  - prefix `WorldGenerator.Initialize(World)` — zapis seeda i ustawień,
  - postfix `GetBaseHeight` — modyfikator; skip `menuTerrain` i `IsSamplingRaw`,
  - flagi `IsSamplingRaw` / `IsAnalyzingIslands`.
- Tymczasowy config BepInEx zamiast UI: `LandCoverage`, `WorldRadius`.
- Test na świeżym świecie: % lądu, naturalne brzegi, góry, spawn, brak błędów w logu.

### Faza 2 — rozmiar świata (własna implementacja)

- Własne transpilery i patche, bez EWS:
  - stałe `10000/10500/10490` w `GetBaseHeight`, `10500` w `GetBiomeHeight`, `10150` w `GetAshlandsHeight`,
  - progi dystansów biomów w `GetBiome` skalowane przez `WorldRadius / 10000`,
  - `IsDeepnorth`, `CreateAshlandsGap`, `CreateDeepNorthGap`, `ashlandsMinDistance`, `ashlandsYOffset`,
  - edge kill/force, woda, wiatr, `_WaterEdge`,
  - minimapa i ewentualne mapowanie `AltBiomeWorldData`.
- Test mniejszego świata (8000, 6000): biomy, minimapa, brak index out of bounds.
- Sprawdzenie Ashlands/Deep North po skalowaniu.

### Faza 3 — analiza wysp i limit biomów

- `IslandAnalysis.cs`:
  - siatka 1024² (opcjonalnie 2048²), klasyfikacja lądu (z modyfikatorem gęstości, bez wymuszania),
  - connected components 4-sąsiedztwo,
  - histogram biomów i obszar per komponent,
  - wykrycie wyspy startowej (centrum/najbliżej centrum),
  - wybór biomów do zachowania (top-N, remis po enumie),
  - `byte[] ForcedBiome`.
- `BiomePatches.cs`: postfix `GetBiome` (skip Ocean, AshLands, DeepNorth, opcjonalnie Mountain).
- Ochrona progresji: brak wyginięcia biomu + log ostrzeżenia.
- Test: limit 1/2, wyspa startowa, checkbox gór, minimapa/roślinność/heightmapa.

### Faza 4 — UI przy tworzeniu świata

- Panel IMGUI na `FejdStartup.m_createWorldPanel`.
- Odczyt `m_newWorldSeed` i `m_newWorldName`.
- Suwaki + dropdowny limitów + checkbox gór + podgląd (siatka 256²/512²) + ostrzeżenia.
- Zapis sidecara.
- Test: utworzenie świata z panelu, ustawienia trafiają do świata.

### Faza 5 — per-world settings i sync

- Sidecar `*.openwaters.json` obok `World.GetMetaPath()`.
- Odczyt przy `ZNet.SetServer` / `Initialize`.
- Synchronizacja RPC w handshake `PeerInfo`.
- Weryfikacja wersji moda.
- Test LAN: serwer + klient, ten sam teren i te same wymuszone biomy.

### Faza 6 — polish i paczka

- Cache kalibracji i analizy (opcjonalnie).
- Ostrzeżenia dla starych światów.
- README, manifest.json, ikona.
- Testy na kilku seedach (dobór stałych).
- Decyzja o publikacji (Thunderstore / GitHub).

## 3. Checklista testów

### Teren i wyspy

- [ ] Vanilla (bez moda) — punkt odniesienia.
- [ ] `LandCoverage` ~30–40% wygląda podobnie do vanilla.
- [ ] 15% — wyraźnie mniej wysp, więcej oceanu.
- [ ] 5% — bardzo mało lądu, ale świat grywalny (brak „półek" i dziwnych płycizn).
- [ ] Linie brzegowe naturalne; brak „ścian" i binarnych/kwadratowych artefaktów.
- [ ] Wyspa startowa zawsze istnieje; spawn i StartTemple na lądzie.
- [ ] Góry występują przy 15% (PeakPreservation działa); przy 5% — ocena, czy nie znikają całkowicie.
- [ ] Mistlands / Ashlands / Deep North osiągalne (bossy, progresja).
- [ ] Trader i bossy znajdują miejsca.
- [ ] Rzeki nie tworzą artefaktów na nowych brzegach.

### Limit biomów

- [ ] Bez limitu — tylko efekt gęstości lądu, brak wymuszania.
- [ ] Max 1 — wyspy mono-biomowe (poza górami/polarami, jeśli wyłączone).
- [ ] Max 2 — wyspy mają najwyżej 2 biomy.
- [ ] Wyspa startowa używa `StartBiomeLimit`; zawsze istnieje.
- [ ] Checkbox gór zaznaczony — góry nie liczone i nienadpisywane.
- [ ] Checkbox gór odznaczony — góry liczone i mogą być nadpisane.
- [ ] AshLands/DeepNorth nietknięte.
- [ ] Żaden biom lądowy nie znika z całego świata; warning przy złagodzeniu limitu.
- [ ] Minimapa, roślinność i heightmapa spójne z wymuszonym biomem.
- [ ] Brak klockowatych granic przy brzegach wysp (albo N=2048²).

### Rozmiar świata

- [ ] 10000 m — brak różnic względem vanilla.
- [ ] 8000 m — mapa mniejsza, biomy nadal obecne.
- [ ] 6000 m — brak crashy/index out of bounds, minimapa działa.
- [ ] Edge/woda/wiatr działają na nowym promieniu.
- [ ] Modyfikator gęstości skaluje się poprawnie z `WorldRadius`.

### Multiplayer

- [ ] Klient bez moda jest odrzucany z czytelnym komunikatem.
- [ ] Klient z modą widzi identyczny teren i biomy co serwer.
- [ ] Różne wersje moda -> czytelny błąd.
- [ ] Zmiana ustawień na serwerze a join klienta -> klient dostaje nowe ustawienia.
- [ ] Stary świat + nowe ustawienia -> ostrzeżenie.

### Wydajność

- [ ] Kalibracja + analiza wysp < ~3 s (cel).
- [ ] Brak spadków FPS podczas generacji stref.
- [ ] Czas ładowania świata akceptowalny.
- [ ] Postfiks `GetBaseHeight` nie wpływa mierzalnie na FPS.

## 4. Przydatne komendy

Build:

```powershell
& "C:\Users\Kacper Bobrowski\.dotnet\dotnet.exe" build "D:\Projekty\valheim-open-waters\OpenWaters.sln" -c Debug
```

Log BepInEx:

```powershell
Get-Content "D:\Lunchery\Steam\steamapps\common\Valheim\BepInEx\LogOutput.log" -Tail 100
```

Uruchomienie gry bez Steama (do szybkiego testu):

```powershell
Start-Process "D:\Lunchery\Steam\steamapps\common\Valheim\valheim.exe" -WorkingDirectory "D:\Lunchery\Steam\steamapps\common\Valheim"
```

Decompilacja (jeśli trzeba wrócić do źródeł gry):

```powershell
& "C:\Users\KACPER~1\AppData\Local\Temp\opencode\valheim-research\ilspy\pkg-win\tools\any\win-x64\ilspycmd.exe" `
  "D:\Lunchery\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll" `
  -t WorldGenerator -o "C:\Users\KACPER~1\AppData\Local\Temp\opencode\valheim-research\decompiled"
```

## 5. Stan na dziś

- Research: zrobiony; 2026-11-14 doprecyzowano `GetBaseHeight`, `GetBiome`/cache, `AltBiomeWorldData` i StartTemple.
- Design: zaktualizowany — modyfikacja szumu zamiast usuwania wysp + limit biomów per wyspa.
- Kod: **nie napisany**.
- Projekt: tylko dokumentacja w `D:\Projekty\valheim-open-waters\docs`.
- BepInEx + .NET SDK: zainstalowane i zweryfikowane.
