<p align="center">
  <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/assets/icons/logo-256.png" alt="Open Waters" width="160" />
</p>

# Open Waters

**Early alpha:** Open Waters is a **Valheim world generation mod** that creates fewer islands, more open ocean, and a more deliberate sailing experience for Valheim 1.0.21. Preview a world seed before creating it, then tune the starting island.

Open Waters keeps vanilla coastlines by lowering terrain instead of deleting land. It works in **single-player** and in **hosted worlds**, with settings synchronized from the host to every client. Dedicated-server support is planned for a future release.

[Read the full User Guide](https://github.com/Mkbewe/valheim-open-waters/blob/main/docs/USER_GUIDE.md) | [Report a problem](https://github.com/Mkbewe/valheim-open-waters/issues)

## What it changes

Open Waters is for players who want fewer islands, more ocean, more sailing, and a more exploration-focused Valheim world.

- **Fewer islands and more open ocean:** choose the `Few` or `Medium` island preset.
- **New-world preview:** compare land and biomes for a seed before creating the world.
- **Starting-island controls:** remove high mountains and limit the biomes on the starting island.
- **Vanilla coastlines:** terrain is lowered instead of deleting land.
- **Per-world settings:** a sidecar stores the settings used when the world was created.

Few targets about 27% of vanilla land; Medium about 50%. These values are relative to the same seed in vanilla, not the whole map.

## Preview examples

| Vanilla - land | Vanilla - biomes |
| --- | --- |
| <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/vanila-generated-lands.png" alt="Vanilla land preview"> | <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/vanila-generated-bioms.png" alt="Vanilla biome preview"> |
| More land and vanilla coastlines. | Vanilla biome distribution. |

| Medium - land | Medium - biomes |
| --- | --- |
| <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/medium-lands.png" alt="Medium land preview"> | <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/medium-bioms.png" alt="Medium biome preview"> |
| About 50% of vanilla land. | Biome colours with the Medium preset. |

## Starting island options

| Few - vanilla biomes | Few - Remove mountains |
| --- | --- |
| <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/few-vanila.png" alt="Few starting island with vanilla biomes"> | <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/few-no-mountains.png" alt="Few starting island without high mountains"> |
| Starting island keeps its normal biomes. | High peaks on the starting island are suppressed. |

| Few - Max biomes: 1 | Few - Max biomes: 1, mountains kept |
| --- | --- |
| <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/few-one-biome.png" alt="Few starting island with one biome"> | <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/few-one-biom-with-mountains.png" alt="Few starting island with one biome and mountains"> |
| Counted biomes become Meadows. | Biome limit applies, but mountain peaks remain. |

## Generated world

| Few world map | Starting island close-up |
| --- | --- |
| <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/few-game-map.png" alt="Few generated world map"> | <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/few-game-map-zoom.png" alt="Few starting island close-up"> |
| Ocean-heavy world with fewer islands. | Close-up of the starting island. |

| Vanilla world | Few world | Medium world |
| --- | --- | --- |
| <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/world-vanila.png" alt="Generated vanilla Valheim world"> | <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/world-few.png" alt="Generated Valheim world with the Few preset"> | <img src="https://raw.githubusercontent.com/Mkbewe/valheim-open-waters/main/docs/images/world-medium.png" alt="Generated Valheim world with the Medium preset"> |
| Vanilla world generation. | Few islands, more open ocean. | Medium islands, about half the vanilla land. |

## Installation

### Thunderstore / r2modman

1. Install **BepInExPack Valheim** if it is not already installed.
2. Install **Open Waters** from Thunderstore or r2modman.
3. Start the game through **Start modded**.

### Manual installation

1. Install BepInExPack Valheim.
2. Copy `OpenWaters.dll` to:
   ```text
   <VALHEIM_DIR>/BepInEx/plugins/OpenWaters/
   ```
3. Set the Open Waters options on the New World screen.

## Configuration

The config file is created at:

```text
BepInEx/config/valheim.openwaters.cfg
```

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

`LandFraction` is relative to vanilla land for the same seed, not a percentage of the world.

## FAQ

### How do I get fewer islands in Valheim?

Install Open Waters, start a new world, and choose the `Few` or `Medium` island preset. The mod changes world generation; it does not remove land from an already generated world.

### Can I preview a Valheim world before creating it?

Yes. The New World panel renders a land or biome preview for the current seed and settings.

### Does Open Waters work with existing worlds?

It is designed around new worlds. Existing worlds are not reshaped, and you should back up any world before testing generation mods.

### Does Open Waters work in multiplayer?

Yes for hosted worlds. The host sends its world settings to every client during the `PeerInfo` handshake, so all players generate the same terrain. Every client needs Open Waters installed; clients without the mod are rejected before they can generate anything, and clients with incompatible settings get a clear message and are disconnected. Dedicated servers are not supported yet.

### Is Open Waters compatible with other world-generation mods?

No compatibility is guaranteed. Avoid combining it with other mods that replace Valheim's world generation.

## Known limitations

- **Hosted worlds need Open Waters on every client.** The host synchronizes settings during the handshake; clients without the mod are rejected with the game's "wrong version" error. Clients with the mod get a specific in-game message when settings are incompatible, and are disconnected.
- **Steam Cloud is not synchronized.** `OpenWaters.worldSettings` stays in local storage and does not travel with a cloud save.
- **Seed-based sidecar lookup is a local-world fallback.** When joining a hosted world the client uses the settings received from the host and ignores its local sidecar. A stable world UID is still planned for local worlds with duplicate seeds.
- **Preview generation can briefly freeze the game.**
- **Boss and location placement changes.** Fewer islands change how many locations are generated, including boss altars. Testing confirmed boss locations can spawn, but 100% reliability cannot be guaranteed.
- **The game can be much harder.** Fewer islands mean fewer resources and longer journeys. With Max biomes: 1, reaching a Black Forest may require building a raft.
- **World size is fixed.** The radius in the sidecar does not resize Valheim's world.
- **The starting island is the only island with biome limits.**

## Links

- [User Guide](https://github.com/Mkbewe/valheim-open-waters/blob/main/docs/USER_GUIDE.md)
- [Source code](https://github.com/Mkbewe/valheim-open-waters)
- [Issues and feedback](https://github.com/Mkbewe/valheim-open-waters/issues)
- [Changelog](https://github.com/Mkbewe/valheim-open-waters/blob/main/CHANGELOG.md)

## AI disclosure

Developed with AI coding assistance (OpenCode) under the author's direction.

## License

MIT. Third-party credits are listed in [THIRD-PARTY-NOTICES.md](https://github.com/Mkbewe/valheim-open-waters/blob/main/THIRD-PARTY-NOTICES.md).
