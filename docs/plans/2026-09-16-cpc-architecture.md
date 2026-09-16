# Architektura współdzielona CPC (i plan reszty maszyn)

Zastępuje `2026-09-16-machine-composition-and-reuse-plan.md` i
`2026-09-16-cpc-composition-cleanup-plan.md` (skonsolidowane; cleanup
wykonany w całości). Otwarte punkty patrz `2026-09-16-open-items.md`.

## Cel

Ujednolicić strukturę PET, VIC-20, CPC464, CPC6128, TRS-80 i Kaypro tak, aby:

- wspólne zachowania były implementowane raz,
- maszyny nadal zachowywały własne mapy pamięci, porty i urządzenia,
- snapshot, reset, zegar i testy miały spójny wzorzec,
- UI, CLI i headless debug korzystały z tych samych kontraktów.

Nie tworzyć jednej uniwersalnej klasy zawierającej wszystkie możliwe
urządzenia. Trzy poziomy współdzielenia: kontrakty globalne (`Core`) →
implementacje wspólne dla rodziny sprzętu → kompozycja konkretnej maszyny.
Dziedziczenie tylko dla stabilnego, wspólnego stanu/cyklu życia; urządzenia
podłączać przez kompozycję.

## Faza 0 — dowód na CPC: ZAMKNIĘTA

1. **Ujednolicony właściciel pętli tick.** `CpcMachineClock`
   (`src/PetEmulator.Cpc/CpcMachineClock.cs`) — cienki wrapper nad
   generycznym `MachineClock` (`lib/PetEmulator.Core/Timing/MachineClock.cs`,
   wyciągnięty jako ekstrakcja, nie projekt z góry). Używany przez
   `Cpc464Machine` i `Cpc6128Machine` — wcześniej `Cpc464Bus.Tick` i
   `Cpc6128Machine.StepInstruction` miały każdy własną, niezależną pętlę.
2. **Ujednolicona kolejność próbkowania IRQ.** Obie maszyny: `_clock.Tick()`
   → `InterruptLines.SetInt(GateArray.InterruptPending)`. Regresja pokryta
   testem `Cpc6128MachineTests.InterruptLineSamplesGateArrayAfterDeviceTick`
   (200k instrukcji, asercja zgodności na każdym kroku + potwierdzenie że
   przerwanie faktycznie wystąpiło).
3. **`IMachineSnapshot`/`IMachineStateStore<TSnapshot>`**
   (`lib/PetEmulator.Core/Abstractions/`) dodane jako cienkie kontrakty nad
   już istniejącymi, przetestowanymi `CaptureState()`/`RestoreState()` obu
   maszyn CPC — nie przepisane od zera.
4. **`CpcMachineBase<TSnapshot>` — decyzja: odrzucone.** Po ujednoliceniu
   zegara i IRQ nadal zostają różnice należące do konkretnej maszyny:
   CPC6128 ma FDC i licznik ramek (`_frameCycles`/`FrameCount`), CPC464 nie
   ma żadnego z nich. `Cpc464Machine` i `Cpc6128Machine` zostają
   niezależnymi kompozycjami, współdzielą tylko urządzenia
   (`CpcGateArray`/`CpcKeyboard`/`CpcCassette`/`CpcMachineClock`) i
   snapshot bazowy (`CpcMachineSnapshot : MachineSnapshot`).
5. **Parytet CPC464.** `Cpc464Machine` dostał symetryczny
   `CaptureState`/`RestoreState` (wcześniej tylko CPC6128 go miał).

Weryfikacja Fazy 0 (zrobiona dziś): pełny build 0/0,
`PetEmulator.Cpc464.Tests` i `PetEmulator.Cpc6128.Tests` zielone
(z `--filter "TestCategory!=BinaryBoot"` dla domyślnego przebiegu).
`SieveProgram_SurvivesTypeSaveResetLoad` (CPC464) jest wolny (~2-3 min,
kategoria `BinaryBoot`), ale przechodzi deterministycznie — nie mylić z
osobnym, faktycznie nieukończonym `tools/Cpc464SieveBenchmark`.

## Sprzątanie po nieautoryzowanym starcie Fazy 1 — ZAMKNIĘTE

Faza 1 miała wymagać osobnej decyzji go/no-go przed startem (patrz niżej).
Mimo to część kodu wystartowała bez takiej decyzji zapisanej. Kontrola
jakości znalazła trzy problemy, wszystkie naprawione:

1. **Martwe pole `CycleCount` na `MachineSnapshot`.** Zapisywane w
   `CaptureState()` obu maszyn CPC, nigdy nie czytane w `RestoreState()`
   (faktyczne cykle wracają przez `Cpu.CaptureSnapshot()`/
   `CpuProcessorBase.RestoreSnapshot`). Usunięte.
2. **`IAudioDevice`/`NullAudioDevice`/`AudioDevice`
   (`lib/PetEmulator.Audio/AudioContracts.cs`) — nieużywana abstrakcja.**
   Dodana do wszystkich 6 ViewModeli Desktop (w tym PET/VIC-20/TRS-80/
   Kaypro — czyli Faza 1 bez decyzji), zero konsumentów w repo poza
   deklaracją i testem sprawdzającym tylko istnienie property. Usunięta
   w całości.
3. **`IKeyboardViewModel`** — wydzielony z `IMachineViewModel`, ciągnął
   `HostKeyEventKind` z `PetEmulator.Pet.Keyboard` przez wszystkie maszyny
   (w tym te niezwiązane z PET), dokładnie jeden konsument
   (`IMachineViewModel`), zero niezależnego pattern-matchingu. Złożony z
   powrotem do `IMachineViewModel`.

Wszystkie trzy zweryfikowane: build 0/0, `Cpc464.Tests` 11/11,
`Cpc6128.Tests` 16/16, `Desktop.Tests` 61/61.

## Faza 1 — reszta rodzin: NIE ROZPOCZĘTA FORMALNIE

Faza 1 nie startuje automatycznie po Fazie 0 — wymaga osobnej decyzji, że
koszt migracji pozostałych czterech rodzin (PET, VIC-20, TRS-80, Kaypro)
jest tego wart, a nie że "skoro zrobiliśmy CPC, robimy resztę". Ta decyzja
**nadal nie jest zapisana**. Dwa punkty poniżej (`IAudioDevice`,
`IKeyboardViewModel`) zostały wykonane bez niej i cofnięte — patrz wyżej.

Pozostałe punkty Fazy 1, w kolejności z oryginalnego planu:

1. `MachineClock` w Core — **zrobione już w Fazie 0** (punkt 1 wyżej),
   jako ekstrakcja z CPC, nie osobny krok Fazy 1.
2. Wspólny kontrakt audio + `NullAudioDevice` dla wszystkich maszyn —
   **cofnięte** (patrz sprzątanie wyżej); wymaga realnego konsumenta przed
   ponownym dodaniem.
3. Wspólny kontrakt klawiatury i kasety poza CPC (CPC już ma
   `CpcKeyboard`/`CpcCassette`) — **cofnięte** w wersji bez konsumenta
   (`IKeyboardViewModel`); nie podjęte dla kasety.
4. Pełne snapshoty PET, VIC-20, TRS-80, Kaypro według schematu z Fazy 0.
5. Routing Desktop/CLI z testów typu konkretnego ViewModelu na capability
   interfaces — częściowo już prawda dziś (`ITapeViewModel`/
   `IDatasetteViewModel`/`IDiskDriveViewModel` istnieją od wcześniejszego
   refaktoru), ale nie systematycznie dla wszystkich operacji.
6. Macierz testów kontraktowych na wszystkich realnych maszynach —
   częściowo istnieje (`MachineContractTests.cs` obejmuje `IMachine` dla
   wszystkich pięciu realnych maszyn), nie rozszerzona o pełny wzorzec
   testów z sekcji niżej.

## Docelowe projekty (aspiracyjne, nie wszystko zrobione)

### `PetEmulator.Core` — kontrakty niezależne od rodziny

- `IMachine`, `IMemoryBus`, `IProcessor` — **istnieją**.
- `IMachineSnapshot`, `IMachineStateStore<TSnapshot>` — **istnieją**
  (Faza 0, punkt 3).
- `IKeyboardDevice`, `ICassetteDevice`, `IDiskController`, `IVideoDevice`,
  `IAudioDevice`, `INull*` — **nie istnieją** (Faza 1, nie rozpoczęte;
  `IAudioDevice` był i został cofnięty, patrz wyżej).

Kontrakty nie mogą zakładać konkretnego CPU, mapy pamięci ani formatu
dysku. Każdy nowy kontrakt w Core, który retrofituje istniejącą,
przetestowaną klasę (tak jak `IMachineStateStore` zrobił dla
`Cpc464Machine`/`Cpc6128Machine`), wlicza koszt dotknięcia tej klasy do
zadania — to nie jest "dodanie kontraktu obok" bez wpływu na istniejący kod.

### `PetEmulator.Hardware`/`PetEmulator.Machines` (nie istnieje)

`MachineBase<TSnapshot>`, `MachineDeviceSet`, `PortDispatcher`,
`RomOverlay`/`MemoryWindow`/`BankedMemory` — wszystko aspiracyjne, poza
zakresem Fazy 0. `MachineBase<TSnapshot>` nie powinien zawierać właściwości
typu `Fdc`/`Crtc`/`Cassette`, gdyby powstał.

### Projekty rodzin sprzętowych

```text
PetEmulator.Cpc          — ISTNIEJE: CpcGateArray, CpcKeyboard, CpcCassette,
                            CpcMachineClock, CpcMachineSnapshot
PetEmulator.Pet          — Faza 1, nie rozpoczęte
PetEmulator.Vic20        — Faza 1, nie rozpoczęte
PetEmulator.Trs80        — Faza 1, nie rozpoczęte
PetEmulator.Kaypro       — Faza 1, nie rozpoczęte
```

`CpcMachineBase<TSnapshot>` jako korzeń `Cpc464Machine`/`Cpc6128Machine` —
**odrzucone**, patrz Faza 0 punkt 4.

## Snapshoty — wzorzec (zaimplementowany dla CPC, wzór dla reszty)

```text
MachineSnapshot (Core, abstract)         — ISTNIEJE
├── CpcMachineSnapshot (Cpc, abstract)   — ISTNIEJE
│   ├── Cpc464Snapshot                   — ISTNIEJE
│   │   └── Cpc464MemorySnapshot, Cpc464PortsSnapshot
│   └── Cpc6128Snapshot                  — ISTNIEJE
│       └── Cpc6128MemorySnapshot, Cpc6128PortsSnapshot, I8272Snapshot
├── PetMachineSnapshot                   — nie istnieje (Faza 1)
├── Vic20MachineSnapshot                 — nie istnieje (Faza 1)
├── Trs80MachineSnapshot                 — nie istnieje (Faza 1)
└── KayproMachineSnapshot                — nie istnieje (Faza 1)
```

Każdy model ma własny codec, nie dzielić formatu między modelami (np. nie
używać codeka CPC6128 do dekodowania snapshotu CPC464).

## Kontrakty opcjonalnych urządzeń (docelowe, Faza 1)

Kontrakt ma opisywać możliwości, nie konkretny sprzęt:

- `ICassetteDevice` — mount, play, stop, eject, sygnał wejściowy,
- `IDiskController` — lista napędów i operacje właściwe dla kontrolera,
- `IAudioDevice` — próbki audio lub bezpieczny null output (**cofnięte w
  wersji bez konsumenta** — dodać ponownie tylko razem z realnym
  odbiorcą),
- `IVideoDevice` — renderowanie i parametry obrazu.

Formaty DSK, JV1, DMK, D64 pozostają adapterami konkretnych kontrolerów,
nie częścią `IDiskController`.

## Wzorzec testów (docelowy, częściowo spełniony dla CPC)

Każda maszyna: konstrukcja i reset, CPU i cykle, mapowanie pamięci/ROM,
porty/dekoder busa, klawiatura, video i audio, media właściwe dla maszyny,
snapshot capture/restore, realny firmware boot (jeśli ROM dostępny). Testy
wspólne przez fixture/capability matrix, specyficzne w projekcie danej
maszyny. Brak FDC w CPC464 nie jest luką testową — oznaczyć jako
`NotApplicable`, nie pomijać milcząco.

## Kryteria akceptacji (całościowe, nie tylko CPC)

- żadna maszyna nie dziedziczy po innej maszynie tylko z powodu wspólnego
  CPU (spełnione: `CpcMachineBase<TSnapshot>` odrzucone),
- wspólne klasy nie zawierają warunków `if (machine is ...)`,
- UI i CLI zależą od capability interfaces,
- każda maszyna ma jawny snapshot i restore (spełnione dla CPC, nie dla
  reszty),
- opcjonalne urządzenia mają bezpieczny null object albo brak kontraktu,
- testy kontraktowe obejmują wszystkie realne implementacje `IMachine`
  (spełnione — `MachineContractTests.cs`),
- format dysku jest niezależny od ogólnego kontraktu napędu,
- pełny build i testy rodziny przechodzą po każdej migracji.

## Ryzyka

- zbyt szeroki `MachineBase` może ukryć różnice sprzętowe (stąd odrzucenie
  `CpcMachineBase<TSnapshot>` powyżej),
- wspólny snapshot może zacząć wymuszać urządzenia, których maszyna
  fizycznie nie posiada,
- wspólny kontrakt dysku może pomieszać różne FDC,
- migracja wszystkich maszyn naraz zwiększy promień regresji — stąd Faza 0
  jako bounded dowód zanim cokolwiek inne się ruszy, i stąd sprzątanie
  natychmiast po tym jak dwa punkty Fazy 1 wystartowały bez decyzji.

Pierwszym celem pozostają kontrakty i macierz testów, nie masowa konwersja
klas maszyn.
