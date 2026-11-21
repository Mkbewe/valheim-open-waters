# Changelog

## [0.1.2](https://github.com/Mkbewe/valheim-open-waters/compare/v0.1.1...v0.1.2) (2026-11-21)

### Documentation

* add dedicated Thunderstore README ([#31](https://github.com/Mkbewe/valheim-open-waters/issues/31)) ([12c8b8c](https://github.com/Mkbewe/valheim-open-waters/commit/12c8b8c2217eebef7a745d3f57fd699b2ffa56ab))

  - add a user-facing store page with preview and generated-world screenshots
  - package docs/README.md as the Thunderstore README
  - compress existing documentation images and add two new world examples
  - disclose AI-assisted development in the README and assembly metadata
* improve Thunderstore page discoverability ([#32](https://github.com/Mkbewe/valheim-open-waters/issues/32)) ([fbb0320](https://github.com/Mkbewe/valheim-open-waters/commit/fbb0320f3074b055d38f6e0ab4ea06a54980faa2))

  - add SEO-friendly world generation wording to the store page
  - add FAQ entries for common Valheim searches
  - update the short package description
  - mark multiplayer as planned for the next release

## [0.1.1](https://github.com/Mkbewe/valheim-open-waters/compare/v0.1.0...v0.1.1) (2026-11-21)

### Features

* **settings:** freeze generation settings in the world sidecar ([#27](https://github.com/Mkbewe/valheim-open-waters/issues/27)) ([d0768fc](https://github.com/Mkbewe/valheim-open-waters/commit/d0768fcc89ef5e28b183dfaf21df2198c0f233bb))

  - store an immutable per-world snapshot: effective config values, mod and
    generator versions, and a Base64-encoded seed
  - route all generation through GenerationSession instead of global settings
  - block world loads with malformed or incompatible sidecars
  - cancel world creation when the sidecar cannot be written
  - add codec tests for escaping, ranges and compatibility

### Bug Fixes

* **settings:** find sidecar by seed when joining a world ([#28](https://github.com/Mkbewe/valheim-open-waters/issues/28)) ([2d293cb](https://github.com/Mkbewe/valheim-open-waters/commit/2d293cb09ea9ec3a5ee21977675e099ddc2779af))

  - fall back to matching local sidecars by seed when the client has no world name
  - add tests for sidecar lookup by seed
  - fix the BepInEx pack dependency namespace
  - document the temporary seed lookup and planned world UID
  - add split starting island to the TODO list
* **settings:** store world sidecar in the local worlds folder ([#26](https://github.com/Mkbewe/valheim-open-waters/issues/26)) ([4630d6f](https://github.com/Mkbewe/valheim-open-waters/commit/4630d6f9a03cf1c9283fe86ba5134520ba3e7085))

  - rename the sidecar to OpenWaters.worldSettings
  - always resolve the local worlds_local directory
  - document that Steam Cloud does not synchronize the sidecar
  - exclude mountains from the Max biomes count
  - clarify preview freezing and refresh TODO
  - remove the stale release ZIP

### Documentation

* prepare early alpha release copy ([#29](https://github.com/Mkbewe/valheim-open-waters/issues/29)) ([007fb71](https://github.com/Mkbewe/valheim-open-waters/commit/007fb714ac291d298950926d4f9e0170be39294b))

  - mark the project as early alpha in README and manifest
  - document local ZIP installation through r2modman
  - clarify seed-based sidecar lookup when joining a hosted world
  - update distribution status for the Thunderstore release

## 0.1.0

- Initial alpha release.
- Few and Medium island presets with configurable terrain tuning.
- Starting-island options: Remove mountains and Max biomes.
- New World preview with Land and Biomes views.
- Per-world `OpenWaters.worldSettings` sidecar.
- Manual per-world settings for unsupported multiplayer experiments.
