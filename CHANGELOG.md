# Changelog

## [0.2.1](https://github.com/Mkbewe/valheim-open-waters/compare/v0.2.0...v0.2.1) (2026-11-27)

### Features

* **42:** match local world settings by UID ([#62](https://github.com/Mkbewe/valheim-open-waters/issues/62)) ([f37a3f8](https://github.com/Mkbewe/valheim-open-waters/commit/f37a3f8917bd0dc6931f90f79eb72fd7b47847f3))

  - store the vanilla world UID in each new world's settings file
  - match local settings by UID so worlds with the same seed keep separate presets
  - ignore settings files from older alpha builds without a UID; those worlds start vanilla
  - add opt-in LocalSidecarFallback for copied settings files when the host sends none
  - print the world UID in the log so an old settings file can be updated by hand
  - update the Thunderstore description and guides for UID matching and the fallback switch
* **60:** sync world settings through Steam Cloud ([#66](https://github.com/Mkbewe/valheim-open-waters/issues/66)) ([dd259d0](https://github.com/Mkbewe/valheim-open-waters/commit/dd259d0bdb70cc955a1f79a27bd6dd4365e38335))

  - keep the sidecar beside the world in cloud or local save folders
  - write cloud sidecars through FileHelpers with a local fallback on failure
  - find sidecars by world UID in the world's source, then the other source
  - migrate a sidecar to the world's current source when it moves
  - add core tests for UID lookup, copying and migration
  - update guides and the store changelog for cloud and local paths

### Bug Fixes

* **63:** reserve a valid test version block for zero patches ([#64](https://github.com/Mkbewe/valheim-open-waters/issues/64)) ([40cb013](https://github.com/Mkbewe/valheim-open-waters/commit/40cb013a1bcf537d2a5e14cf7ad46af0098a2229))

  - start zero-patch test builds at 0.2.101 instead of 0.2.1
  - keep 0.2.1 through 0.2.100 free for real releases
  - reset the test counter when the base version changes
  - reject builds that would leave the reserved version block
  - document the zero-patch versioning rule

### Documentation

* **56:** backfill and enforce the store changelog ([#65](https://github.com/Mkbewe/valheim-open-waters/issues/65)) ([c7b1d9c](https://github.com/Mkbewe/valheim-open-waters/commit/c7b1d9c9a8f762ae356dbea3529e742d437cecb5))

  - add 0.2.0, 0.1.2, 0.1.1 and 0.1.0 entries for Thunderstore
  - refuse a release without a matching docs/CHANGELOG-STORE.md section
  - include the store changelog in the release commit
  - document the manual step in README
* **59:** refactor README and player guides ([#67](https://github.com/Mkbewe/valheim-open-waters/issues/67)) ([cda3f7d](https://github.com/Mkbewe/valheim-open-waters/commit/cda3f7d6ec6c0d799dfecd98348270f741353cfa))

  - describe the mod as more open ocean with fewer islands
  - make installation and hosted-world setup easier to follow
  - document how to upgrade settings files from older alpha builds
  - clarify Steam Cloud storage and world UID matching
  - list known generation limitations consistently across the docs

## [0.2.0](https://github.com/Mkbewe/valheim-open-waters/compare/v0.1.2...v0.2.0) (2026-11-23)

### Features

* **50:** synchronize world settings between host and clients ([#53](https://github.com/Mkbewe/valheim-open-waters/issues/53)) ([3872dc0](https://github.com/Mkbewe/valheim-open-waters/commit/3872dc0e40fde1f7f995d63b069b3b3d7d051bd7))

  - clients use the host's settings during the handshake instead of local sidecars
  - clients without the mod are rejected and disconnected before generating terrain
  - incompatible settings show a specific message and disconnect the client
  - a disabled mod stays vanilla instead of advertising support to modded hosts
  - add unit tests and a startup self-check for the new peer-info logic
  - document hosted-world requirements, test results and the diagnostics switch

### Documentation

* document world generation limits and follow-up work ([#35](https://github.com/Mkbewe/valheim-open-waters/issues/35)) ([a91a7c7](https://github.com/Mkbewe/valheim-open-waters/commit/a91a7c76804f08c7006b75f1afe0b0602357cebe))
* update generated world gallery ([#34](https://github.com/Mkbewe/valheim-open-waters/issues/34)) ([ea5cc9b](https://github.com/Mkbewe/valheim-open-waters/commit/ea5cc9bf9631356399d786557152f16066d1ff53))

  - replace world1/world2 with per-preset screenshots
  - add Vanilla, Few and Medium comparison images
  - refresh the existing generated-world screenshots

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
