# Do zrobienia

- [ ] Przetestować paczkę z `release/` na czystym BepInEx/r2modman: Few, Medium, Vanilla, podgląd, brak błędów w logu.
- [ ] Opublikować release na GitHubie i Thunderstore (team `MkbewePP`); ustawić sekret `TCLI_AUTH_TOKEN`.
- [ ] Synchronizacja ustawień serwer <-> klient.
- [ ] Wygenerować stabilny UID świata, zapisywać go w sidecarze i używać przy dołączaniu do hosta zamiast wyszukiwania po seedzie.
- [ ] Zmiana rozmiaru świata i limit biomów poza wyspą startową.
- [ ] Opcja "rozbij wyspę startową" (checkbox w sekcji Starting Island): jeśli wyspa jest wystarczająco duża, podzielić ją na 2-3 mniejsze wyspy, postawić gracza na jednej z mniejszych, a sąsiednie wyspy pozostawić relatywnie blisko, żeby po zasoby trzeba było przepłynąć. Uwzględnić seed/spawn, minimalny obszar wysp, bezpieczny promień startu, sidecar, klucz cache podglądu i bump `generatorVersion`.
- [ ] Cache kalibracji na dysku i ostrzeżenia w UI.
- [ ] Opcja "rzeka dzieli wyspę na dwie" (`MergeRiverSeparatedIslands`): `Separate` = tylko komponent spawnu, `Merge` = dokładanie małych komponentów w promieniu 500 m. Wymaga: `OpenWatersSettings` + config BepInEx, pola w sidecarze i bump wersji 1->2 z fallbackiem `false`, UI w sekcji Starting Island, klucza cache podglądu i trybu w logu analizy.
- [ ] Lepszy sposób generowania podglądu bez zamrażania gry. Obecny cache generatora wymaga pełnego `WorldGenerator.Initialize` (~3 s) przy zmianie `StartBiomeLimit`/`ExcludeMountains`, bo ustawienia te wpływają na `Pregenerate` (rzeki/lakes) - muszą być w kluczu cache'a. Do rozważenia: własny sampler terenu w wątku roboczym albo inkrementalna odbudowa danych generatora.
- [ ] Zgodność wyspy startowej z faktycznym spawinem na seedach, gdzie środek mapy jest oceanem. Obecnie anchor to `(0,0)`, więc wybieramy najbliższy ląd, a Valheim stawia StartTemple według własnych warunków. Rozważane podejścia: (a) dwie fazy — tymczasowa maska w `Pregenerate`, potem przeliczenie po `ZoneSystem.GenerateLocations` z prawdziwej pozycji `GetLocationIcon("StartTemple")`, albo (b) nadpisanie pozycji StartTemple: postfix na `ZoneSystem.GenerateLocations`, przenieść wpis w `m_locationInstances` pod nowy klucz `ZoneSystem.GetZone(point)` i ustawić `m_position` na punkcie wybranej wyspy (uwaga na kolizje z innymi lokacjami i determinizm u klientów).
