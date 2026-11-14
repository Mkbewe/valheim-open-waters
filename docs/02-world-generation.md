# 02 — Generator świata Valheima 1.0.21

Na podstawie dekompilacji `assembly_valheim.dll` (ILSpy 11.2.0.10050).

Aktualizacja: 2026-11-14 — dodano ustalenia pod modyfikator gęstości lądu, wymuszanie biomów, StartTemple i cache biomów.

## 1. Przepływ inicjalizacji

```text
WorldGenerator.Initialize(World world)          // statyczne
  -> private WorldGenerator(World world)
       m_version = world.m_worldGenVersion
       VersionSetup(m_version)
       Random.InitState(world.m_seed)
       m_noiseGen = new FastNoise(world.m_seed)  // Cellular
       m_offset0..m_offset4 = Random.Range(-10000, 10000)
       m_riverSeed, m_streamSeed = Random.Range(...)
       if (!world.m_menu) Pregenerate()
  -> m_instance = generator
```

`Pregenerate()` (prywatne):

```text
FindLakes()
m_rivers  = PlaceRivers()
m_streams = PlaceStreams(isDN: false)
PlaceStreams(isDN: true)
```

Ważne: `Pregenerate()` jest wywoływane **w konstruktorze**, przed zewnętrznym postfixem `Initialize`. Modyfikator gęstości musi być aktywny od początku konstruktora (postfix `GetBaseHeight`), a analiza wysp musi być gotowa **przed** `Pregenerate` (prefix `Pregenerate`), bo rzeki/strumienie korzystają z już zmienionego terenu.

## 2. Podstawowe metody

### `GetBaseHeight(float wx, float wy, bool menuTerrain)`

- Prywatna metoda.
- Wynik w przybliżeniu `0..1` (nie metry).
- Bazowy szum: kilka warstw `DUtils.PerlinNoise` z offsetami `m_offset0`, `m_offset1`.
- Na końcu odejmowane jest `0.07`.
- Woda: `30 m` odpowiada bazowej wysokości około `0.15` (bo `GetHeightMultiplier() == 200`).
- Ocean/land w `GetBiome` jest klasyfikowany progiem `baseHeight <= oceanLevel`, domyślnie `oceanLevel = 0.02` (czyli ~4 m w skali bazowej).
- Edge world:
  - `distance > 10000` -> lerp do `-0.2`,
  - `distance > 10490` -> lerp do `-2`.
- Wbudowana „banda" archipelagowa (linie 904–909 dekompilacji): różnica dwóch niskoczęstotliwościowych Perlinów tworzy pasma; `SmoothStep(744, 1000, distance)` wyłącza ją blisko centrum, żeby zagwarantować wyspę startową. Nasz modyfikator gęstości powinien analogicznie wygasać w pobliżu centrum.
- Przy `distance < m_minMountainDistance` (domyślnie 1000 m) szczyty są ściskane z zakresu `0.28–0.38` (vanilla-owa ochrona okolic startu).
- `menuTerrain` obsługuje tło menu; **nie wolno** aplikować na nie modyfikatorów Open Waters.

**Wniosek dla Open Waters:** globalne obniżenie terenu („podniesienie poziomu wody") można zrobić postfiksem `GetBaseHeight`: `result -= bias * maska(x, y)`. Wyspy znikają naturalnie, gdy `base` spada poniżej `0.02`, bez dyskretnego usuwania komponentów. Szczegóły: [`03-design.md`](03-design.md) §4.

### `GetHeightMultiplier()`

```text
return 200f;
```

Finalna wysokość w metrach = wynik funkcji biomu \* 200.

### `GetBiome(float wx, float wy, float oceanLevel = 0.02f, bool waterAlwaysOcean = false)`

Kolejność sprawdzeń (uproszczona):

1. `m_world.m_menu` -> Meadow/Mountain dla tła.
2. `IsAshlands(wx, wy)` -> **AshLands** (sprawdzane PRZED oceanem!).
3. `baseHeight <= oceanLevel` -> **Ocean**.
4. `IsDeepnorth(wx, wy)` -> **DeepNorth**.
5. `baseHeight > 0.4` -> **Mountain**.
6. Swamp: Perlin > 0.6, dystans 2000..maxMarshDistance, `0.05 < baseHeight < 0.25`.
7. Mistlands: Perlin > minDarklandNoise, dystans ~6000..10000.
8. Plains: Perlin > 0.4, dystans ~3000..8000.
9. BlackForest: Perlin > 0.4, dystans ~600..6000.
10. Dystans > 5000 -> BlackForest.
11. W przeciwnym razie -> Meadows.

Kluczowy wniosek: wymuszenie `baseHeight <= 0.02` usuwa zwykłe wyspy (Ocean), ale **nie usuwa Ashlands/Deep North**, bo te sprawdzenia są wcześniej. Regiony biegunowe trzeba traktować osobno (najlepiej zawsze je zachowywać).

**Wymuszanie biomu (Open Waters):** publiczne `GetBiome(float, float, ...)` jest używane przez `Heightmap.GetBiome`, `WorldGenerator.GetHeight` i `AltBiomeWorldData`, więc jego postfix wystarcza, żeby nadpisać biom na wyspie. Trzeba tylko:

- pomijać `Ocean` (nie wymuszamy biomu na wodzie),
- pomijać `AshLands`/`DeepNorth` (ochrona biegunów),
- pomijać `Mountain`, gdy checkbox gór jest zaznaczony,
- pamiętać o prywatnym `GetBiome(Vector2s)` (cache `s_cachedBiomes`) i `GetBiomeArea` — cache jest czyszczony w konstruktorze generatora, więc między światami nie przecieka; analizę wysp robimy przez publiczne `GetBiome(float, float)`, żeby nie zatruwać cache'u wartościami bez wymuszania.

### `GetBiomeHeight(Biome biome, float wx, float wy, out Color mask, bool preGeneration = false, bool riverPreDN = true)`

- Zwraca finalną wysokość w **metrach**.
- `num2 = GetHeightMultiplier() * CreateAshlandsGap(...) * CreateDeepNorthGap(...)`.
- Dla `distance > 10500` zwraca `-2f * GetHeightMultiplier()`.
- Switch po biomie:
  - Swamp -> `GetMarshHeight`
  - DeepNorth -> `GetDeepNorthHeight` / `GetDeepNorthHeightPregenerate`
  - Mountain -> `GetSnowMountainHeight`
  - BlackForest -> `GetForestHeight`
  - Ocean -> `GetOceanHeight`
  - AshLands -> `GetAshlandsHeight` / `GetAshlandsHeightPregenerate`
  - Plains -> `GetPlainsHeight`
  - Meadows -> `GetMeadowsHeight`
  - Mistlands -> `GetMistlandsHeight`
- `GetHeight(wx, wy)` = `GetBiome` + `GetBiomeHeight`.

### `GetBiomeSector` i `AltBiomeWorldData`

- `World.m_biomeData` (`AltBiomeWorldData`) to mapa 2048×2048 punktów biomów/sektorów.
- `GetBiomeSector(wx, wy)` mapuje świat na siatkę 2048².
- `GenerateBiomes()` w `WorldGenerator` jest w tej wersji praktycznie puste (`m_biomes = new List<Biome>()` z ogromnym `num` — dekompilacja pokazuje martwy kod); główna mapa biomów jest w `AltBiomeWorldData`.
- `AltBiomeWorldData` buduje punkty przez `WorldGenerator.instance.GetBiome(...)` + `GetBiomeHeight(biome, ...)` — dzięki temu postfix `GetBiome` wpływa też na minimapę i mapę biomów, a wysokości nadpisanych komórek są spójne z wymuszonym biomem.
- Przy zmianie rozmiaru świata trzeba rozszerzyć tablice `PointHeights`, `PointBiomes`, `PointSectors` i przeliczyć mapowanie świata (robi to Expand World Size).

### Spawn i wyspa startowa

- `Game.m_StartLocation = "StartTemple"` — początkowa pozycja gracza to ikona lokacji `StartTemple` z `ZoneSystem` (`Game.FindSpawnPoint`), a nie samo centrum świata. Brak tej lokacji -> fallback `Vector3.zero`.
- StartTemple jest umieszczany przez system lokacji po wygenerowaniu terenu, więc **musi istnieć ląd w pobliżu centrum**. Vanilla chroni ten obszar (banda archipelagowa wyłączona < 744 m, góry ściskane < 1000 m).
- Open Waters musi zachować podobną strefę ochronną przy modyfikatorze gęstości; wyspę startową wykrywamy jako komponent najbliższy centrum (lub zawierający centrum).

## 3. Stałe i twarde wartości

W `WorldGenerator`:

```csharp
private const float m_waterTreshold = 0.05f; // używane gdzie indziej, nie mylić z oceanLevel 0.02
public const float worldSize     = 10000f;
public const float waterEdge     = 10500f;
public const float waterEdgeSqr  = 110250000f;
private const float maxDarklandDistance = 10000f;
private const float oceanBiomeScale = 0.0005f;
private const float oceanBiomeMinNoise = 0.4f;
private const float oceanBiomeMaxNoise = 0.6f;
private const float oceanBiomeMinDistance = 1000f;
private const float oceanBiomeMinDistanceBuffer = 256f;
private float m_minMountainDistance = 1000f;
private const float mountainBaseHeightMin = 0.4f;
private const float deepNorthMinDistance = 12000f;
private const float deepNorthYOffset = 4000f;
public static readonly float ashlandsMinDistance = 12000f;
public static readonly float ashlandsYOffset = -4000f;
```

Twarde wartości w metodach:

- `GetBaseHeight`: `10000f`, `10500f`, `10490f`, progi `0.28`, `0.38`, `-0.2`, `-2`.
- `GetBiomeHeight`: `10500f`.
- `GetBiome`: progi dystansów `2000`, `6000`, `3000`, `8000`, `600`, `10000`, `5000`.
- `IsDeepnorth`: `4000`, `12000`.
- `CreateAshlandsGap` / `CreateDeepNorthGap`: `400` itd.
- `GetAshlandsHeight`: ograniczenia szerokości `7500` i długości `1000`.

## 4. Jak powstają wyspy

W vanilla **nie ma parametru liczby wysp**. Wyspy to po prostu connected-components obszarów, gdzie finalna wysokość terenu jest powyżej poziomu wody (30 m). Kształt wynika z Perlin noise i progów biomów.

Możliwe podejścia:

1. Próbkować wysokości i wyznaczyć wyspy (connected components), a następnie usuwać całe komponenty (odrzucone — zbyt dyskretne, wymaga maski oceanu i ochrony wielu przypadków).
2. **Wybrane (Open Waters):** modyfikować `GetBaseHeight` (obniżać teren / maska kontynentalna), przez co wyspy znikają naturalnie, gdy schodzą pod poziom wody. Zachowujemy pipeline vanilla i naturalne brzegi, ale nie 1:1 kształt wysp.
3. Connected components nadal używamy — ale tylko do **analizy biomów per wyspa** (limit biomów), nie do usuwania lądu.

## 5. Miejsca patchowania

### `GetBaseHeight` — postfix (gęstość lądu)

Miejsce na modyfikator gęstości:

```text
if (!menuTerrain && !isSamplingRaw)
    result -= bias * maskaKontynentalna(wx, wy) * fadeCentrum(distance)
```

Zalety:

- działa na cały pipeline (biomy, heightmapy, rzeki, minimapa, spawn),
- nie trzeba reimplementować vanilla noise,
- brak binarnej maski i artefaktów krawędzi.

Uwagi:

- `GetBaseHeight` jest prywatne -> Harmony może patchować.
- Trzeba pominąć `menuTerrain == true`.
- Podczas surowej kalibracji trzeba pominąć modyfikator (flaga `IsSamplingRaw`), bo kalibracja sama liczy modyfikator na zapisanych próbkach.
- Ustawienia/seed ustawić w prefixie `Initialize(World)` — konstruktor i `Pregenerate` wykonują się przed przypisaniem `m_instance`.

### `GetBiome` — postfix (limit biomów)

Do wymuszania biomu per wyspa (patrz [`03-design.md`](03-design.md) §5):

```text
if (!menuTerrain && !isAnalyzingIslands && __result != Ocean
    && __result != AshLands && __result != DeepNorth
    && !(excludeMountains && __result == Mountain)
    && ForcedBiome[cell] != 0)
    __result = ForcedBiome[cell] - 1;
```

Uwagi:

- `GetBiome` jest publiczne; używają go `Heightmap`, `GetHeight` i `AltBiomeWorldData`, więc wymuszenie propaguje się do terenu i mapy.
- `GetBiomeHeight` nie wymaga patcha: dostaje biom z `GetBiome`.
- Analiza wysp musi widzieć biomy **bez** wymuszania (flaga `IsAnalyzingIslands`), ale **z** modyfikatorem gęstości.

### `Pregenerate` — prefix

Miejsce na analizę wysp **przed** rzekami/strumieniami i przed generacją heightmap. Konstruktor wywołuje `Pregenerate` zaraz po ustawieniu offsetów i seedów, więc seed i offsety są już gotowe.

### Rozmiar świata

Decyzja: **własna implementacja, bez zależności od Expand World Size**. Do transpilacji/patchy:

- `WorldGenerator.GetBaseHeight` — `10000/10500/10490`
- `WorldGenerator.GetBiomeHeight` — `10500`
- `WorldGenerator.GetAshlandsHeight` — `10150` i ograniczenia biegunów
- `WorldGenerator.GetBiome` — progi dystansów biomów (`2000`, `3000`, `6000`, `8000`, `10000`, `500`, `600`)
- `WorldGenerator.IsDeepnorth`, `CreateAshlandsGap`, `CreateDeepNorthGap`, `ashlandsMinDistance`, `ashlandsYOffset`
- `Ship.ApplyEdgeForce`, `Player.EdgeOfWorldKill`
- `EnvMan.UpdateWind`, `WaterVolume.SetupMaterial`, `WaterVolume.GetWaterSurface`, `EnvMan.Awake`
- minimapa (skala/rozmiar tekstury)
- ewentualnie `AltBiomeWorldData` (mapowanie 2048²) — na start bez resize, sprawdzić mniejszy świat.

Zakres początkowy: **6000–10000 m**. Większe światy wymagają rozszerzenia save systemu i map biomów — poza zakresem MVP.

## 6. Pułapki

1. **Menu terrain** — nie aplikować modyfikatorów na tło menu (`world.m_menu` / `menuTerrain`).
2. **Próba surowa vs analiza** — rozdzielić flagi: `IsSamplingRaw` (kalibracja: bez gęstości i bez wymuszania), `IsAnalyzingIslands` (analiza: gęstość tak, wymuszanie nie). Inaczej rekurencja lub zatrucie danych.
3. **Kolejność** — modyfikator gęstości musi działać od początku konstruktora (przed `Pregenerate`, rzekami i heightmapą); analiza wysp w prefixie `Pregenerate`.
4. **Cache biomów** — prywatne `GetBiome(Vector2s)` używa `s_cachedBiomes`; cache jest czyszczony w konstruktorze. Analizę rób publicznym `GetBiome(float, float)`, żeby nie zapisać biomów bez wymuszania.
5. **Ashlands/Deep North** — sprawdzane przed oceanem; nie nadpisywać ich postfiksem `GetBiome`.
6. **Wyspa startowa** — przy niskim % lądu trzeba zagwarantować ląd w pobliżu centrum (fade modyfikatora); sprawdzić StartTemple/spawn.
7. **Biomy kluczowe** — wymuszanie biomów może wyeliminować biom z całego świata; dodać globalną kontrolę i cofanie wymuszania na najmniejszej wyspie z danym biomem.
8. **Stare światy** — modyfikator i wymuszanie wpływają tylko na nieodkryte strefy; szew na granicy starych/nowych stref; zalecany świeży świat.
9. **Multiplayer** — seed i ustawienia muszą być identyczne; inaczej klient wygeneruje inny teren.
10. **Wydajność** — kalibracja 512² i analiza 1024² mogą trwać sekundy; cache na dysku (seed + config + wersja moda + wersja gry).
11. **Góry** — modyfikator gęstości obniża też szczyty; bez łagodnej ochrony szczytów przy niskim % lądu znikną góry (problem dla srebra/obsydianu).
