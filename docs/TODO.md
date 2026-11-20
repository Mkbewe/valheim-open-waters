# Do zrobienia

- [ ] Przetestować paczkę z `release/` na czystym BepInEx/r2modman: Few, Medium, Vanilla, podgląd, brak błędów w logu.
- [ ] Opublikować release na GitHubie i Thunderstore (team `MkbewePP`); ustawić sekret `TCLI_AUTH_TOKEN`.
- [ ] Synchronizacja ustawień serwer <-> klient.
- [ ] Zmiana rozmiaru świata i limit biomów poza wyspą startową.
- [ ] Cache kalibracji na dysku i ostrzeżenia w UI.
- [ ] Opcja "rzeka dzieli wyspę na dwie" (`MergeRiverSeparatedIslands`): `Separate` = tylko komponent spawnu, `Merge` = dokładanie małych komponentów w promieniu 500 m. Wymaga: `OpenWatersSettings` + config BepInEx, pola w sidecarze i bump wersji 1->2 z fallbackiem `false`, UI w sekcji Starting Island, klucza cache podglądu i trybu w logu analizy.
- [ ] Lepszy sposób generowania podglądu bez zamrażania gry. Obecny cache generatora wymaga pełnego `WorldGenerator.Initialize` (~3 s) przy zmianie `StartBiomeLimit`/`ExcludeMountains`, bo ustawienia te wpływają na `Pregenerate` (rzeki/lakes) - muszą być w kluczu cache'a. Do rozważenia: własny sampler terenu w wątku roboczym albo inkrementalna odbudowa danych generatora.
