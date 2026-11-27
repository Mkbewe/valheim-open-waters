# Changelog

## 0.2.1

- World settings are now matched by a world UID instead of the seed, so worlds with the same seed keep separate settings. New worlds save it automatically.
- **Old worlds:** load the world once to get its UID from `BepInEx/LogOutput.log`, then add `uid=<id>` to `OpenWaters.worldSettings`. Without the UID, the world uses vanilla terrain. You can also create a new world. [Step-by-step guide](https://github.com/Mkbewe/valheim-open-waters/blob/main/docs/USER_GUIDE.md#updating-a-settings-file-from-an-older-alpha).
- Steam Cloud worlds now keep `OpenWaters.worldSettings` in the cloud save folder, so the custom terrain travels with the world to another PC. Previously the settings file stayed on the original PC.
- Local worlds keep their settings in `worlds_local`. If Steam Cloud is unavailable or full, the settings fall back to local storage with a warning.
- New `LocalSidecarFallback` option lets a client use a manually copied settings file when the host's settings are unavailable. Leave it off for normal play.
- Hosted worlds work as before: when custom settings are used, every player needs the mod and clients receive the host's settings automatically.
- Updated the Thunderstore page and User Guide with hosted-world setup and settings-file upgrade steps.

## 0.2.0 (2026-11-23)

- Hosted worlds now share the host's generation settings with every client, so everyone explores the same islands.
- Clients without Open Waters can no longer join a world with custom settings; previously they generated different terrain.
- Joining a vanilla host now always uses vanilla terrain, even if you have custom settings saved locally.
- Clients with incompatible settings see a clear in-game message and are disconnected instead of silently generating a different world.

## 0.1.2 (2026-11-21)

- Updated the Thunderstore page with preview screenshots and example worlds.
- Documented that world settings are stored locally and are not synced through Steam Cloud.
- Documented how settings are matched by seed when joining a hosted world.

## 0.1.1 (2026-11-21)

- World settings are now frozen when the world is created, so changing the config later no longer changes existing worlds.
- World settings are stored in the local worlds folder.
- Loading a world with damaged or incompatible settings now shows an error instead of silently generating different terrain.
- Joining a hosted world now finds the matching settings by seed when the world name is not available.
- Mountains no longer count toward the starting-island biome limit.

## 0.1.0

- First alpha release.
- Few and Medium island presets with configurable terrain tuning.
- Starting-island options: remove mountains and limit biomes.
- New World preview with Land and Biomes views.
- Per-world `OpenWaters.settings` file.
- Manual per-world settings for unsupported multiplayer experiments.
