# 03 — Design: Open Waters

Data: 2026-11-13 (aktualizacja: 2026-11-14 — pivot na modyfikację szumu + limit biomów per wyspa)

## 1. Cel

- Znacząco zmniejszyć liczbę wysp i zwiększyć udział otwartego oceanu.
- **Nie przepisywać** generatora: modyfikujemy `GetBaseHeight` i `GetBiome`, resztę pipeline'u zostawiamy vanilla.
- Zachować naturalny styl i „organiczność" terenu; linie brzegowe nadal są przecięciem terenu z poziomem wody.
- Dać graczowi kontrolę: gęstość lądu, rozmiar świata, limit biomów na wyspę (osobno dla startowej).
- Działać w single-playerze i multiplayerze (LAN / serwer dedykowany).

### 1.1. Czego już nie gwarantujemy

Poprzednia koncepcja zakładała usuwanie całych wysp i zachowanie ocalałych 1:1. Po decyzji z 2026-11-14:

- gęstość lądu regulujemy **modyfikatorem szumu** (obniżanie terenu / maska kontynentalna), więc linie brzegowe i wysokość terenu różnią się od vanilla — nie ma „ocalałych wysp 1:1",
- limit biomów jest wymuszany przez **nadpisywanie biomu komórek** na wyspie, co zmienia wygląd jej wnętrza (inne funkcje wysokości, roślinność, środowisko),
- wyspy nie są usuwane dyskretnie; wyspa „znika", gdy jej teren schodzi pod poziom wody.

Świadoma wymiana: mniej wysp i kontrola biomów zamiast idealnej zgodności z vanilla.

## 2. Ustalone decyzje

| Temat | Decyzja |
|---|---|
| Nazwa | **Open Waters** |
| Katalog | `D:\Projekty\valheim-open-waters` |
| Assembly / namespace | `OpenWaters` |
| Plugin GUID | `valheim.openwaters` |
| Tryb gry | multiplayer LAN / dedykowany |
| UI | panel przy tworzeniu świata + podgląd mapy |
| Zakres v1 | **tylko liczba wysp**; limity biomów w kolejnej wersji |
| Tryb domyślny | **Vanilla** — mod nie wpływa na generator (patch aktywne tylko dla Few/Medium) |
| Presety v1 | **Few ≈ 8% lądu, Medium ≈ 15% lądu, Vanilla bez zmian** (wartości startowe do strojenia) |
| Gęstość lądu | **modyfikacja szumu `GetBaseHeight`** (bez usuwania wysp) |
| Gęstość lądu (UI) | suwak 5–40% (domyślnie ~15%) |
| Rozmiar świata | suwak, własna implementacja, 6000–10000 m |
| Limit biomów per wyspa | **bez limitu / max 1 / max 2 / max 3** (domyślnie bez limitu) |
| Limit wyspy startowej | osobne ustawienie: bez limitu / 1 / 2 / 3 (domyślnie bez limitu) |
| Wyspa startowa (biomy) | zawsze zachowana; nadmiarowe biomy przycinane wg `StartBiomeLimit` (późniejsza wersja) |
| Sposób wymuszania limitu | nadpisanie biomu nadmiarowych komórek (bez usuwania wysp) |
| Próg liczenia biomów | **brak** — każdy biom się liczy (nawet pojedyncze komórki) |
| Góry | checkbox „góry nie liczą się do limitu"; gdy wyłączony — góry są liczone i mogą zostać nadpisane |
| Wyspa startowa | może się zmniejszać, ale musi pozostać grywalna: rdzeń ~700–800 m bez modyfikacji, fade do ~1600–1800 m, minimum ~0,10–0,15 km² ciągłego lądu (adaptacyjnie) |
| Biomy polarne | AshLands i DeepNorth nigdy nie są nadpisywane (zawsze wyłączone z liczenia i wymuszania) |
| Ochrona progresji | żaden biom lądowy nie może zniknąć z całego świata; w razie konfliktu wymuszanie jest częściowo cofane na najmniejszej wyspie z tym biomem |
| MinIslandArea | usunięte z zakresu — nieaktualne przy podejściu szumowym |
| Ustawienia | per świat |
| Synchronizacja | serwer -> klienci |
| Kod | samodzielny; bez kopiowania kodu innych modów |
| Podejście | EWS/Better Continents tylko jako referencja do zrozumienia |
| Licencja | MIT (nasz kod) |

## 3. Architektura

```text
┌──────────────────────────────────────────────────────────────┐
│ OpenWaters plugin (BepInEx + HarmonyX)                       │
│                                                              │
│  Settings (per world, JSON)                                  │
│    - LandCoverage 0.05..0.40                                 │
│    - WorldRadius (własna implementacja)                      │
│    - BiomeLimit (0=bez limitu, 1..3)                         │
│    - StartBiomeLimit (0..3)                                  │
│    - ExcludeMountains (bool)                                 │
│    - Version / seed hash                                     │
│                                                              │
│  TerrainModifier                                             │
│    - bias + maska kontynentalna na GetBaseHeight             │
│    - kalibracja bias pod LandCoverage (siatka 512²)          │
│    - strefa ochronna centrum (start)                         │
│    - cache per seed+ustawienia+wersje                        │
│                                                              │
│  IslandAnalysis                                              │
│    - budowana w Pregenerate (prefix)                         │
│    - siatka ~1024² po całym świecie                          │
│    - connected components (4-sąsiedztwo)                     │
│    - histogram biomów per komponent                          │
│    - wybór biomów do zachowania + byte[] ForcedBiome         │
│                                                              │
│  Patches                                                     │
│    - GetBaseHeight postfix: gęstość lądu (pomija menu)       │
│    - GetBiome postfix: wymuszony biom wyspy (pomija Ocean)   │
│    - Pregenerate prefix: analiza wysp                        │
│    - Initialize prefix: seed + ustawienia świata             │
│                                                              │
│  UI                                                          │
│    - panel na ekranie tworzenia świata                       │
│    - suwaki + limity biomów + checkbox gór + podgląd         │
│    - zapis ustawień do sidecara                              │
│                                                              │
│  Network                                                     │
│    - handshake RPC przed PeerInfo                            │
│    - wersja moda + ustawienia                                │
└──────────────────────────────────────────────────────────────┘
```

## 4. Gęstość lądu (modyfikacja szumu)

### 4.1. Punkt zaczepienia

`WorldGenerator.GetBaseHeight(float wx, float wy, bool menuTerrain)` jest prywatne — patchujemy Harmony **postfixem**.

- Pomiń `menuTerrain == true` (tło menu).
- Pomiń, gdy trwa „surowe" próbkowanie kalibracyjne (flaga `IsSamplingRaw`) — wtedy liczymy modyfikator jawnie w kodzie.
- Ustawienia i seed świata zapamiętujemy w **prefixie `WorldGenerator.Initialize(World world)`**; konstruktor (i `Pregenerate`) wykonuje się przed przypisaniem `m_instance`, więc nie można polegać na `WorldGenerator.instance` w trakcie budowy.

Vanilla `GetBaseHeight` zawiera już „bandę" archipelagową (linie 904–909 w dekompilacji) wyłączaną w promieniu ~744 m i włączaną do ~1000 m od centrum — to vanilla-owy mechanizm ochrony wyspy startowej. Nasz modyfikator musi robić to samo dla siebie.

### 4.2. Modyfikator (MVP, stałe do strojenia)

```text
raw   = vanilla GetBaseHeight(wx, wy, menuTerrain: false)
d     = length(wx, wy)
m     = 0..1  niskoczęstotliwościowy szum z seeda (częstotliwość skalowana WorldRadius/10000)
fade  = SmoothStep(800, 1600, d)          // 0 blisko centrum, 1 dalej
peak  = SmoothStep(0.30, 0.45, raw)       // 1 dla szczytów
bias' = Bias * (1 - PeakPreservation * peak)
mod   = bias' * Lerp(1, m, Patchiness) * fade
result = raw - mod
```

- `Bias` — kalibrowany pod `LandCoverage` (im wyższy, tym mniej lądu).
- `m` (maska kontynentalna) — decyduje, **gdzie** obniżenie jest silniejsze; tworzy duże obszary otwartego oceanu zamiast równomiernego „odchudzenia" wszystkich wysp.
- `Patchiness` (0..1) — 0: równomierne podniesienie poziomu wody; 1: maksymalna „platowość" oceanów. Wartość startowa ~0.5–0.7, poza UI w MVP.
- `PeakPreservation` (0..1) — łagodna ochrona szczytów, żeby przy niskim % lądu nie zniknęły wszystkie góry. Start ~0.5.
- `fade` — strefa startowa bez obniżania (ląd gwarantowany pod StartTemple/spawn).

Wszystko deterministyczne: ten sam seed + ustawienia = ten sam teren na serwerze i kliencie.

### 4.3. Kalibracja pod `LandCoverage`

- Presety v1: `Few` -> target `0.08`, `Medium` -> target `0.15`; `Vanilla` nieaktywna (`0`).
- Wartości są startowe — po testach na kilku seedach można je dostroić bez zmiany logiki.
- Próbkujemy **surowe** `GetBaseHeight` na siatce 512×512 w kole `WorldRadius`, zapisujemy też `m` i `fade` dla każdej próbki.
- Udział lądu = odsetek próbek, gdzie `raw - mod > 0.02` (próg oceanu z `GetBiome`).
- `Bias` znajdujemy binary searchem (funkcja monotoniczna); ~12 iteracji po gotowych próbkach = tanio.
- Wynik cache'ujemy na dysku: klucz = seed + WorldRadius + LandCoverage + Patchiness + PeakPreservation + wersja moda + wersja gry.
- Podgląd w UI używa tej samej kalibracji na mniejszej siatce (256²).

### 4.4. Co za tym idzie w pipeline

Modyfikacja `GetBaseHeight` automatycznie wpływa na:

- klasyfikację Ocean/ląd w `GetBiome` (base <= 0.02 -> Ocean),
- wysokości wszystkich biomów w `GetBiomeHeight` (bo dostają już zmieniony base),
- rzeki i strumienie (`Pregenerate` używa `GetBaseHeight`),
- rozmieszczanie lokacji, startu i spawnów (brak lądu = brak miejsca),
- mapę biomów/minimapę (`AltBiomeWorldData` próbkuje `GetBiome` + `GetBiomeHeight`).

Nie potrzebujemy osobnej maski `ForceOcean` ani sztucznego „usuwania" komponentów: wyspa znika, bo jej teren schodzi pod wodę.

### 4.5. Znane konsekwencje

- **Brak 1:1**: linie brzegowe wysp przesuwają się, teren jest niższy; przy dużym obniżeniu giną góry (łagodzone przez `PeakPreservation`).
- **Wyspa startowa może się zmniejszyć** (zewnętrzne partie mogą zniknąć), ale rdzeń wokół centrum pozostaje niemodyfikowany; po kalibracji sprawdzamy minimalną powierzchnię ciągłego lądu i w razie potrzeby lokalnie zmniejszamy modyfikator.
- Przy dużym `Patchiness` mogą powstać szerokie, płytkie przejścia (łagodne stoki pod wodą) — akceptowalne wizualnie, do oceny na podglądzie.
- Stare światy: nowe strefy będą się różnić od już wygenerowanych (szew na granicy stref). Zalecany świeży świat + ostrzeżenie.
- Menu background nietknięty (skip `menuTerrain`).
- Rzeki mogą wymagać korekty, jeśli zaczną „wisieć" na granicy nowego oceanu — do testów.

### 4.6. Dlaczego nie usuwanie wysp

- Brak dyskretnych decyzji „tę wyspę usuwamy" i brak binarnych granic maski.
- Brak artefaktów krawędziowych (wyspa albo cała, albo cała pod wodą).
- Prostszy pipeline: jeden modyfikator zamiast maski + dwóch postfiksów wymuszających ocean.
- Minus: nie da się zachować ocalałych wysp 1:1 (świadomie zaakceptowane).

## 5. Limit biomów per wyspa

### 5.1. Detekcja wysp

W prefixie `WorldGenerator.Pregenerate()` (po ustawieniu offsetów seeda, przed rzekami):

1. Siatka `N=1024` (komórka ~20,5 m dla 21 km); ewentualnie 2048², jeśli artefakty granic będą widoczne.
2. Dla każdej komórki klasyfikacja identyczna jak vanilla:
   - `base = GetBaseHeight(wx, wy, false)` — **z aktywnym modyfikatorem gęstości**,
   - `base <= 0.02` -> ocean,
   - w przeciwnym razie `biome = GetBiome(wx, wy)` i `height = GetBiomeHeight(biome, wx, wy, out _, false)`; `height > 30` -> ląd, inaczej ocean.
3. Connected components po 4-sąsiedztwie -> etykiety wysp.
4. Dla każdego komponentu zapisujemy:
   - `areaCells`,
   - histogram biomów (`Dictionary<Biome,int>`) — **bez wymuszania** (flaga `IsAnalyzingIslands` wyłącza `GetBiome` postfix, ale zostawia modyfikator gęstości),
   - czy zawiera/obejmuje centrum świata (lub jest najbliżej centrum) -> wyspa startowa.

Uwaga: komórki `AshLands`/`DeepNorth` mogą wypaść jako ocean wg tej reguły (w vanilla `GetBiome` sprawdza je przed oceanem). To akceptowalne — bieguny są wyłączone z liczenia i wymuszania biomów.

Podczas analizy nie wolno korzystać z prywatnego `GetBiome(Vector2s)` (cache `s_cachedBiomes`) — próbkujemy publiczne `GetBiome(float, float)`. Cache i tak jest czyszczony w konstruktorze generatora, ale nie chcemy go zatruwać wartościami bez wymuszania.

### 5.2. Liczenie biomów

- Liczą się **wszystkie** biomy lądowe na wyspie; brak progu powierzchni (nawet 1 komórka).
- `Ocean` nie jest liczony (nie jest biomem wyspy).
- Jeśli checkbox **„góry nie liczą się do limitu"** jest zaznaczony: `Mountain` nie jest liczony ani nadpisywany.
- `AshLands` i `DeepNorth` są **zawsze** wyłączone z liczenia i nadpisywania (ochrona biegunów i progresji), niezależnie od checkboxa gór.
- Reszta (Meadows, BlackForest, Swamp, Plains, Mistlands) liczy się normalnie.

### 5.3. Semantyka limitu

- `BiomeLimit = 0` -> bez limitu (wyspa zostaje taka, jaka wyszła z szumu).
- `BiomeLimit = N` (1..3): jeśli wyspa ma `> N` policzonych biomów:
  1. Wybieramy `N` biomów do zachowania: największe po liczbie komórek; remis rozstrzyga wartość enuma (stabilnie, deterministycznie).
  2. Wszystkie komórki pozostałych biomów nadpisujemy na **największy z zachowanych** (dominujący).
  3. Efekt: wyspa ma najwyżej `N` biomów; dla `N=1` staje się mono-biomowa.
- Wyspa startowa używa `StartBiomeLimit` (własne 0..3). Przekroczenie -> to samo nadpisywanie („przycinanie" jej biomów). Wyspa nigdy nie jest usuwana.
- Nadpisujemy tylko komórki lądowe; `Ocean` nigdy nie jest wymuszany.

### 5.4. Reprezentacja i aplikacja

- `byte[] ForcedBiome` o rozmiarze `N*N`:
  - `0` = bez zmian,
  - `biomeId + 1` = wymuszony biom.
- Postfix na publicznym `WorldGenerator.GetBiome(float wx, float wy, float oceanLevel = 0.02f, bool waterAlwaysOcean = false)`:
  - pomiń `menuTerrain` i analizę (`IsAnalyzingIslands`),
  - jeśli `__result == Ocean` -> nic nie rób,
  - jeśli `ExcludeMountains && __result == Mountain` -> nic nie rób,
  - jeśli `__result` to `AshLands`/`DeepNorth` -> nic nie rób,
  - jeśli `ForcedBiome[cell] != 0` -> `__result = ForcedBiome[cell] - 1`.
- Wymuszony biom propaguje się dalej automatycznie:
  - `Heightmap.GetBiome` deleguje do `WorldGenerator.GetBiome`,
  - `WorldGenerator.GetHeight` = `GetBiome` + `GetBiomeHeight`,
  - `AltBiomeWorldData` buduje wysokości i mapę przez `GetBiome` + `GetBiomeHeight`,
  - dzięki temu minimapa, roślinność i heightmapa są spójne z wymuszonym biomem.
- Cache `s_cachedBiomes` / `s_cachedBiomeAreas` jest czyszczony w konstruktorze generatora; wymuszone wartości trafiają do cache'u spójnie. Nie patchujemy prywatnego `GetBiome(Vector2s)`.

### 5.5. Ochrona progresji

- **Bieguny**: `AshLands`/`DeepNorth` nigdy nie są nadpisywane.
- **Brak wyginięcia biomu**: po policzeniu wymuszeń sprawdzamy, czy każdy biom lądowy nadal istnieje gdziekolwiek na świecie. Jeśli nie — cofamy nadpisywanie komórek tego biomu na najmniejszej wyspie, która go zawiera (tylko dla tego biomu), i logujemy warning. Limit może wtedy nie być w pełni dotrzymany na tej wyspie — to świadomy priorytet progresji.
- **Wyspa startowa**: strefa `fade` w modyfikatorze gęstości gwarantuje ląd w pobliżu centrum; spawn/StartTemple do walidacji w testach.
- **Podgląd**: ostrzeżenie, jeśli któryś biom zniknie lub limit zostanie złagodzony.

### 5.6. Znane konsekwencje

- Nadpisywanie biomu zmienia wnętrze wyspy (wysokość, roślinność, dźwięki/środowisko) — to jest zamierzone.
- Wymuszony biom zmienia też wysokości komórek (np. Swamp/Mistlands mają inne funkcje wysokości); przy brzegach mogą powstać lokalne różnice względem analizy lądu — do weryfikacji w testach.
- Gdy góry są liczone (checkbox odznaczony) i zostaną wybrane jako biom do zachowania, nadpisywanie może objąć całą wyspę biomem Mountain — to skrajny przypadek, ale zgodny z semantyką.
- Siatka ~20 m może dawać lekko „klockowate" granice przy brzegach wyspy; jeśli widoczne, zwiększyć N do 2048² lub doprecyzować krawędzie.
- Wymuszanie na wyspach z górami (gdy góry liczone) może drastycznie zmienić rzeźbę — checkbox gór istnieje właśnie po to.
- Stare światy: wymuszanie dotyczy tylko nowych stref; mogą powstać szwy.

## 6. Rozmiar świata

Decyzja: **własna, samodzielna implementacja** — bez zależności od Expand World Size i bez kopiowania jego kodu.

Zakres początkowy: **6000–10000 m** (zmniejszanie świata; większe światy odkładamy).

Do patchy:

- `WorldGenerator.GetBaseHeight` — stałe `10000/10500/10490` (transpiler),
- `WorldGenerator.GetBiomeHeight` — `10500`,
- `WorldGenerator.GetBiome` — progi dystansów biomów (`2000`, `3000`, `6000`, `8000`, `10000`, `500`, `600`) skalowane przez `WorldRadius / 10000`,
- `WorldGenerator.IsDeepnorth`, `CreateAshlandsGap`, `CreateDeepNorthGap`, `GetAshlandsHeight` — pozycje biegunów i `10150`,
- `static ashlandsMinDistance` / `ashlandsYOffset` — proporcjonalnie (`1.2 * radius`, `-0.4 * radius`),
- `Ship.ApplyEdgeForce`, `Player.EdgeOfWorldKill` — edge kill/force,
- `EnvMan.UpdateWind`, `WaterVolume.SetupMaterial`, `WaterVolume.GetWaterSurface`, `EnvMan.Awake` — woda/wiatr/`_WaterEdge`,
- minimapa — skala/rozmiar tekstury,
- `AltBiomeWorldData` — na start bez resize; sprawdzić mapowanie biomów dla mniejszego świata.

Uwagi:

- postfix gęstości lądu i transpiler rozmiaru świata muszą działać spójnie (modyfikator liczy `d` i częstotliwość maski względem `WorldRadius`),
- strefa ochronna centrum (`800–1600 m`) pozostaje w metrach (nie skaluje się) — start ma zawsze sensowny rozmiar,
- ryzyka: index out of bounds, brak biomów przy małym świecie, minimapa/save system.

## 7. UI

### 7.1. Panel przy tworzeniu świata

- Miejsce: `FejdStartup.m_createWorldPanel`.
- Odczyt seeda: `FejdStartup.m_newWorldSeed` (`GuiInputField`).
- Odczyt nazwy: `FejdStartup.m_newWorldName`.
- Technologia: IMGUI (`OnGUI`) w stylu Valheima; wzorzec: Better Continents `UI.cs`.
- Zawartość:
  - **v1:** suwak **Number of islands**: Few / Medium / Vanilla (domyślnie **Vanilla**),
  - Vanilla = brak modyfikacji generatora; `AffectsGeneration` jest wtedy `false` i żaden patch generacji nie działa,
  - suwak **Gęstość lądu** 5–40% (domyślnie ~15%) — późniejsza wersja,
  - suwak **Rozmiar świata** 6000–10000 m (domyślnie 10000) — późniejsza wersja,
  - dropdown **Limit biomów na wyspie**: Bez limitu / Max 1 / Max 2 / Max 3 (domyślnie Bez limitu) — późniejsza wersja,
  - dropdown **Limit biomów na wyspie startowej**: Bez limitu / Max 1 / Max 2 / Max 3 (domyślnie Bez limitu) — późniejsza wersja,
  - checkbox **„Góry nie liczą się do limitu"** (domyślnie zaznaczony) — późniejsza wersja,
  - pole seeda / przycisk „losuj",
  - przycisk **Podgląd** -> wygeneruj teksturę mapy,
  - przycisk **Zastosuj** -> zapisz ustawienia dla tej nazwy/seedu.
- Podgląd:
  - wariant A (rekomendowany): replikacja modyfikatora gęstości + analiza wysp na siatce 256²/512²; pokazuje % lądu, liczbę wysp, legendę biomów, wyspę startową i ostrzeżenia o limitach/progresji,
  - wariant B: tymczasowy `World` + `WorldGenerator.Initialize` (droższy, ryzyko side-effectów) — później, jeśli potrzeba.
- Ostrzeżenia w panelu:
  - „Limit biomów usunie ostatnie wystąpienie X — zostanie złagodzony",
  - „Brak lądu w pobliżu centrum" (nie powinno się zdarzyć przy `fade`),
  - przy starym świecie: „Zmiany dotyczą tylko nowych stref".

### 7.2. Panel w grze (opcjonalnie)

- F7 / przycisk w menu: podgląd ustawień („Open Waters: 15% lądu, limit 1 biomu, góry poza limitem").
- Bez zmiany ustawień w trakcie gry (albo ostrzeżenie, że wpływa tylko na nowe strefy).

## 8. Ustawienia per świat

- Plik sidecar obok metadanych świata:
  - `World.GetMetaPath()` -> np. `.../worlds_local/Nazwa.fwl` -> `Nazwa.fwl.openwaters.json`.
- Zawartość:
  - wersja moda,
  - `LandCoverage`,
  - `WorldRadius`,
  - `BiomeLimit`,
  - `StartBiomeLimit`,
  - `ExcludeMountains`,
  - `Patchiness`, `PeakPreservation` (zaawansowane, zapisywane mimo braku w UI MVP),
  - hash seeda / nazwa świata (walidacja).
- Odczyt:
  - w prefixie `WorldGenerator.Initialize(World)` / przy ładowaniu świata,
  - jeśli brak pliku -> utwórz z domyślnych configów.
- Zapis:
  - po utworzeniu świata (z panelu) i przy zmianie ustawień,
  - nie nadpisywać bez potrzeby.
- Stare światy: tylko nowe strefy; wyświetlić ostrzeżenie.

## 9. Multiplayer

- Mod obowiązkowy na serwerze i u klientów.
- Synchronizacja ustawień:
  - przekierowanie RPC `PeerInfo` (wzorzec Better Continents),
  - serwer wysyła `version + LandCoverage + WorldRadius + BiomeLimit + StartBiomeLimit + ExcludeMountains + Patchiness + PeakPreservation + seedHash`,
  - klient aplikuje ustawienia **przed** zakończeniem `PeerInfo`, żeby generator był gotowy,
  - ACK od klienta, potem oryginalne `PeerInfo`,
  - przy niezgodności wersji: czytelny błąd i disconnect.
- Determinizm: modyfikator szumu jest funkcją seeda + ustawień; wymuszanie biomów jest funkcją tego samego seeda i ustawień + deterministycznego wyboru (kolejność enuma). Brak losowości runtime.
- Cache maski/analizy nie może różnić się między serwerem a klientem (ten sam klucz).
- Nie używamy EWS, więc nie ma ryzyka kolizji z jego synchronizacją.

## 10. Wydajność

- Modyfikator gęstości: kilka operacji na wywołanie `GetBaseHeight` (Perlin 1 warstwa + lerp) — pomijalne.
- Kalibracja: 512² surowych próbek (~262k wywołań `GetBaseHeight`) + binary search; jednorazowo per świat, cache na dysku.
- Analiza wysp: 1024² próbek z `GetBaseHeight` + `GetBiome` + `GetBiomeHeight`; podobny rząd jak poprzednia maska (sekundy). Optymalizacje:
  - pomijać `GetBiomeHeight` dla `base <= 0.02`,
  - ewentualnie N=768,
  - cache wyniku (`ForcedBiome` + statystyki) na dysku.
- `ForcedBiome`: 1 MB dla 1024²; odczyt O(1) w postfiksie.
- Podgląd UI: N=256/512.
- Wymuszanie biomów nie dodaje pracy runtime poza jednym lookupem.

## 11. Testy

- Świeży świat dla każdej zmiany algorytmu.
- Backup save'ów przed testami.
- Weryfikacja terenu:
  - procent lądu bliski suwakowi (licznik z kalibracji + ocena wizualna),
  - liczba wysp przed/po,
  - naturalne linie brzegowe (brak „ścian" i kwadratowych artefaktów),
  - góry nadal występują przy niskim % lądu (PeakPreservation),
  - spawn gracza i StartTemple na lądzie,
  - rzeki nie tworzą artefaktów,
  - stare strefy nie są modyfikowane.
- Weryfikacja limitów biomów:
  - limit 1: wyspy mono-biomowe (poza górami/polarami),
  - limit 2: wyspy mają najwyżej 2 biomy,
  - bez limitu: zachowanie jak vanilla (tylko gęstość lądu),
  - wyspa startowa z własnym limitem; zawsze istnieje i ma ląd,
  - checkbox gór: góry nie liczone i nienadpisywane,
  - AshLands/DeepNorth nietknięte,
  - żaden biom nie znika z całego świata,
  - minimapa/roślinność/heightmapa spójne z wymuszonym biomem,
  - brak „klockowatych" granic przy brzegach (lub N=2048).
- Multiplayer: klient widzi ten sam teren i te same biomy co serwer.
- Wydajność: czas kalibracji + analizy < ~3 s (cel), brak spadków FPS.

## 12. Licencja i pochodzenie kodu

- Nasz kod: **MIT**.
- Nie kopiujemy kodu Better Continents (**LGPL-2.1**) ani Expand World Size (**Unlicense**); patrzymy tylko, jak rozwiązują problem, i piszemy własną implementację.
- Nie kopiujemy kodu gry; dekompilacja służy wyłącznie do poznania API i zachowania.
- Zależności runtime: BepInEx (**LGPL-2.1**) i Harmony (**MIT**) — linkowane, nie kopiowane; użytkownik instaluje BepInEx osobno.
- W repo: `LICENSE` (MIT), `THIRD-PARTY-NOTICES.md` (kredyty i zależności), sekcja Credits w README.
- Szczegóły: [`05-licensing.md`](05-licensing.md).
