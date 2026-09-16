# Plan: wspólna kompozycja i struktura emulatorów

## Cel

Ujednolicić strukturę PET, VIC-20, CPC464, CPC6128, TRS-80 i Kaypro tak, aby:

- wspólne zachowania były implementowane raz,
- maszyny nadal zachowywały własne mapy pamięci, porty i urządzenia,
- snapshot, reset, zegar i testy miały spójny wzorzec,
- UI, CLI i headless debug korzystały z tych samych kontraktów.

Nie tworzyć jednej uniwersalnej klasy zawierającej wszystkie możliwe urządzenia.

## Status wdrożenia

- [x] Wspólny zegar CPU (`IClock`/`EmulationClock`) oraz wspólny zegar urządzeń CPC (`CpcMachineClock`).
- [x] Wspólna baza snapshotu (`MachineSnapshot`) i pełne snapshoty CPC464/CPC6128.
- [x] Macierz kontraktu `IMachine` obejmuje wszystkie realne maszyny, w tym CPC6128.
- [x] Audio, taśma, routing capability Desktop i headless debug CPC są już dostępne w bieżącej implementacji.
- [ ] Pełne snapshoty PET, VIC-20, TRS-80 i Kaypro.
- [ ] Wspólne kontrakty urządzeń w Core (`IKeyboardDevice`, `ICassetteDevice`, `IDiskController`, `IVideoDevice`) z implementacjami/null-adapterami.

### Sprzątanie CPC composition — 2026-09-16

- [x] Usunięto martwe `MachineSnapshot.CycleCount` (`5ea5067`).
- [x] Usunięto nieużywane `IAudioDevice`, `AudioDevice` i `NullAudioDevice` (`78091dc`).
- [x] Scalono `IKeyboardViewModel` z `IMachineViewModel` (`ceed646`).

Powyższe punkty oznaczone jako ukończone zostały zweryfikowane w kodzie i testach. Pozostałe wymagają implementacji, a nie tylko dopisania typów lub dokumentacji.

## Zasada architektoniczna

Stosować trzy poziomy współdzielenia:

```text
kontrakty globalne
    ↓
implementacje wspólne dla rodziny sprzętu
    ↓
kompozycja konkretnej maszyny
```

Dziedziczenie stosować wyłącznie dla stabilnego, wspólnego stanu lub cyklu życia. Urządzenia podłączać przez kompozycję.

## Docelowe projekty

### `PetEmulator.Core`

Kontrakty niezależne od rodziny:

- `IMachine` — reset, krok, run, CPU, pamięć i status,
- `IMemoryBus` — odczyt i zapis pamięci,
- `IProcessor` — wspólny dostęp do CPU,
- `IMachineSnapshot` — wersja i metadane snapshotu,
- `IMachineStateStore` — capture/restore,
- `IKeyboardDevice`, `ICassetteDevice`, `IDiskController`, `IVideoDevice`, `IAudioDevice`,
- `INull*`/implementacje null dla opcjonalnych urządzeń.

Kontrakty nie mogą zakładać konkretnego CPU, mapy pamięci ani formatu dysku.

**Koszt retrofitu, nie projektowanie od zera:** `Cpc464Machine`/`Cpc6128Machine` już mają działające,
przetestowane `CaptureState()`/`RestoreState(TSnapshot)` (zwykłe metody, bez wspólnego interfejsu) —
patrz `src/PetEmulator.Cpc464/Cpc464Machine.cs`, `src/PetEmulator.Cpc6128/Cpc6128Machine.cs`,
`Cpc464Snapshot`/`Cpc6128Snapshot` oba dziedziczą już po `CpcMachineSnapshot`. Wprowadzenie
`IMachineStateStore`/`IMachineSnapshot` oznacza dotknięcie tych dwóch już-gotowych, przetestowanych
klas ponownie (zmiana nazw metod na kontrakt interfejsu, ewentualne opakowanie `Cpc464Snapshot`/
`Cpc6128Snapshot` żeby spełniały `IMachineSnapshot`) — wliczyć to jako koszt kroku 1, nie traktować
jako "dodanie nowego kontraktu obok" bez wpływu na istniejący kod.

### `PetEmulator.Hardware` lub `PetEmulator.Machines`

Wspólna infrastruktura techniczna, bez logiki konkretnego komputera:

- `MachineBase<TSnapshot>` — minimalny cykl życia maszyny,
- `MachineClock` — naliczanie cykli i tick urządzeń,
- `MachineDeviceSet` — jawny zestaw urządzeń,
- `PortDispatcher` — opcjonalny dekoder portów,
- `RomOverlay`, `MemoryWindow`, `BankedMemory`,
- walidacja wersji i rozmiaru snapshotu.

`MachineBase<TSnapshot>` nie powinien zawierać właściwości typu `Fdc`, `Crtc` lub `Cassette`.

### Projekty rodzin sprzętowych

```text
PetEmulator.Cpc
├── CpcGateArray
├── CpcKeyboard
├── CpcCassette
├── CpcPpi
├── CpcMachineSnapshot
└── CpcMachineBase<TSnapshot>   (opcjonalnie, po migracji obu modeli)

PetEmulator.Pet
├── PetDevices
├── PetMachineSnapshot
└── PetMachine

PetEmulator.Vic20
├── Vic20Devices
├── Vic20MachineSnapshot
└── Vic20Machine

PetEmulator.Trs80
├── Trs80Devices
├── Trs80MachineSnapshot
└── Trs80Machine

PetEmulator.Kaypro
├── KayproDevices
├── KayproMachineSnapshot
└── KayproMachine
```

## Docelowa hierarchia maszyn

```text
IMachine
└── MachineBase<TSnapshot>
    ├── PetMachine
    ├── Vic20Machine
    ├── CpcMachineBase<TSnapshot>
    │   ├── Cpc464Machine
    │   └── Cpc6128Machine
    ├── Trs80Machine
    └── KayproMachine
```

`CpcMachineBase<TSnapshot>` można wprowadzić tylko wtedy, gdy oba CPC mają identyczny cykl CPU/tick/reset.

**Stan sprawdzony w kodzie (2026-09-16): różnice są dziś istotne, warunek nie jest spełniony.**
`Cpc464Bus.Tick(cycles)` napędza `GateArray.Tick()`/`Cassette.Tick()` w krokach `cycles / 4`
wewnątrz magistrali; `Cpc6128Machine.StepInstruction()` robi to samo bezpośrednio w maszynie
(z pominięciem `Bus`), dokłada osobno `_fdc.Tick(cycles)` bez dzielenia przez 4 i liczy ramki
we własnym polu `_frameCycles`, którego `Cpc464Machine` w ogóle nie ma. Właściciel pętli tick
jest dziś wspólny (`CpcMachineClock`), ale kolejność próbkowania IRQ nadal nie jest wspólna:
`Cpc464Machine` wykonuje `_clock.Tick()` i dopiero potem `SetInt`, natomiast
`Cpc6128Machine` również wykonuje `_clock.Tick()` i dopiero potem `SetInt`. Dzięki temu CPU
próbuje stan wyjścia IRQ Gate Array po wykonaniu kroku urządzeń. Regresję tej kolejności pokrywa
`Cpc6128MachineTests.InterruptLineSamplesGateArrayAfterDeviceTick`; CPC464 zachowuje tę samą
kolejność w istniejącym właścicielu pętli.

Jeśli po tym ujednoliceniu nadal zostaną istotne różnice, pozostawić obie klasy bez wspólnej
klasy maszynowej i współdzielić wyłącznie urządzenia oraz snapshot bazowy (to już działa —
patrz `CpcMachineSnapshot` w `src/PetEmulator.Cpc/CpcMachineSnapshot.cs`, użyte przez oba modele).

## Wzorzec kompozycji

```text
MachineBase
├── Processor
├── MemoryBus
├── MachineClock
├── VideoDevice
├── AudioDevice
├── KeyboardDevice?
├── CassetteDevice?
├── DiskController?
└── MachineSnapshot
```

Każda maszyna deklaruje jawnie, które urządzenia posiada. Brak fizycznego urządzenia reprezentować przez `NullAudioDevice`, `NullCassetteDevice` albo brak kontrolera, zależnie od potrzeb API.

## Snapshoty

### Wspólne

```csharp
public abstract class MachineSnapshot : IMachineSnapshot
{
    public int Version { get; set; } = 1;
    public CpuDebugSnapshot Cpu { get; set; } = null!;
    public ulong CycleCount { get; set; }
}
```

### Rodzinne

```text
CpcMachineSnapshot
├── wspólny CPU/cykle
├── GateArray
├── CRTC
├── AY
├── Keyboard
└── Cassette
```

### Konkretne

```text
Cpc464Snapshot
├── Cpc464MemorySnapshot
└── Cpc464PortsSnapshot

Cpc6128Snapshot
├── Cpc6128MemorySnapshot
├── Cpc6128PortsSnapshot
└── I8272Snapshot
```

Każdy model powinien mieć własny codec albo jawnie używać wspólnego formatu z identyfikatorem typu maszyny. Nie używać codeców CPC6128 do dekodowania snapshotów CPC464.

## Kontrakty opcjonalnych urządzeń

```text
IMachine
├── IKeyboardDevice?
├── ICassetteDevice?
├── IDiskController?
├── IVideoDevice
└── IAudioDevice
```

Kontrakt powinien opisywać możliwości, a nie konkretny sprzęt:

- `ICassetteDevice` — mount, play, stop, eject, sygnał wejściowy,
- `IDiskController` — lista napędów i operacje właściwe dla danego kontrolera,
- `IAudioDevice` — próbki audio lub bezpieczny null output,
- `IVideoDevice` — renderowanie i parametry obrazu.

Formaty DSK, JV1, DMK i D64 pozostają adapterami konkretnych kontrolerów, nie częścią `IDiskController`.

## Kolejność wdrożenia

Nie otwierać wszystkich punktów naraz. Faza 0 dowodzi wzorca na jednej już-gotowej rodzinie
(CPC — ma dziś oba modele, wspólny projekt `PetEmulator.Cpc` i pełne snapshoty), zanim cokolwiek
dotknie PET/VIC-20/TRS-80/Kaypro. Dopiero po Fazie 0 decydować, czy Faza 1 w ogóle jest warta
kosztu — nie zakładać z góry, że tak.

### Faza 0 — dowód na CPC (jedyny bounded pierwszy krok)

1. Ujednolicić właściciela pętli tick między `Cpc464Machine`/`Cpc6128Machine` (patrz różnica
   opisana wyżej przy `CpcMachineBase<TSnapshot>`) — warunek wstępny dla wszystkiego dalej.
2. Dodać `IMachineStateStore`/`IMachineSnapshot` w `PetEmulator.Core` **jako opakowanie** nad
   istniejącymi `CaptureState`/`RestoreState` obu maszyn CPC (koszt opisany wyżej) — nie pisać
   nowej logiki snapshotu od zera.
3. Dopiero teraz, z ujednoliconym tickiem i wspólnym interfejsem snapshotu potwierdzonym na
   dwóch realnych maszynach, ocenić `CpcMachineBase<TSnapshot>`: albo wprowadzić, albo jawnie
   odrzucić i zapisać dlaczego (patrz sekcja o `CpcMachineBase<TSnapshot>` wyżej).
4. Pełny build + `PetEmulator.Cpc464.Tests`/`PetEmulator.Cpc6128.Tests` zielone po każdym punkcie.

### Faza 1 — reszta rodzin (dopiero po zamknięciu Fazy 0, osobna decyzja go/no-go)

5. Dodać wspólny `MachineClock` — najpierw jako ekstrakcja z tego, co Faza 0 już ujednoliciła
   w CPC, nie jako nowy abstrakcyjny byt projektowany z góry.
6. Dokończyć wspólny kontrakt audio oraz `NullAudioDevice` dla wszystkich maszyn.
7. Wydzielić wspólny kontrakt klawiatury i kasety poza CPC (CPC już ma `CpcKeyboard`/`CpcCassette`).
8. Dodać snapshoty PET, VIC-20, TRS-80 i Kaypro według schematu z Fazy 0.
9. Przenieść routing Desktop/CLI z testów typu konkretnego ViewModelu na capability interfaces.
10. Dodać macierz testów kontraktowych uruchamianą na wszystkich realnych maszynach.

Faza 1 nie startuje automatycznie po Fazie 0 — wymaga osobnej decyzji, że koszt migracji
pozostałych czterech rodzin jest tego wart, a nie że "skoro zrobiliśmy CPC, robimy resztę".

## Wzorzec testów

Każda maszyna powinna mieć testy:

- konstrukcja i reset,
- CPU i cykle,
- mapowanie pamięci/ROM,
- porty lub dekoder busa,
- klawiatura,
- video i audio,
- media właściwe dla maszyny,
- snapshot capture/restore,
- realny firmware boot, jeśli ROM jest dostępny.

Testy wspólne uruchamiać przez fixture/capability matrix, a testy specyficzne pozostawić w projekcie danej maszyny. Brak FDC w CPC464 nie jest luką testową; powinien być oznaczony jako `NotApplicable`.

## Kryteria akceptacji

- żadna maszyna nie dziedziczy po innej maszynie tylko z powodu wspólnego CPU,
- wspólne klasy nie zawierają warunków `if (machine is ...)`,
- UI i CLI zależą od capability interfaces,
- każda maszyna ma jawny snapshot i restore,
- opcjonalne urządzenia mają bezpieczny null object albo brak kontraktu,
- testy kontraktowe obejmują wszystkie realne implementacje `IMachine`,
- format dysku jest niezależny od ogólnego kontraktu napędu,
- pełny build i testy rodziny przechodzą po każdej migracji.

## Ryzyka

- zbyt szeroki `MachineBase` może ukryć różnice sprzętowe,
- wspólny snapshot może zacząć wymuszać urządzenia, których maszyna fizycznie nie posiada,
- wspólny kontrakt dysku może pomieszać różne FDC,
- migracja wszystkich maszyn naraz zwiększy promień regresji.

Dlatego pierwszym celem wdrożenia powinny pozostać kontrakty i macierz testów, a nie masowa konwersja klas maszyn.

## Wynik Fazy 0 — 2026-09-16

- Wspólny `CpcMachineClock` został wdrożony i jest używany przez CPC464 oraz CPC6128.
- Próbkowanie IRQ zostało ujednolicone: `_clock.Tick()` poprzedza `SetInt`; CPC6128 ma test regresyjny przejścia sygnału z Gate Array do linii CPU.
- `IMachineSnapshot` i `IMachineStateStore<TSnapshot>` zostały dodane jako cienkie kontrakty nad istniejącymi snapshotami.
- `CpcMachineBase<TSnapshot>` nie został wprowadzony. Po ujednoliceniu zegara nadal pozostają różnice należące do maszyny: CPC6128 ma FDC i licznik ramek, a CPC464 nie ma tych elementów.
- Decyzja: współdzielić zegar, urządzenia CPC i snapshot bazowy, ale pozostawić `Cpc464Machine` i `Cpc6128Machine` jako niezależne kompozycje.
- Boot CPC464 przechodzi izolowany test w około 6 sekund. `SieveProgram_SurvivesTypeSaveResetLoad` jest wolnym testem real-firmware SAVE/LOAD (około 2–3 minut), ale przechodzi deterministycznie i należy go traktować jako zaliczony. Nie mylić go z osobnym `tools/Cpc464SieveBenchmark`, który wykonuje właściwe sito do końca i pozostaje problemem wydajnościowym.
- Pełny zestaw `Cpc464.Tests` przechodzi `13/13`; pełny zestaw `Cpc6128.Tests` przechodzi `17/17`.
- Faza 1 dla PET/VIC-20/TRS-80/Kaypro pozostaje odłożona do osobnej decyzji.
