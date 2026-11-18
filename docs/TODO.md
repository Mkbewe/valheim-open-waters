# Do zrobienia

- [ ] Jedno źródło wersji: `package.json`, wersja generatorowana do `Plugin.cs` przez MSBuild.
- [ ] `CHANGELOG.md`.
- [ ] Ikona 256×256 PNG — wybrać z `assets/icons/` i wyeksportować.
- [ ] `scripts/release.mjs`: bump wersji, build Release, złożenie paczki, zip, tag, GitHub Release, `--dry-run`.
- [ ] `thunderstore.toml` dla `tcli build` / `tcli publish`.
- [ ] Weryfikacja wersji zależności `BepInEx-BepInExPack` i wpisanie jej do `manifest.json`.
- [ ] Workflow release w CI: build → zip → GitHub Release → opcjonalnie Thunderstore.
- [ ] Rozwiązanie problemu DLL-i Valheim w CI (nie wolno ich commitować do repo).
- [ ] Test paczki na czystym BepInEx: Few, Medium, Vanilla, podgląd, brak błędów w logu.
- [ ] Publikacja: GitHub Release, `Mkbewe-OpenWaters` na Thunderstore, opcjonalnie Nexus.
- [ ] Synchronizacja ustawień serwer ↔ klient.
- [ ] Zmiana rozmiaru świata i limit biomów poza wyspą startową.
- [ ] Cache kalibracji na dysku i ostrzeżenia w UI.
