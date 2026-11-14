# 01 — Research: środowisko i modding Valheima

Data: 2026-11-13

## 1. Środowisko lokalne

| Element | Wartość |
|---|---|
| Gra | Valheim **1.0.21** (BepInEx log), Steam buildid `26249040` |
| Ścieżka gry | `D:\Lunchery\Steam\steamapps\common\Valheim` |
| Unity | `6000.0.75.2503836` |
| BepInEx | **5.4.23.6** z paczki **BepInExPack Valheim 5.4.2360** |
| Log BepInEx | `D:\Lunchery\Steam\steamapps\common\Valheim\BepInEx\LogOutput.log` |
| .NET SDK | **8.0.428**, instalacja per-user: `C:\Users\Kacper Bobrowski\.dotnet\dotnet.exe` |
| Runtime .NET | 8.0.x obecny systemowo |
| Backup starego BepInEx | `C:\Users\KACPER~1\AppData\Local\Temp\opencode\valheim-research\valheim-bepinex-backup-20261113-195137` |

BepInEx został zainstalowany i zweryfikowany — log pokazuje poprawne ładowanie i `Valheim Version: 1.0.21`. Obecnie w `BepInEx/plugins` nie ma żadnych pluginów.

## 2. Stack moddingowy

- **BepInEx 5** — loader pluginów. Plugin to klasa `BaseUnityPlugin` z atrybutem `[BepInPlugin(GUID, Name, Version)]`.
- **HarmonyX** — patchowanie metod w runtime (`Prefix`, `Postfix`, `Transpiler`, `Finalizer`).
- Język: **C#**, target najczęściej `netstandard2.1` (np. Expand World Size) lub `net48` (Better Continents).
- Referencje do kompilacji:
  - `BepInEx\core\BepInEx.dll`
  - `BepInEx\core\0Harmony.dll`
  - `valheim_Data\Managed\assembly_valheim.dll`
  - `valheim_Data\Managed\UnityEngine.dll`, `UnityEngine.CoreModule.dll`, `UnityEngine.IMGUIModule.dll`, `UnityEngine.UI.dll`, `Unity.TextMeshPro.dll`, `assembly_utils.dll`
- Prywatne pola/metody: `AccessTools` / refleksja. Publicizer nie jest konieczny.
- Build: `dotnet build` (SDK 8) i kopiowanie DLL do `BepInEx\plugins\OpenWaters\`.
- Zmiany terenu **muszą** być zainstalowane na serwerze i u wszystkich klientów. Inaczej teren się nie zgadza.

## 3. Przeanalizowane mody

### Better Continents

- Autor: billw2012; aktywny fork: **JereKuusela/BetterContinents**.
- Licencja: **LGPL-2.1** — kod tylko do wglądu; nie kopiujemy do naszego projektu.
- Nexus 446, Thunderstore `JereKuusela-BetterContinents`.
- Co robi:
  - podmienia `WorldGenerator.GetBaseHeight` i `GetBiomeHeight` prefixami (własna wersja V1/V2/V3),
  - własny system warstw noise + heightmap/biome map/forest map/spawn map z plików PNG,
  - `GlobalScale`, `SeaLevelAdjustment`, `WorldSize`, `EdgeSize`, regulacja rzek i lasów,
  - ustawienia **per świat** w pliku `.BetterContinents` obok save'a (`World.GetMetaPath()`),
  - własne IMGUI (przez `gui_framework`), debug UI, eksport mapy PNG,
  - synchronizacja ustawień w multiplayer przez przekierowanie RPC `PeerInfo` (`ZRpc.Register` + handshake).
- Kluczowe pliki:
  - `BetterContinents.WorldGeneratorPatch.cs` — nadpisanie `GetBaseHeight`/`GetBiomeHeight`/`GetBiome`,
  - `BetterContinents.HeightmapPatch.cs` — własny build heightmapy,
  - `BetterContinents.WorldSizePatch.cs` — transpilery rozmiaru świata,
  - `Patcher.cs` — dynamiczne zakładanie/zdejmowanie patchy,
  - `BetterContinents.ZNetPatch.cs` — synchronizacja ustawień,
  - `BetterContinents.SaveCollectionPatch.cs` — rozpoznawanie pliku `.BetterContinents` przez save system.
- Wniosek: to najbliższy wzorzec dla nas, ale nie chcemy kopiować całego systemu heightmap. Interesują nas:
  - per-world sidecar,
  - dynamiczne patche,
  - IMGUI,
  - synchronizacja ustawień.

### Expand World Size

- Autor: **JereKuusela**.
- GitHub: `JereKuusela/valheim-expand_world_size`, licencja **Unlicense**.
- Co robi:
  - `World radius`, `World edge size`, `Stretch world`, `Stretch biomes`,
  - `Altitude multiplier/delta`, `Water depth multiplier`, `Forest multiplier`,
  - skaluje progi biomów i pozycje Ashlands/Deep North przy zmianie rozmiaru,
  - patchuje stałe `10000/10500/10490/10150` transpilerami,
  - rozszerza tablice biomów (`AltBiomeWorldData`, 2048²), minimapę, save system, zone grid,
  - obsługuje mniejsze i większe światy; wersja 1.41 naprawiała index out of bounds dla mniejszych światów,
  - własna asynchroniczna generacja minimapy.
- Kluczowe pliki:
  - `ExpandWorldSize/features/WorldSize.cs` — transpilery stałych rozmiaru, resize biomów,
  - `ExpandWorldSize/features/Stretch.cs` — skalowanie progów biomów i biegunów,
  - `ExpandWorldSize/features/Poles.cs` — Ashlands/Deep North,
  - `ExpandWorldSize/config/Patcher.cs` — dynamiczne patchowanie,
  - `ExpandWorldSize/config/Configuration.cs` — publiczne `WorldRadius`, `WorldTotalRadius`, `WorldStretch`, itd.,
  - `ExpandWorldSize/Generate.cs` — minimapa.
- Wniosek: EWS pokazuje, które miejsca są wrażliwe na rozmiar. **Decyzja: własna implementacja bez zależności i bez kopiowania kodu** (Unlicense pozwalałby na kopiowanie, ale chcemy samodzielny kod). Zakres: 6000–10000 m.

## 4. Inne przydatne źródła

- Valheim Modding Wiki: <https://github.com/Valheim-Modding/Wiki/wiki>
  - Creating Your First Mod,
  - Best Practices,
  - Referencing Assemblies.
- HarmonyX / Harmony docs: <https://harmony.pardeike.net/>
- BepInEx docs: <https://docs.bepinex.dev/>
- Thunderstore BepInExPack: `denikson/BepInExPack_Valheim` (obecnie 5.4.2360)
- WorldCache (pomysł na cache generacji): <https://github.com/Chistix/ValheimWorldCache> (link z Thunderstore; sprawdzić przed użyciem)
- Better Continents:
  - <https://github.com/JereKuusela/BetterContinents>
  - <https://billw2012.github.io/BetterContinents-Docs/>
- Expand World Size:
  - <https://github.com/JereKuusela/valheim-expand_world_size>
  - <https://thunderstore.io/c/valheim/p/JereKuusela/Expand_World_Size/>

## 5. Jak ponownie zdekompilować grę

ILSpy CLI (użyta wersja 11.2.0.10050) została pobrana do temp:

```powershell
& "C:\Users\KACPER~1\AppData\Local\Temp\opencode\valheim-research\ilspy\pkg-win\tools\any\win-x64\ilspycmd.exe" `
  "D:\Lunchery\Steam\steamapps\common\Valheim\valheim_Data\Managed\assembly_valheim.dll" `
  -t WorldGenerator -o "C:\Users\KACPER~1\AppData\Local\Temp\opencode\valheim-research\decompiled"
```

Jeśli temp zostanie wyczyszczony, można pobrać ILSpyCmd ponownie z NuGet (`ilspycmd.win-x64`) albo użyć ILSpy/dnSpy.

## 6. Stan repo

- Kod moda: **brak**.
- Projekt/csproj: **brak** (na razie tylko dokumentacja).
- BepInEx i .NET SDK: zainstalowane i gotowe do dalszej pracy.
