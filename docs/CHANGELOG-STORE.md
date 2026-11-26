# Changelog

## 0.2.1

- Settings now follow a world ID instead of the seed. New worlds save it automatically.
- **Old worlds:** add `uid=<id from the BepInEx log>` to `OpenWaters.worldSettings`, or create a new world. Otherwise they generate vanilla terrain.
- Hosting works as before: everyone needs the mod and gets the host's settings.
- New `LocalSidecarFallback` option allows a copied settings file. Leave it off normally.
