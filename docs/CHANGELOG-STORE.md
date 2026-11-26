# Changelog

## 0.2.1

- Settings now follow a world ID instead of the seed. New worlds save it automatically.
- **Old worlds:** add `uid=<id from the BepInEx log>` to `OpenWaters.worldSettings`, or create a new world. Otherwise they generate vanilla terrain.
- Hosting works as before: everyone needs the mod and gets the host's settings.
- New `LocalSidecarFallback` option allows a copied settings file. Leave it off normally.

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
