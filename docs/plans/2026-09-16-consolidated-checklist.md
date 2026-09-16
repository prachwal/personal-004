# Skonsolidowana checklista — plany z 2026-09-16

Źródła: `2026-09-16-cpc6128-integration-plan.md`, `2026-09-16-cpc6128-remaining-work-plan.md`,
`2026-09-16-machine-composition-and-reuse-plan.md`, `2026-09-16-cpc-composition-cleanup-plan.md`.

`[x]` = zaimplementowane **i** przeze mnie zweryfikowane dziś (build/test/grep/diff).
`[ ]` = do zrobienia.

## 1. CPC6128 — rdzeń i integracja

- [x] Projekt `PetEmulator.Cpc6128`, `Cpc6128Machine : IMachine`, RAM 128 KB, bankowanie 0-7
- [x] `PetEmulator.CpcFdc` (I8272Chip, DskDiskImage) — osobny projekt, nie miesza z FD1791/FD1793
- [x] ROM fixture: `roms/cpc6128/cpc6128.rom` + `amsdos.rom`, SHA-256 w planie
- [x] Test bootu realnego ROM-u do `Ready` + `PRINT 1+1` (`Cpc6128BootTests.cs`)
- [x] `cpc6128-debug` CLI session + test (`Cpc6128DebuggerSession.cs`, `Cpc6128DebuggerSessionTests.cs`)
- [x] Desktop: ViewModel, View, menu, `IDatasetteViewModel`+`IDiskDriveViewModel`
- [x] Test CP/M 2.2 boot z realnego DSK (`Cpc6128Cpm22BootTests.cs`, 1/1 w izolacji, 11s) — pokrywa `|CPM`/AMSDOS DSK read z M9
- [ ] Firmware `CAT` (BASIC) — brak osobnego testu
- [ ] `docs/plans/2026-09-16-cpc6128-integration-plan.md` status M9 nie zaktualizowany po dodaniu testu CP/M (wciąż pisze "niepotwierdzone")

## 2. CPC6128 — snapshot/debug (M8)

- [x] `I8272Snapshot`/`CaptureState`/`RestoreState` na FDC (reużyte, nie przepisane)
- [x] `CaptureState`/`RestoreState` na `Cpc6128MemoryBus`, `CpcGateArray`, `MT6545`, `Ay38910`, `CpcKeyboard`, `CpcCassette`, `Cpc6128Ports`
- [x] `Cpc6128Snapshot`/`Cpc6128SnapshotCodec` (System.Text.Json, walidacja wersji)
- [x] Test round-trip na **nowej** instancji maszyny (nie tej samej — mocniejszy test)
- [x] Test snapshot z zamontowanym dyskiem (dane sektora przeżywają restore)

## 3. Dokumentacja CPC6128

- [x] `docs/chips/I8272.md` + wpis w `docs/chips/README.md`
- [x] `docs/desktop/multi-machine.md` — CPC6128 capability opisane

## 4. Test hygiene

- [x] `[Category("BinaryBoot")]` sformalizowane jako opt-in, `AGENTS.md` opisuje filtr
- [x] Pełny `dotnet test PetEmulator.slnx --filter "TestCategory!=BinaryBoot"` — zielony (potwierdzone dziś, wszystkie projekty)
- [x] `SieveProgram_SurvivesTypeSaveResetLoad` — status skorygowany w planie (przechodzi deterministycznie ~2-3 min, nie mylić z `tools/Cpc464SieveBenchmark`)

## 5. Architektura CPC — Faza 0 (plan kompozycji)

- [x] Ujednolicony właściciel pętli tick: `CpcMachineClock` używany przez CPC464 i CPC6128
- [x] Ujednolicona kolejność próbkowania IRQ (`_clock.Tick()` → `SetInt` w obu maszynach) + test regresyjny `InterruptLineSamplesGateArrayAfterDeviceTick`
- [x] `IMachineSnapshot`/`IMachineStateStore<TSnapshot>` w `PetEmulator.Core`, wpięte jako opakowanie nad istniejącym `CaptureState`/`RestoreState`
- [x] `CpcMachineBase<TSnapshot>` — decyzja jawnie zapisana: **odrzucone**, CPC464/CPC6128 zostają niezależnymi kompozycjami (FDC/frame-count różne)
- [x] `Cpc464Machine` dostał symetryczny `CaptureState`/`RestoreState` (parytet z CPC6128)

## 6. Architektura — Faza 1 (nie miała startować bez osobnej decyzji — część i tak wystartowała)

- [x] `MachineClock` wyciągnięty do `PetEmulator.Core.Timing` jako generyczny (ekstrakcja z CPC, nie projekt z góry), `CpcMachineClock` = cienki wrapper
- [x] **Wycofano:** `IAudioDevice`/`NullAudioDevice`/`AudioDevice` (`78091dc`) — zero konsumentów w repo, dotknęło PET/VIC-20/TRS-80/Kaypro bez decyzji o Fazie 1
- [x] **Wycofano:** `IKeyboardViewModel` (`ceed646`) — jeden konsument (`IMachineViewModel`), ciągnął zależność od `PetEmulator.Pet.Keyboard` przez wszystkie maszyny
- [ ] Pełne snapshoty PET, VIC-20, TRS-80, Kaypro
- [ ] Wspólne kontrakty urządzeń w Core (`IKeyboardDevice`, `ICassetteDevice`, `IDiskController`, `IVideoDevice`) z null-adapterami
- [ ] Formalna decyzja go/no-go dla reszty Fazy 1 — nadal nie zapisana mimo że część kodu już wystartowała

## 7. Sprzątanie po Fazie 1 (plan cleanup)

- [x] **Zadanie 1** — usunięto martwe pole `CycleCount` z `MachineSnapshot` (commit `5ea5067`) — zweryfikowane dziś: build 0/0, `Cpc464.Tests` 11/11, `Cpc6128.Tests` 16/16
- [x] **Zadanie 2** — usunięto `IAudioDevice`/`NullAudioDevice`/`AudioDevice` z `PetEmulator.Audio` i wszystkich 6 ViewModeli Desktop (commit `78091dc`) — Desktop.Tests 61/61
- [x] **Zadanie 3** — złożono `IKeyboardViewModel` z powrotem do `IMachineViewModel`, usunięto plik (commit `ceed646`) — Desktop.Tests 61/61
- [ ] Po 2+3: pełny `dotnet test PetEmulator.slnx --filter "TestCategory!=BinaryBoot"` zielony
- [ ] Dopisać wynik sprzątania do `2026-09-16-machine-composition-and-reuse-plan.md`

## 8. Otwarte porządkowe (niskie ryzyko, nikt jeszcze nie podjął)

- [ ] `docs/plans/2026-09-16-cpc6128-integration-plan.md` — zaktualizować status M9 po teście CP/M (punkt 1 wyżej)
