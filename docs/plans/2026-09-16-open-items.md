# Otwarte punkty — 2026-09-16

Lista obejmuje wyłącznie punkty nadal otwarte albo wymagające osobnej,
pełnej weryfikacji. Szczegóły i kontekst: `2026-09-16-cpc6128-status.md`,
`2026-09-16-cpc-architecture.md`.

## CPC6128

- [ ] Test firmware `CAT` (listing katalogu w BASIC) — realny ROM, brak
      dziś osobnego testu (boot do `Ready` i CP/M są pokryte).

## Architektura — pozostałe elementy Fazy 1

- [ ] Ustalić status i zakres pozostałej Fazy 1. Snapshoty PET, VIC-20,
      TRS-80 i Kaypro oraz podstawowe kontrakty `IMachine` i
      `IMachineStateStore<TSnapshot>` są już zrealizowane; decyzja dotyczy
      wyłącznie dalszych prac poniżej.
- [ ] Wspólne kontrakty urządzeń w Core (`IKeyboardDevice`, `ICassetteDevice`,
      `IDiskController`, `IVideoDevice`) z null-adapterami — **`IAudioDevice`
      celowo pominięty**: dodać dopiero z realnym konsumentem w tym samym
      commicie, nie wcześniej.
- [ ] Systematyczny routing Desktop/CLI przez capability interfaces dla
      wszystkich operacji (dziś częściowe: tape/disk już tak, reszta nie).
- [ ] Rozszerzenie macierzy testów kontraktowych o pełny wzorzec (klawiatura,
      video, audio, media, snapshot) — podstawowe `IMachine` oraz
      `IMachineStateStore<TSnapshot>` są już sprawdzane dla wszystkich sześciu
      maszyn; pozostałe capability nadal wymagają osobnej macierzy.
