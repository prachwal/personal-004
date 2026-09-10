# GitNexus Engineering Plan

> Task: dodać komendy dyskowe VIC-20 w CLI oraz pełny status/LED stacji w Desktop.
> Evidence verified at commit `e9f758d07cb227186fc7872c2f0d9a7158f6fd45`; GitNexus index is 1 commit behind HEAD, so source verification is authoritative.
> Evidence provenance schema 2; global dirty digest `42a863282d5030f7a315c920f0f54ce375ce5728504897d00d6e5ab0f4629531`.

## Objective (§1)

Umożliwić sterowanie zamontowanym dyskiem VIC-20 przez `Vic20DebuggerSession` oraz udostępnić w Desktop przyciski Load/New Disk i dedykowany wskaźnik aktywności IEC, bez zmian w działającym backendzie IEC/D64/DOS.

## Current Behaviour (§2–3)

- [verified] `Vic20Machine.MountDisk`/`MountNewDisk` tworzą `PetIeeeDiskDrive`, podpinają go do `Vic20SerialBus` i publikują `PetIeeeDriveStatus` (`src/PetEmulator.Vic20/Vic20Machine.cs:107-130`).
- [verified] `Vic20Machine.StepInstruction` wywołuje `Vic20SerialBusBinding.Tick` dla każdego cyklu CPU (`Vic20Machine.cs:180-184`); transport IEC i testy E2E już działają.
- [verified] `Vic20DebuggerSession` obsługuje `roms`, `key`, `type`, `status` oraz generyczny debugger, ale nie ma `disk`/`new-disk`.
- [verified] `Vic20MachineView` pokazuje generyczne `Devices`, lecz nie renderuje `DiskDriveControl`; `DiskDriveControl` jest silnie typowany do `PetMachineViewModel`.
- [verified] `MainWindowViewModel.LoadDisk/NewDisk` routują wyłącznie do `PetMachineViewModel`.
- [verified] PET ma wzorzec `PollDiskActivity` + `DiskBusy` + `DiskIconBrush` z 200 ms podtrzymaniem LED; VIC nie ma analogicznego API aktywności.

## Findings (§4–5)

- [graph] GitNexus `impact(Vic20DebuggerSession, upstream)` raportuje 5 bezpośrednich zależności, w tym `Program.RunVic20DebugScript` i testy CLI; ryzyko HIGH.
- [graph] GitNexus `impact(Vic20MachineViewModel, upstream)` wskazuje granicę `IMachineViewModel`; źródło potwierdza, że zmiana dotknie widoku VIC i shellu Desktop.
- [verified] `Vic20SerialBus.Activity` już emituje zdarzenia `byte` i `line-change`; plan powinien dodać tylko maszynowy latch/poll analogiczny do PET, nie nowe zdarzenia UI.
- [inferred] Najmniejsza bezpieczna integracja Desktopu to współdzielony kontrakt widoku dysku dla `PetMachineViewModel` i `Vic20MachineViewModel`, zamiast drugiego, duplikowanego kontrolka XAML.

## Proposed Changes (§6)

1. `src/PetEmulator.Cli/Vic20DebuggerSession.cs` — dodać `disk <path> [device]` i `new-disk <path> [diskName] [device]`, z domyślnym urządzeniem 8, delegujące do istniejących metod maszyny; zachować lazy construction i konwencję `error:`.
2. `tests/PetEmulator.Cli.Tests/Vic20DebuggerSessionTests.cs` — pokryć mount istniejącego D64, utworzenie/sformatowanie nowego D64, status urządzenia i błąd przed `roms`.
3. `src/PetEmulator.Vic20/Vic20Machine.cs` — dodać `HasDisk` oraz `PollDiskActivity`; latch powinien reagować wyłącznie na `Vic20SerialActivity.Kind == "byte"`, a `Activity` musi być podłączone w konstruktorze i wyzerowane przy resecie.
4. `src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs` — dodać `LoadDisk`, `NewDisk`, `DiskLoaded`, `DiskBusy`, `DiskIconBrush`, 200 ms linger i polling w `Tick`; wykluczyć napęd 8 z generycznej listy `Devices`, tak jak w PET.
5. `src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs` — routować Load/New Disk do aktualnego PET albo VIC ViewModelu; komunikat „PET only” usunąć lub uogólnić.
6. `src/PetEmulator.Desktop/Views/Controls/DiskDriveControl.axaml` i kontrakt ViewModelu — uogólnić `x:DataType` z `PetMachineViewModel` do małego interfejsu dysku (`DiskLoaded`, `DiskBusy`, `DiskIconBrush`), implementowanego przez oba ViewModele.
7. `src/PetEmulator.Desktop/Views/Vic20MachineView.axaml` — dodać współdzielony `DiskDriveControl` obok Datasette; pozostawić `Devices` dla dodatkowych napędów/statusów.

## Implementation Sequence (§7)

1. Dodać testy CLI i implementację `disk`/`new-disk`; zweryfikować, że urządzenie 8 i ścieżki błędów zachowują się jak w PET.
2. Dodać latch aktywności i `HasDisk` w `Vic20Machine`; rozszerzyć testy maszyny o aktywność IEC i reset latcha.
3. Wprowadzić wspólny kontrakt dyskowego ViewModelu, przenieść `DiskDriveControl` na ten kontrakt i zachować istniejące zachowanie PET.
4. Podłączyć VIC ViewModel, routing shellu i widok XAML; sprawdzić compiled bindings.
5. Uruchomić testy CLI/PET/VIC oraz build Desktop; dopiero potem zaktualizować dokumentację opisującą VIC jako obsługujący dysk w Desktop/CLI.

## Test Strategy (§8)

- CLI: `disk` po `roms` pokazuje urządzenie, `new-disk` tworzy poprawny D64, brak `roms` zwraca `error:`.
- VIC machine: aktywność `byte` ustawia latch, `PollDiskActivity()` zwraca true tylko raz, `Reset()` czyści stan.
- Desktop: testy ViewModelu, jeśli istnieje infrastruktura; obowiązkowo build z compiled bindings.
- E2E istniejące `Vic20DiskEndToEndTests` pozostają bez zmian i nadal potwierdzają `LOAD`/`SAVE` przez realny KERNAL.

## Implementation Context (§11)

```yaml
implementation_context:
  task_summary: "Expose existing VIC-20 IEC disk support through CLI and Desktop status/LED."
  acceptance_criteria:
    - "vic20-debug accepts disk and new-disk"
    - "VIC Desktop can load/create a D64 and shows mounted drive plus IEC activity LED"
    - "PET disk UI and CLI behavior remain unchanged"
    - "existing VIC IEC E2E tests remain green"
  evidence_provenance:
    schema_version: 2
    head_commit: e9f758d07cb227186fc7872c2f0d9a7158f6fd45
    generated_plan_path: docs/plans/2026-09-10-gitnexus-plan-vic20-disk-cli-desktop.md
    global_dirty_digest:
      algorithm: sha256
      canonicalization: gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records
      value: 42a863282d5030f7a315c920f0f54ce375ce5728504897d00d6e5ab0f4629531
    cited_path_manifest:
      - {path: src/PetEmulator.Cli/Program.cs, state: clean, head_digest: sha256:30c3ba04f2f368d8db206fb3abe065cd79402bd67fbaeb7b8eb0b102394fac93}
      - {path: src/PetEmulator.Cli/Vic20DebuggerSession.cs, state: clean, head_digest: sha256:fba05c48b00f23159364676ac2275067dfe736be0bbefd771c5af27e82127407}
      - {path: src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs, state: clean, head_digest: sha256:d0f6ca7812828fda17070f2f82a16c29cb87e7e6576e24f9ea2a032a531b161f}
      - {path: src/PetEmulator.Desktop/ViewModels/PetMachineViewModel.cs, state: clean, head_digest: sha256:c074d337c40111dcb0358add6eaa87ee17afafad2f7f43300930d1df62bf81c3}
      - {path: src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs, state: clean, head_digest: sha256:4c5cb1e21b0776327a7a8f2526b385fcc8fd16214eb8835fe074384a63e23d50}
      - {path: src/PetEmulator.Desktop/Views/Controls/DiskDriveControl.axaml, state: clean, head_digest: sha256:05e899ef398d726963c84add329bc8c48e2ba400ef05ad7f673d56c96b752e7d}
      - {path: src/PetEmulator.Desktop/Views/PetMachineView.axaml, state: clean, head_digest: sha256:9e92f34638db9f109a8b58d65e5cc6f634a1abfa8de590523fdfd6db349e0174}
      - {path: src/PetEmulator.Desktop/Views/Vic20MachineView.axaml, state: clean, head_digest: sha256:3c81d7f330bcefa7c41e04d8caae80c082ebee6e5a44d7992fa0fbacffddf03}
      - {path: src/PetEmulator.Vic20/Vic20Machine.cs, state: clean, head_digest: sha256:9a62dd598dd291b1a57cba9ca8e6e091169216ecc2d892311c7e24e535995efd}
      - {path: tests/PetEmulator.Cli.Tests/Vic20DebuggerSessionTests.cs, state: clean, head_digest: sha256:1e99b1a1ad30ac061af33641bd1e1dbf198484e49f9896ee5ff402a9f589cd2e}
      - {path: tests/PetEmulator.Vic20.Tests/Serial/Vic20DiskEndToEndTests.cs, state: clean, head_digest: sha256:9d0441018749291ff57f968a230ac6f2d9d3cbc36b206cb7face042f36203acd}
  primary_symbols:
    - {symbol: Vic20DebuggerSession, file: src/PetEmulator.Cli/Vic20DebuggerSession.cs, lines: "18-137", role: CLI command dispatch}
    - {symbol: Vic20Machine, file: src/PetEmulator.Vic20/Vic20Machine.cs, lines: "107-130,180-184", role: disk mount and IEC tick}
    - {symbol: Vic20MachineViewModel, file: src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs, lines: "83-157", role: Desktop status/tick}
    - {symbol: MainWindowViewModel, file: src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs, lines: "117-160", role: disk command routing}
  related_symbols:
    - {symbol: PetIeeeDiskDrive, relationship: reused-device-adapter, relevance: shared D64/DOS device}
    - {symbol: Vic20SerialBus, relationship: transport, relevance: IEC byte/line activity source}
    - {symbol: Vic20DiskEndToEndTests, relationship: regression-test, relevance: real KERNAL LOAD/SAVE}
  execution_path:
    - "CLI command -> Vic20DebuggerSession -> Vic20Machine.MountDisk/MountNewDisk"
    - "Desktop menu -> MainWindowViewModel routing -> Vic20MachineViewModel -> Vic20Machine"
    - "VIA activity -> Vic20SerialBus -> Activity byte latch -> ViewModel Tick -> DiskIconBrush"
  architectural_patterns:
    - {pattern: "PET disk command and LED pattern", example_location: src/PetEmulator.Cli/PetDebuggerSession.cs and src/PetEmulator.Desktop/ViewModels/PetMachineViewModel.cs, usage_guidance: mirror behavior without reusing IEEE-488 transport}
    - {pattern: "generic device status plus dedicated primary-device control", example_location: src/PetEmulator.Desktop/Views/PetMachineView.axaml, usage_guidance: use same layout for VIC}
  files_to_modify:
    - {file: src/PetEmulator.Cli/Vic20DebuggerSession.cs, symbols: [Execute, LoadDisk, NewDisk], intended_change: add VIC disk commands}
    - {file: src/PetEmulator.Vic20/Vic20Machine.cs, symbols: [constructor, Reset, PollDiskActivity, HasDisk], intended_change: expose activity latch}
    - {file: src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs, symbols: [LoadDisk, NewDisk, Tick, DiskIconBrush], intended_change: wire disk status and LED}
    - {file: src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs, symbols: [LoadDisk, NewDisk], intended_change: route PET or VIC}
    - {file: src/PetEmulator.Desktop/Views/Controls/DiskDriveControl.axaml, symbols: [DiskDriveControl], intended_change: bind to shared disk contract}
    - {file: src/PetEmulator.Desktop/Views/Vic20MachineView.axaml, symbols: [device bar], intended_change: add disk control}
  tests:
    - {file: tests/PetEmulator.Cli.Tests/Vic20DebuggerSessionTests.cs, scenarios: ["disk after roms -> mounted device", "new-disk -> formatted file and status", "before roms -> error"]}
    - {file: tests/PetEmulator.Vic20.Tests/Vic20MachineTests.cs, scenarios: ["byte activity -> poll true once", "reset -> activity false"]}
    - {file: tests/PetEmulator.Vic20.Tests/Serial/Vic20DiskEndToEndTests.cs, scenarios: ["existing LOAD/SAVE/LOAD/RUN regression"]}
  verification_commands:
    - "dotnet test tests/PetEmulator.Cli.Tests/PetEmulator.Cli.Tests.csproj --no-restore"
    - "dotnet test tests/PetEmulator.Vic20.Tests/PetEmulator.Vic20.Tests.csproj --no-restore"
    - "dotnet build src/PetEmulator.Desktop/PetEmulator.Desktop.csproj --no-restore"
  pdg_constraints: []
  risks:
    - "MainWindowViewModel and compiled XAML bindings are shared Desktop boundaries."
    - "IEC Activity must count byte transfers, not line changes, or the LED will remain permanently busy."
    - "Do not route VIC through PetIeeeBus; its transport is Vic20SerialBus."
  assumptions:
    - "A small shared disk ViewModel interface is acceptable; verify binding generation before implementation."
    - "Existing PetIeeeDriveStatus remains semantically acceptable for VIC or is renamed to a transport-neutral status type."
  open_questions:
    - "Should the status Id remain ieee488:8 for compatibility, or change to a transport-neutral identifier with test updates?"
  avoid:
    - "Do not modify Vic20SerialBus protocol timing or PetIeeeDiskDrive/D64/CbmDos behavior."
    - "Do not duplicate DiskDriveControl for VIC."
    - "Do not expose raw Activity events directly to Avalonia; keep polling/latch logic in the ViewModel boundary."
```

## Assumptions and Open Questions (§12)

- Decide whether to preserve `ieee488:<n>` status IDs for compatibility or introduce `iec:<n>`/a neutral ID; source currently uses the former in VIC.
- Confirm compiled binding accepts the selected shared disk contract before changing XAML.
- Keep the existing untracked `docs/plans/*.md` files outside implementation commits unless explicitly requested.

## Definition of Done (§13)

- `vic20-debug` supports `disk` and `new-disk` with tests.
- Desktop Load/New Disk works for VIC-20 and the disk icon is gray/green/red according to mount/activity state.
- PET behavior remains green under its existing tests.
- VIC serial unit, binding, machine, and E2E tests pass; Desktop build passes without binding errors.
