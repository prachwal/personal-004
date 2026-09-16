# Plan sprzątania po planie kompozycji maszyn

Trzy małe, niezależne zadania. Znalezione podczas kontroli jakości commitów
`fd62ed5..f38ad3c` (patrz `docs/plans/2026-09-16-machine-composition-and-reuse-plan.md`).
Każde zadanie to **jeden bounded commit**, bez zależności między nimi — wykonuj
w dowolnej kolejności, nie mieszaj dwóch zadań w jednym commicie.

Zasady wykonania (jak w innych planach tego repo, patrz `CLAUDE.md`):

1. Impact analysis przed edycją symbolu (`impact` MCP lub CLI fallback).
2. `detect_changes` przed commitem.
3. Po każdym zadaniu: `dotnet build PetEmulator.slnx --nologo` (0/0) i
   `dotnet test` na wskazanym niżej projekcie.
4. Jeśli GitNexus MCP nie działa (np. `spawnSync git EPERM`, obcięty indeks) —
   nie blokuj się na tym, pracuj z grepem/Read i napisz to wprost w commit
   message zamiast milczeć.

---

## Zadanie 1 — usunąć martwe pole `CycleCount` z `MachineSnapshot`

**Fakt:** `lib/PetEmulator.Core/Abstractions/MachineSnapshot.cs:10` ma pole
`CycleCount`. `Cpc464Machine.CaptureState()`/`Cpc6128Machine.CaptureState()`
je ustawiają, ale **żaden** `RestoreState()` go nie czyta. Faktyczne cykle CPU
wracają wyłącznie przez `_cpu.RestoreSnapshot(state.Cpu)` — `CpuDebugSnapshot`
już ma własne `CycleCount`, restorowane w `CpuProcessorBase.RestoreSnapshot`
(`lib/PetEmulator.Core/Cpu/CpuProcessorBase.cs:68`, `Clock.Advance(snapshot.CycleCount)`).
Pole na `MachineSnapshot` jest więc 100% redundantne — zapisywane i porzucane,
zero testu je czyta.

**Decyzja: usunąć, nie dodawać testu spójności.** Nie ma dwóch źródeł prawdy
do synchronizowania — jest jedno (`Cpu.CycleCount`) i jedno martwe pole obok.
Test "niespójności" pilnowałby czegoś, co nie powinno istnieć.

**Kroki:**

1. Usuń `public ulong CycleCount { get; set; }` z `MachineSnapshot.cs`.
2. Usuń `CycleCount = CycleCount,` z `Cpc464Machine.CaptureState()`
   (`src/PetEmulator.Cpc464/Cpc464Machine.cs`) i z
   `Cpc6128Machine.CaptureState()` (`src/PetEmulator.Cpc6128/Cpc6128Machine.cs`).
3. Build — jeśli coś jeszcze czyta `snapshot.CycleCount` (mało prawdopodobne,
   zgodnie z powyższym grepem), kompilator to wskaże.

**Weryfikacja:**

```
dotnet build PetEmulator.slnx --nologo
dotnet test tests/PetEmulator.Cpc464.Tests/PetEmulator.Cpc464.Tests.csproj --filter "TestCategory!=BinaryBoot" --nologo
dotnet test tests/PetEmulator.Cpc6128.Tests/PetEmulator.Cpc6128.Tests.csproj --filter "TestCategory!=BinaryBoot" --nologo
```

**Commit:** `refactor: remove dead CycleCount field from MachineSnapshot`

---

## Zadanie 2 — usunąć nieużywaną abstrakcję `IAudioDevice`

**Fakt:** `cd85816` dodał `IAudioDevice`/`NullAudioDevice`/`AudioDevice` do
`lib/PetEmulator.Audio/AudioContracts.cs` i `AudioDevice { get; }` do
`IMachineViewModel` (`src/PetEmulator.Desktop/ViewModels/IMachineViewModel.cs:32`),
wdrożone we wszystkich 6 ViewModeli (Pet/Vic20/Trs80/Kaypro/Cpc464/Cpc6128).
Grep po całym repo: **zero konsumentów** poza deklaracją i jednym testem,
który sprawdza tylko że property istnieje na interfejsie
(`typeof(IMachineViewModel).GetProperty("AudioDevice").Should().NotBeNull()`)
— nie że cokolwiek go czyta czy że zwraca poprawną wartość.

To też dotyka PET/VIC-20/TRS-80/Kaypro, czyli Fazę 1 z planu kompozycji, na
którą plan wymaga osobnej decyzji ("nie startuje automatycznie po Fazie 0") —
decyzji tej nigdzie nie zapisano przed `cd85816`.

**Decyzja: usunąć całość, nie "deferować".** Deferowanie bez usunięcia to
martwy kod siedzący w 8 plikach. Jeśli realny konsument się pojawi (np.
wizualizator audio w UI), dodać `IAudioDevice` wtedy, z tym konsumentem w tym
samym commicie — nie wcześniej.

**Kroki (jeden commit, cofa dokładnie `cd85816`):**

1. `lib/PetEmulator.Audio/AudioContracts.cs` — usuń `IAudioDevice`,
   `NullAudioDevice`, `AudioDevice`.
2. `src/PetEmulator.Desktop/ViewModels/IMachineViewModel.cs` — usuń
   `IAudioDevice AudioDevice { get; }`.
3. W każdym z: `Cpc464MachineViewModel.cs`, `Cpc6128MachineViewModel.cs`,
   `KayproMachineViewModel.cs`, `PetMachineViewModel.cs`,
   `Trs80MachineViewModel.cs`, `Vic20MachineViewModel.cs` — usuń pole
   `_audioDevice`, jego inicjalizację w konstruktorze (`new AudioDevice(...)`
   / `new NullAudioDevice()`) i property `AudioDevice`. Zostaw
   `_audioOutput`/`AudioOutput` bez zmian — to osobny, używany kontrakt.
4. `tests/PetEmulator.Desktop.Tests/MainWindowViewModelTests.cs` — usuń blok
   `typeof(IMachineViewModel).GetProperty(nameof(IMachineViewModel.AudioDevice))...`.

**Weryfikacja:**

```
dotnet build PetEmulator.slnx --nologo
dotnet test tests/PetEmulator.Desktop.Tests/PetEmulator.Desktop.Tests.csproj --nologo
```

**Commit:** `refactor: remove unused IAudioDevice abstraction`

---

## Zadanie 3 — złożyć `IKeyboardViewModel` z powrotem do `IMachineViewModel`

**Fakt:** `f38ad3c` wydzielił `IKeyboardViewModel.HandleKey` z
`IMachineViewModel` do osobnego interfejsu w
`src/PetEmulator.Desktop/ViewModels/IKeyboardViewModel.cs`, który importuje
`HostKeyEventKind` z `PetEmulator.Pet.Keyboard` (`IPetKeyboardMap.cs`) —
typ zdarzenia klawiatury zdefiniowany w projekcie PET. Efekt: "wspólna,
rodzino-neutralna" capability w nazwie faktycznie ciągnie zależność od PET
przez wszystkie maszyny (VIC-20, TRS-80, Kaypro, CPC464, CPC6128), które
same w sobie nie mają nic wspólnego z PET.

Grep: `IKeyboardViewModel` ma dokładnie jednego konsumenta —
`IMachineViewModel : IShellModule, IKeyboardViewModel` — nikt nigdzie nie
patternmatchuje `is IKeyboardViewModel` niezależnie od `IMachineViewModel`.
Rozdzielenie nie daje dziś żadnej wartości: to warstwa bez odbiorcy, tak jak
`IAudioDevice` w Zadaniu 2, tylko mniejsza.

**Decyzja: złożyć z powrotem, nie przenosić `HostKeyEventKind`.** Przenosinę
typu do Core/Desktop-neutral rozważyć dopiero gdy pojawi się realny drugi
konsument `IKeyboardViewModel` niezależny od `IMachineViewModel` — dziś go
nie ma, więc to migracja bez powodu.

**Kroki:**

1. Usuń plik `src/PetEmulator.Desktop/ViewModels/IKeyboardViewModel.cs`.
2. W `IMachineViewModel.cs`: `public interface IMachineViewModel : IShellModule`
   (usuń `, IKeyboardViewModel`), dodaj z powrotem
   `void HandleKey(Key key, HostKeyEventKind kind);` do ciała interfejsu
   (dokładnie tam gdzie `f38ad3c` je wyciął — patrz diff tego commita).
3. `tests/PetEmulator.Desktop.Tests/MainWindowViewModelTests.cs` — usuń 6
   asercji `Should().Implement<IKeyboardViewModel>()`; asercje
   `Implement<IMachineViewModel>()` już pokrywają `HandleKey`, nic nie traci
   pokrycia.

**Weryfikacja:**

```
dotnet build PetEmulator.slnx --nologo
dotnet test tests/PetEmulator.Desktop.Tests/PetEmulator.Desktop.Tests.csproj --nologo
```

**Commit:** `refactor: fold IKeyboardViewModel back into IMachineViewModel`

---

## Po wykonaniu wszystkich trzech

`dotnet test PetEmulator.slnx --filter "TestCategory!=BinaryBoot" --nologo` —
cały solution zielony. Dopisz do
`docs/plans/2026-09-16-machine-composition-and-reuse-plan.md`, sekcja
"Status wdrożenia" albo nowa sekcja "Sprzątanie — 2026-09-16": które z tych
trzech zadań wykonane, z hashami commitów.
