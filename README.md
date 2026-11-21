<p align="center">
  <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/assets/icons/logo-256.png" alt="Open Waters" width="160" />
</p>

# Open Waters

Valheim 1.0.21 mod for BepInEx that reduces the number of islands and increases open ocean. It keeps vanilla coastlines by lowering terrain instead of removing land.

> **Early alpha:** single-player is the main supported target. Multiplayer and dedicated servers are unsupported, and settings or world compatibility may change between builds.

[![Release downloads](https://img.shields.io/github/downloads/Mkbewe/valheim-open-waters/total?style=flat-square&label=release%20downloads)](https://github.com/Mkbewe/valheim-open-waters/releases)

**Start here:** [User Guide](https://github.com/Mkbewe/valheim-open-waters/blob/main/docs/USER_GUIDE.md) - installation, panel controls, preview, configuration, and experimental manual multiplayer notes. Multiplayer is not officially supported.

## Facts

- Version: `0.1.1`
- Plugin GUID: `valheim.openwaters`
- Assembly / namespace: `OpenWaters`
- Target: `netstandard2.1`
- Game: Valheim 1.0.21, BepInEx 5.4.x
- License: MIT
- Distribution: early alpha; published on Thunderstore

## Features

### Generation

- Island presets: `Few`, `Medium`, `Vanilla` (default).
- Few and Medium re-implement the vanilla noise stack with configurable frequency, detail damping, peak protection and a binary-searched height offset. Defaults: Few `0.27`, Medium `0.50` of vanilla land.
- Few compresses high terrain with `MountainCompressionStart` and `MountainCompressionFactor`.
- `Vanilla` changes nothing unless `Remove mountains` or `Max biomes` is set.
- Dragonqueen altar minimum altitude is 40 m when Few or Medium is active.

### Starting island

- `IslandAnalysis` samples a 1024x1024 grid using `GetHeight > 30`, builds connected components and records area, centre, bounds and biome histograms.
- The starting island is the component at world centre.
- `Remove mountains` (default off): suppresses heights `0.34`-`0.39` on the starting island; also active with Vanilla.
- `Max biomes` (default `Vanilla`): `1` sets the counted starting-island biomes to Meadows; `2` keeps native Meadows/BlackForest, keeps a 250 m spawn safe radius and maps other counted biomes to Meadows/BlackForest. Mountains do not count toward the limit.

### New World UI and preview

- Enable toggle, island preset slider, starting-island section (`Remove mountains`, `Max biomes`), `Preview` toggle.
- `Generate preview` renders a 400x400 map from 512x512 samples, with `Land` / `Biomes` views, biome colours, separate swamp-water colour and spawn marker.
- Preview work is split across frames (16 rows per step). Land and biome textures are cached in memory (up to 8 entries).
- New-world settings are captured on creation and saved as `OpenWaters.worldSettings` in the **local** `worlds_local\<world>\` folder, next to that world's local files. The sidecar is an immutable snapshot of the preset and the effective tuning values; existing worlds ignore later config changes. Without a matching sidecar the preset is Vanilla; a malformed or incompatible sidecar blocks the world load with an error. When joining a hosted world, the sidecar is found by seed because the client has no world name.

## Constraints

- World size is fixed; `WorldRadius` is used only for analysis and preview sampling.
- `BiomeLimit` is stored in the sidecar but not applied.
- Settings are local; there is no server-to-client synchronization. Steam Cloud saves are not synchronized: the sidecar is written to local storage and does not travel with the cloud save.
- Calibration has no disk cache; only preview textures are cached.

## Install

1. Install BepInExPack Valheim.
2. Copy `OpenWaters.dll` to `<VALHEIM_DIR>/BepInEx/plugins/OpenWaters/`.
3. Set the Open Waters options on the New World screen.

## Build

The SDK is under `~/.dotnet`; the game directory is set in `Directory.Build.props` (override with `-p:ValheimDir=...`). A successful build copies the DLL to `BepInEx/plugins/OpenWaters/`:

```powershell
& "$env:USERPROFILE\.dotnet\dotnet.exe" build OpenWaters.sln -c Debug
```

Use `-p:SkipPluginCopy=true` when the game is running and the DLL is locked.

To build a release package locally:

```powershell
npm run build
```

The Thunderstore package uses `docs/README.md` as its page. The repository `README.md` is for GitHub.

The script validates the version in `manifest.json`, `package.json`, `package-lock.json` and `Plugin.cs`, checks the 256x256 icon, runs the core tests, builds `Release`, and writes `release/MkbewePP-OpenWaters-<version>.zip`. The package contains the DLL, manifest, icon, README, changelog and license files. It deliberately excludes `valheim.openwaters.cfg` and `OpenWaters.worldSettings`.

## Releases

Release changelog, versions and the release ZIP are built locally. Preview the next release:

```powershell
npm run release:dry
```

Run the release and confirm the proposed version:

```powershell
npm run release
# npm run release:patch
# npm run release:minor
# npm run release:major
```

The script requires a clean `main`, creates `release/v<version>`, updates `package.json`, `package-lock.json`, `manifest.json`, `CHANGELOG.md`, `Plugin.cs`, README and user guide, builds `release/MkbewePP-OpenWaters-<version>.zip`, and creates one release commit. It does not tag or push.

Push the release branch and merge the PR. After merge, run the `Publish Release` workflow manually from GitHub Actions; type `release` in the `confirm` input to continue. It creates the Git tag and GitHub Release, attaches the ZIP and publishes the same ZIP to Thunderstore. CI does not build the mod. Thunderstore publishing requires the `TCLI_AUTH_TOKEN` repository secret.

If there is no previous `v*` tag, the first changelog includes the full history; create a baseline `v0.1.0` tag first. Do not edit `CHANGELOG.md` manually for new releases.

## Configuration

`BepInEx/config/valheim.openwaters.cfg`:

| Section | Key | Default |
| --- | --- | --- |
| General | `Enabled` | `true` |
| Few | `LandFraction` | `0.27` |
| Few | `FrequencyScale` | `0.60` |
| Few | `PeakProtection` | `0.20` |
| Few | `DetailDamping` | `0.35` |
| Few | `MountainCompressionStart` / `MountainCompressionFactor` | `0.34` / `0.55` |
| Medium | `LandFraction` | `0.50` |
| Medium | `FrequencyScale` | `0.70` |
| Medium | `PeakProtection` | `0.60` |
| Medium | `DetailDamping` | `0.35` |

`LandFraction` is relative to vanilla land, not a percentage of the world.

## Development

Commit messages follow Conventional Commits (lowercase type/scope/subject, max 72 characters); Husky `commit-msg` enforces it. Run `npm install` once after cloning. Pull request titles are checked by `.github/workflows/pr-title-check.yml`.

Remaining work: [`docs/TODO.md`](https://github.com/Mkbewe/valheim-open-waters/blob/main/docs/TODO.md).

## License

MIT - see [LICENSE](https://github.com/Mkbewe/valheim-open-waters/blob/main/LICENSE). Third-party credits and runtime dependencies are listed in [THIRD-PARTY-NOTICES.md](https://github.com/Mkbewe/valheim-open-waters/blob/main/THIRD-PARTY-NOTICES.md).
