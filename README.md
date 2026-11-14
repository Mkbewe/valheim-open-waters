# Open Waters

Mod do Valheima zmieniający generator świata: **mniej wysp, więcej otwartego oceanu**, z naturalnymi liniami brzegowymi. Dodatkowo panel preferencji przy tworzeniu świata (gęstość lądu, rozmiar świata, limit biomów na wyspę, podgląd mapy).

## Status

- Etap: **research + design**. Kod jeszcze nie został napisany.
- Data ustaleń: 2026-11-14
- Nazwa: **Open Waters**
- Katalog projektu: `D:\Projekty\valheim-open-waters`
- DLL / assembly / namespace: `OpenWaters`
- Plugin GUID: `pl.kacper.openwaters`
- Docelowy tryb: multiplayer LAN / dedykowany serwer (mod na serwerze i u klientów)
- Kod: samodzielny (bez kopiowania z innych modów); licencja **MIT**

## Dokumentacja

- [`docs/01-research.md`](docs/01-research.md) — środowisko, stack moddingowy, przeanalizowane mody, linki.
- [`docs/02-world-generation.md`](docs/02-world-generation.md) — jak działa generator świata Valheima 1.0.21 (zdekompilowane).
- [`docs/03-design.md`](docs/03-design.md) — ustalona koncepcja (gęstość lądu z szumu, limit biomów per wyspa), UI, synchronizacja.
- [`docs/04-next-steps.md`](docs/04-next-steps.md) — otwarte decyzje, plan wdrożenia, checklisty testów.
- [`docs/05-licensing.md`](docs/05-licensing.md) — licencja MIT, kredyty, zasady korzystania z innych modów.

## Najważniejsze ustalenia

- Nie przepisujemy całego generatora. Zostawiamy vanilla i modyfikujemy `GetBaseHeight` (gęstość lądu) oraz `GetBiome` (limit biomów).
- Gęstość lądu regulujemy szumem: obniżanie terenu / maska kontynentalna sprawia, że wyspy znikają naturalnie pod wodą — bez dyskretnego usuwania wysp.
- Limit biomów per wyspa: bez limitu / max 1 / max 2 / max 3; osobny limit dla wyspy startowej; checkbox wyłączający góry z liczenia.
- Nadmiarowe biomy są nadpisywane (wymuszanie biomu), bez usuwania wysp; biomy polarne i progresja są chronione.
- Użytkownik dostaje suwaki: gęstość lądu 5–40% i rozmiar świata, oraz ustawienia limitów biomów.
- Ustawienia mają być per świat i synchronizowane z serwera na klientów.
- Panel preferencji ma być widoczny przy tworzeniu świata, z podglądem mapy i ostrzeżeniami.
- Kod piszemy samodzielnie; EWS i Better Continents służą tylko jako referencja (bez kopiowania).
