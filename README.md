# Open Waters

A Valheim mod that changes world generation: **fewer islands, more open ocean**, with natural coastlines. It also adds a preferences panel on the world creation screen (land density, world size, per-island biome limit, map preview).

## Status

- Stage: **early development**. The project skeleton (BepInEx plugin, hello world) is in place; world-generation code is not implemented yet.
- Decisions date: 2026-11-14
- Name: **Open Waters**
- Project directory: `D:\Projekty\valheim-open-waters`
- DLL / assembly / namespace: `OpenWaters`
- Plugin GUID: `valheim.openwaters`
- Target mode: multiplayer LAN / dedicated server (mod on the server and all clients)
- Code: original (no copying from other mods); license **MIT**

## Documentation

> The documents are currently written in Polish.

- [`docs/01-research.md`](docs/01-research.md) — environment, modding stack, analyzed mods, links.
- [`docs/02-world-generation.md`](docs/02-world-generation.md) — how the Valheim 1.0.21 world generator works (decompiled).
- [`docs/03-design.md`](docs/03-design.md) — agreed design (noise-based land density, per-island biome limit), UI, synchronization.
- [`docs/04-next-steps.md`](docs/04-next-steps.md) — open decisions, implementation plan, test checklists.
- [`docs/05-licensing.md`](docs/05-licensing.md) — MIT license, credits, rules for using other mods.

## Key decisions

- We do not rewrite the whole generator. We keep vanilla and modify `GetBaseHeight` (land density) and `GetBiome` (biome limit).
- Land density is controlled by noise: lowering terrain / a continental mask makes islands disappear naturally underwater — no discrete island removal.
- Per-island biome limit: unlimited / max 1 / max 2 / max 3; a separate limit for the starting island; a checkbox excluding mountains from the count (later version).
- First version changes **only the number of islands**; the default **Vanilla** setting leaves world generation completely untouched.
- Excess biomes are overwritten (biome forcing) without removing islands; polar biomes and progression are protected (later version).
- The player gets sliders for land density 5–40% and world size, plus biome-limit settings.
- Settings are per world and synchronized from the server to clients.
- The preferences panel is shown on the world creation screen, with a map preview and warnings.
- Code is written from scratch; EWS and Better Continents are used only as references (no copying).

## Development

Build (the output DLL is copied to `BepInEx\plugins\OpenWaters\`):

```powershell
dotnet build OpenWaters.sln -c Debug
```

Commit messages follow [Conventional Commits](https://www.conventionalcommits.org/) (lowercase type/scope/subject, header max 72 characters) and are checked by a Husky `commit-msg` hook. Run `npm install` once after cloning to enable the hook.

## License

Open Waters is licensed under the MIT License. See [LICENSE](LICENSE) for the full text.

Third-party credits and runtime dependencies are listed in [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
