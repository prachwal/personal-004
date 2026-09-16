# Otwarte punkty — 2026-09-16

Wszystko poza tą listą z dzisiejszych planów jest zrobione i zweryfikowane
(build + testy). Szczegóły i kontekst: `2026-09-16-cpc6128-status.md`,
`2026-09-16-cpc-architecture.md`.

## CPC6128

- [ ] Test firmware `CAT` (listing katalogu w BASIC) — realny ROM, brak
      dziś osobnego testu (boot do `Ready` i CP/M są pokryte).

## Architektura — Faza 1 (PET/VIC-20/TRS-80/Kaypro)

- [ ] Formalna decyzja go/no-go: czy w ogóle robić Fazę 1. Nie zakładać
      "tak" domyślnie — dwa punkty poniżej już raz wystartowały bez tej
      decyzji i zostały cofnięte.
- [ ] Pełne snapshoty PET, VIC-20, TRS-80, Kaypro (wzór: `CpcMachineSnapshot`).
- [ ] Wspólne kontrakty urządzeń w Core (`IKeyboardDevice`, `ICassetteDevice`,
      `IDiskController`, `IVideoDevice`) z null-adapterami — **`IAudioDevice`
      celowo pominięty**: dodać dopiero z realnym konsumentem w tym samym
      commicie, nie wcześniej.
- [ ] Systematyczny routing Desktop/CLI przez capability interfaces dla
      wszystkich operacji (dziś częściowe: tape/disk już tak, reszta nie).
- [ ] Rozszerzenie macierzy testów kontraktowych o pełny wzorzec (klawiatura,
      video, audio, media, snapshot) — dziś tylko `IMachine` podstawowy.
