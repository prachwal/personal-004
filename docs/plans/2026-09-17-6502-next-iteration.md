# Kolejna iteracja 6502 — plan startu nowego repo na bazie personal-004

**Data:** 2026-09-17. **Status:** plan, nie rozpoczęte.
**Zasada tego pliku:** odznaczaj checkbox dopiero po weryfikacji w sesji
(build/test/grep) — nie na słowo poprzedniej sesji. To jedyne źródło prawdy.

Celem NIE jest kopia personal-004. Celem jest **jedna maszyna 6502**
(np. PET albo VIC-20, do wyboru w M0) zbudowana na zaimportowanym, a nie
sklonowanym, fundamencie. Wszystko spoza M0 jest jawnie nieautoryzowane
do momentu domknięcia kamienia milowego.

## 0. Decyzja startowa (M0) — jedna maszyna, nic więcej

- [ ] Wybrać dokładnie jedną maszynę docelową (propozycja: VIC-20 NTSC —
  mniejszy zakres niż PET, a ćwiczy VIC + 2×VIA + kartridże; albo PET 2001 —
  prostszy video, ale cięższy KERNAL/BASIC).
- [ ] Zapisać wybór w tym pliku (nazwa profilu, ROM-set, geometria ekranu).
- [ ] Zamrozić listę CPU: tylko `6502` (wariant NMOS). Bez 65C02/6510/2A03
  do M4. Warianty to konfiguracja tabeli, nie nowe klasy
  (patrz `docs/architecture/overview.md` — warstwy `Official6502 → Nmos6502`).

## 1. Co pobrać z personal-004 (allowlist importu, nic poza tym)

Import = kopia plików + ich testów + ich docs. Bez „pożyczania na chwilę".

### 1.1 Core — brać w całości, to jest kontrakt

- [ ] `lib/PetEmulator.Core/Abstractions/` — `IMachine`, `IProcessor`,
  `IMemoryBus`, `IMemoryMappedDevice`, `IDevice`, `IClock`,
  `IMachineStateStore<T>`/`IMachineSnapshot`/`MachineSnapshot`,
  `IInterruptLines`, `IFirqProcessor`, `IDebuggableProcessor`,
  `ICpuExecutionObserver`, `IDeviceStatus`.
- [ ] `lib/PetEmulator.Core/Cpu/` — `CpuProcessorBase<TState>`,
  `OpcodeTable`/`OpcodeDefinition`/`OpcodeKey`, `CpuState`,
  `CpuStepResult`/`CpuStepTrace`, `CpuDebugSnapshot`/`CpuStateSnapshot`,
  `CpuExecutionContext`.
- [ ] `lib/PetEmulator.Core/Timing/` — `MachineClock`, `EmulationClock`.
- [ ] `lib/PetEmulator.Core/Bus/BusAccess.cs`, `Extensions/MachineExtensions.cs`
  (`RunUntil/RunUntilOrStalled`), `Keyboard/AtKeyboard*`,
  `Logging/EmulatorLogging.cs`, `Serial/ISerialTransport.cs` +
  `BufferedSerialTransport.cs`.
- [ ] Dlaczego: to jest granica, która w personal-004 faktycznie zadziałała —
  CPU nie zna maszyny, maszyna nie zna UI.

### 1.2 CPU 6502 — brać rdzeń + jeden wariant

- [ ] `lib/PetEmulator.Cpu6502` — `Cpu6502Classic`, `CpuState`, tabele
  `Official6502`/`Nmos6502`, `CpuBehavior` (BCD, JMP-indirect bug, RMW).
- [ ] Od razu dodać defensywny cap z `docs/architecture/overview.md:238-250`:
  `if (_cycleCount > 7) throw` — w personal-004 to znane ryzyko hanga,
  nowa iteracja startuje z poprawką, nie z długiem.
- [ ] NIE brać: 6507/6510/2A03/65C02/R65C02S (wracają w M4 jako `DeriveWith()`).

### 1.3 Chipy — tylko te, których wymaga wybrana maszyna

- [ ] Dla VIC-20: `MOS6522`, `MOS2114`, `MOS6560` (`lib/PetEmulator.Chips/`).
- [ ] Dla PET: `MT6520`, `MOS6522`, opcjonalnie `MT6545`.
- [ ] Brać razem z plikiem `ChipSnapshots.cs` dla danego chipa i jego testami
  z `tests/PetEmulator.Chips.Tests/`.
- [ ] NIE brać na start: `FD1791/FD1793`, `Z80SIO/Z80PIO`, `MC146818`,
  `MOS6551/MOS6702`, `I8272`, `AY38910`. Dyskietki/serial/RTC to M5+.

### 1.4 Maszyna — brać jedną jako szablon kształtu, nie kopiować obu

- [ ] Wzorzec `StepInstruction/Reset/BusObserver/snapshot`: albo
  `src/PetEmulator.Pet/PetMachine.cs` albo
  `src/PetEmulator.Vic20/Vic20Machine.cs` (ten wybrany w M0).
- [ ] Razem z nim: jego `*MemoryBus`, `*MemoryMap`, `*KeyboardMatrix`,
  `*RomLoader`/`*RomManifest`, `Display/*RasterDisplay`, `Tape/*Datasette`
  (tylko LOAD; SAVE-capture z `Vic20Machine.CaptureSaveIfDispatched` to M3).
- [ ] Dla VIC-20 dodatkowo: `Vic20Cartridge.cs` +
  `lib/PetEmulator.Vic20.Cartridge.Abstractions/` (pluginy RAM/RTC wracają w M5).
- [ ] NIE brać: drugiej maszyny, SuperPET (`M6809Cpu`, `SuperPet6809MemoryBus`,
  `MOS6702`), IEEE-488/IEC drive (`PetIeeeBus`, `D64Image` — wracają w M5).

### 1.5 Narzędzia — brać mały zestaw

- [ ] `lib/PetEmulator.Debugger/MachineDebugger.cs` + jedna sesja CLI
  (`PetDebuggerSession` albo `Vic20DebuggerSession`) jako narzędzie
  `trace/watch/dump`.
- [ ] `lib/PetEmulator.DebugApi/DebugApiHost.cs` (peek bez efektów ubocznych).
- [ ] `tests/PetEmulator.TestSupport/` — buildery maszyny/busa do testów.
- [ ] `tests/coverage.runsettings` + `scripts/test-chips-coverage.sh` (wzorzec).
- [ ] `lib/PetEmulator.Screenshot/` (headless Avalonia.Skia) do regresji ekranu.
- [ ] `lib/PetEmulator.Audio/AudioContracts.cs` + `NullAudioOutput` (kontrakt,
  nie synteza — prawdziwy dźwięk to M5).
- [ ] NIE brać na start: pełnego `PetEmulator.Desktop` (wraca okrojony w M3,
  jeden ViewModel maszyny), pełnego CLI (`apps/run/debug` wystarczą 3 komendy).

### 1.6 Dokumentacja — brać kontrakt, nie całość

- [ ] `docs/architecture/overview.md` (model składania maszyny).
- [ ] `docs/chips/<wybrane chipy>.md` + `docs/chips/README.md` (kryteria).
- [ ] Mapa portów i profil wybranej maszyny (`docs/pet/io-ports.md` albo
  `docs/vic20/io-ports.md`).
- [ ] NIE brać: `docs/scan/` (indeks poprzednich iteracji), planów CPC/Kaypro/TRS-80.

## 2. Architektura minimalizująca impakt skali

1. **Jedna maszyna w repo.** Druga maszyna = nowe repo albo jawny milestone
   M6 z własnym `MemoryBus` + profil. Zakaz „wspólnych" klas maszyn
   (`CpcMachine`-style shared base) — personal-004 pokazuje, że wspólny
   kod maszyn gnije w `Cpc/Cpc464/Cpc6128` szybciej niż Core.
2. **Kierunek zależności jeden:** `Maszyna → Chips/Core/CPU`, `UI/CLI → Maszyna`
   przez `IMachine`/`IProcessor`/`IMemoryBus`. Zakaz referencji
   `Chips → Maszyna`, `Core → Maszyna`, `ViewModel → chip konkretny`
   (tylko `IDeviceStatus`/snapshoty).
3. **CPU czysty:** zna `IMemoryBus + IClock`, nie zna PET/VIC/ekranu.
   Różnice wariantów tylko przez `OpcodeTable.DeriveWith()` + `CpuBehavior`,
   zakaz `if (variant == ...)` w handlerach.
4. **Timing jawny:** `StepInstruction()` robi `cpu.StepInstruction()` →
   `Tick(cycles)` urządzeń → `SetIRQ/SetNMI`. Dla 6522-czułych maszyn
   (VIC-20) tick per-cykl przez `CycleElapsed`, nie hurtem — lekcja
   z `Vic20Machine.cs:89-104`.
5. **Profil jako dane:** ROM-set, RAM, I/O, layout klawiatury, geometria
   ekranu w `*Profile`/`*DisplayConfig`, nie w `ifach` maszyny.
6. **Snapshot od dnia 1:** `IMachineStateStore<T>` dla CPU+pamięć+chipy+zegar+
   pending IRQ/NMI. Każdy nowy stan musi mieć round-trip test, inaczej
   rośnie jak w personal-004 (SuperPET jawnie `NotSupported` do dziś).
7. **Anty-dryf:** nowy kod współdzielony trafia do Core tylko gdy mają go
   dwaj konsumenci; wcześniej mieszka w maszynie. Decyzję zapisuj w
   `docs/architecture/` (1 strona: kontekst → decyzja → konsekwencje).

## 3. Konfiguracja OpenCode pod minimalny impakt

Cel: agent domyślnie czyta i planuje, pisze tylko w wyznaczonym zakresie.

```jsonc
// opencode.json — nowa iteracja (zweryfikowane z https://opencode.ai/docs/config i /skills)
{
  "$schema": "https://opencode.ai/config.json",
  "model": "opencode-go/muse-spark-1.3-contributor",
  "lsp": true,
  "permission": {
    // twarde bariery skali: brak destrukcji historii i brak sieci w kodzie
    "deny": ["bash:git push*", "bash:git * --force*", "bash:rm -rf *"],
    "allow": [
      "read:*",
      "bash:dotnet build * --no-restore",
      "bash:dotnet test * --no-restore --filter *"
    ],
    "ask": ["write:*", "edit:*", "bash:dotnet run *"],
    // skills: brak klucza `skills.paths` w schemacie — discovery jest automatyczne.
    // `.claude/skills` ładuje się SAMO jako ścieżka Claude-compatible, więc jedyny
    // udokumentowany sposób odcięcia to deny-pattern na nazwy skilli:
    "skill": {
      "*": "allow",
      "gitnexus*": "deny"
    }
  },
  "formatter": { "dotnet": ["dotnet", "format", "--no-restore"] }
}
```

### Skills — tylko natywne, zero `.claude/skills`

- [ ] Własne skille wyłącznie jako `.opencode/skills/<nazwa>/SKILL.md`
  (nazwa `^[a-z0-9]+(-[a-z0-9]+)*$`, zgodna z katalogiem; frontmatter `name`
  - `description` 1–1024 znaki; `SKILL.md` wielkimi literami).
- [ ] NIE używać `.claude/skills/` ani `.agents/skills/` — w nowej iteracji
  nie ma GitNexusa ani zależności Claude; `permission.skill.gitnexus* = deny`
  odcina wszystkie 7 skilli gitnexus-* z personal-004.
- [ ] Na start wystarczą 2 skille: `dotnet-check` (build + szybkie testy
  - bramka review z sekcji 4) i `machine-verify` (boot ROM + snapshot
  round-trip + screenshot). Każdy nowy skill wymaga wpisu „kiedy używać"
  w opisie, inaczej agent go nie wybierze.
- [ ] Weryfikacja: `opencode debug config` pokazuje resolved config
  (bez błędów schematu), a nieużywany skill nie pojawia się
  w `<available_skills>` opisu narzędzia `skill`.

- [ ] `AGENTS.md` na root: 1 strona — komendy build/test (z filtrem
  `TestCategory!=BinaryBoot`), układ `Views/ViewModels/Services`,
  reguła docs (`docs/` indeksem, plany w `docs/plans/`, decyzje
  w `docs/architecture/`). Bez kopiowania sekcji GitNexus, jeśli nowa
  iteracja nie używa GitNexus.
- [ ] Zagnieżdżony `src/<Maszyna>/AGENTS.md` (2-3 linijki): mapa adresów,
  profil ROM, co jest poza zakresem (np. „dyskietki nieautoryzowane do M5").
- [ ] `.opencode/command/` — 3 slash-komendy i koniec: `/check` (build +
  szybkie testy), `/verify-machine` (boot ROM + snapshot round-trip),
  `/add-scope` (musi dopisać wpis do sekcji 7 tego pliku, inaczej odrzuć).
- [ ] Tryb pracy: agent najpierw `plan` (lista plików do dotknięcia +
  blast radius), potem implementacja. Limit: max ~5 plików produkcyjnych
  na jeden task; więcej = podzielić milestone.

## 4. Sprawdzanie kodu (review gate)

Każdy PR/task przechodzi tę bramkę w tej kolejności:

- [ ] **Gate 0 — zakres:** `git status --short` pokazuje tylko pliki
  z planu taska. Plik spoza allowlist = stop.
- [ ] **Gate 1 — build:** `dotnet build <slnx> --no-restore`
  (`TreatWarningsAsErrors=true` — zero warningów).
- [ ] **Gate 2 — testy szybkie:**
  `dotnet test <slnx> --no-restore --filter "TestCategory!=BinaryBoot"`.
- [ ] **Gate 3 — blast radius:** przed edycją symbolu impact-analysis
  (upstream callers), po zmianie `detect_changes()` vs `main`; ryzyko
  HIGH/CRITICAL = stop i decyzja człowieka.
- [ ] **Gate 4 — konwencje:** nullable + `ImplicitUsings` bez aliasów,
  Avalonia tylko w `Views/Services`, ViewModele na interfejsach
  (`IFilePickerService`), XAML z `x:DataType`, kolory z `App.axaml`
  przez `DynamicResource`.
- [ ] **Gate 5 — docs:** zmiana kontraktu chipa/busa = update
  `docs/chips/<chip>.md` lub mapy portów; zmiana decyzji = wpis
  w `docs/architecture/`. Bez docs PR nie wchodzi.

## 5. Moduły funkcjonalności anty-dryfowe

Szablon każdego modułu (urządzenie, tape, cartridge, ekran, debugger):

```text
<Moduł>/
  <Moduł>.cs            # kontrakt + implementacja, IMemoryMappedDevice lub usługa
  <Moduł>Snapshot.cs    # stan do Capture/Restore (od dnia 1)
  <Moduł>Profile.cs     # dane konfiguracyjne, zero logiki (gdy dotyczy)
tests/.../<Moduł>Tests.cs
docs/<maszyna|chips>/<moduł>.md  # kontrakt + ograniczenia, nie kopia checklisty
```

- [ ] Reguły: jeden moduł = jeden katalog; brak cykli zależności
  (sprawdzać `dotnet build` + review importów); `Snapshot` obowiązkowy;
  każdy `Tick(cycles)` deterministyczny (zero `DateTime.Now` w logice);
  RAM po `Reset` zerowany dla powtarzalności bootów.
- [ ] Urządzenia zewnętrzne (dysk/taśma/kartridż) za interfejsem
  (`ISerialTransport`, `IVic20ExpansionDevice`-style) + fake w `TestSupport`.
- [ ] UI doklejane ostatnie: `IMachineViewModel` per maszyna, moduły UI
  (`HexEditor`, `OpcodeStepper`) jako `IShellModule`, zero logiki maszyny
  w code-behind.

## 6. Testy — jak pilnować i projektować

Piramida (kolejność pisania = kolejność warstw):

1. [ ] **Kontrakt/tabela:** `OpcodeTableTests` — 256 wpisów, podział
   official/undocumented/undefined, timing bazowy.
2. [ ] **Instrukcje:** rejestry/flagad/mody adresowania, zapis do busa
   (fake bus z `TestSupport`, nie prawdziwa maszyna).
3. [ ] **Timing:** page-cross, RMW double-write, IRQ/NMI, `Tick(cycles)`.
4. [ ] **Bus/maszyna:** mapowanie RAM/ROM/I/O, wektor reset, `BusObserver`
   widzi realne dostępy (lekcja z `PetMachine.BusObserver` vs 6809-view).
5. [ ] **Integracja:** boot prawdziwego ROM (Klaus Dormann / KERNAL),
   klawiatura przez macierz, ekran (screenshot headless, porównanie ramek),
   snapshot round-trip, tape LOAD.
6. [ ] **ROM/długie:** `TestCategory=BinaryBoot`, tylko jawnie:
   `dotnet test <slnx> --filter "TestCategory=BinaryBoot"`.

- [ ] Konwencje: nazwa `Metoda_Warunek_Oczekiwane`; jeden assert logiczny
  per test (FluentAssertions); dane brzegowe jako `[TestCase]`, nie kopie testów.
- [ ] Coverage: `coverlet` + `coverage.runsettings`; bramka 100% line/branch
  tylko dla chipów Tier-1 w M2; dla reszty bramka = „każda gałąź ma test
  zachowania na busie/maszynie", nie procent.
- [ ] Mutacja lekka: przy nowym handlerze/chipie ręcznie usunąć jedną gałąź
  i potwierdzić, że test pada (zapis w PR, 1 zdanie).

## 7. Kamienie milowe (exit criteria, nie daty)

- [ ] **M0 — start (ten plik):** wybór maszyny + allowlist importu
  zamrożone. Exit: ten plik zacommitowany, `docs/README.md` wskazuje go jako plan.
- [ ] **M1 — rdzeń chodzi:** CPU NMOS + bus + RAM/ROM + reset vector +
  `StepInstruction/Run` + Klaus non-BCD zielony. Exit: boot do promptu
  bez urządzeń, testy warstw 1-4 zielone.
- [ ] **M2 — maszyna minimalna:** chipy Tier-1 + klawiatura (macierz) +
  raster display + snapshot round-trip + `MachineDebugger` trace/watch/dump.
  Exit: skrypt CLI `boot+type+dump` przechodzi, screenshot ramki w repo.
- [ ] **M3 — interakcja:** Desktop z jednym ViewModelem maszyny, file picker
  (ROM/tape), `PollDiskActivity`-style LED/activity, tape LOAD.
  Exit: ręczny scenariusz boot → LOAD → RUN bez debuggera.
- [ ] **M4 — warianty CPU:** 65C02/6510/2A03 jako `DeriveWith()` + testy tabel
  i timingów, zero `if (variant)` w handlerach. Exit: regresja M1-M3 nietknięta.
- [ ] **M5 — media (dopiero tu):** D64/IEC lub kartridż-pluginy + SAVE round-trip
  - audio kontrakt (nie synteza). Exit: każdy nośnik ma test integracyjny.
- [ ] **M6 (opcjonalnie) — druga maszyna:** tylko po domknięciu M0-M5;
  wymaga nowego wpisu w tym pliku i własnego `MemoryBus`/profilu.
  Bez tego — odrzucić.

## 8. Ryzyka przeniesione z personal-004 (nie powtarzać)

- Pętla cyklowa bez capa → hang niewidoczny w unit testach, widoczny na
  KERNAL/BASIC (`overview.md:238-250`). Poprawka w 1.2 jest wymagana, nie opcjonalna.
- Sklejanie VIA1/VIA2 do jednej linii IRQ (VIC-20 wymaga NMI dla VIA1).
- Klawiatura „działa, ale nic nie pisze" = zły VIA/port w `PortBWritten`
  (lekcja z `Vic20Machine.cs:114-127`).
- `BusObserver` podpięty do złego busa przy wielu view (lekcja SuperPET).
- Snapshoty dopisywane na końcu = `NotSupported` na lata. Snapshot w szablonie
  modułu (sekcja 5) od pierwszego PR.

## 9. Zakres debugowania VIC-20/PET (optimum dla nowej iteracji)

Wycięte z personal-004: bierzemy cienką warstwę CPU-agnostyczną + jedną
sesję maszynową. Wszystko, co wymaga nieistniejącego kontraktu (ekran, 6809,
dysk), wraca z milestone'em, który ten kontrakt wprowadza.

### 9.1 Warstwa Core — brać w całości (CPU-agnostyczna, zero kosztu)

- [ ] `MachineDebugger` (`lib/PetEmulator.Debugger/`): `trace`,
  `break-cycle`, `break-instruction-count`, `watch`, `watch-range`,
  `unwatch`, `dump`. Jeden prymityw kroku (`IMachine.StepInstruction()`),
  osobna komenda `run` celowo zwinięta w `trace` — nie odtwarzać.
- [ ] `break-pc` tylko za `IDebuggableProcessor` (błąd zamiast cichego no-opa
  dla zwykłego `IProcessor`). 6502 implementuje `GetRegisters()`
  (`PC/A/X/Y/SP/P` + `EightBitRegisterNames`) — nic więcej nie potrzeba.
- [ ] `BusAccess` observer na busie (`Action<BusAccess>?`, null = zero kosztu
  na hot path). Podstawa `trace-log` i `InstructionTracer`; przy wielu view
  pamięci observer MUSI być przepinany na aktywny bus (lekcja SuperPET).
- [ ] `DebugApiHost`: tylko `GET /status`, `POST|GET /step`, `POST /trace`
  (próbkowany: `steps/maxSamples`, cap 100k/500), `GET|POST /memory` (cap
  64KB), `POST /reset`. Loopback-only + jeden `Lock` + `Task.Run` (brak
  wątku UI). Endpointy bez backing-konceptu (`/snapshot`, `/screen.png`,
  `/key`, `/tape`, `/disk`, `/fdc`, `/frames`) NIE — wracają w M3/M5.
- [ ] Konwencja błędów: nigdy throw do REPL/HTTP — string `error: ...`
  - exit code / status 400. Jeden zły wiersz skryptu nie zatrzymuje reszty.

### 9.2 Sesja maszynowa — jedna (PET albo VIC-20, ta wybrana w M0)

- [ ] Setup: `profile` (tylko PET), `roms`, `keymap` (tylko PET),
  `tape`/`play`/`stop`/`eject`, `key`, `type`, `devices`, `status`.
- [ ] Obserwacja: `trace-log <n> <plik>` (`InstructionTracer` per instrukcja
  → plik, nie stdout) + `devices` (dynamiczna lista `IDeviceStatus` na ikony
  GUI) + `status` (gotowość, liczniki, PC).
- [ ] Diagnostyka generyczna: `via-irq-check`-style (wejście: budżet
  instrukcji; wyjście: licznik IRQ/NMI + kroki do pierwszego przerwania).
  Wzorzec `disk-stall-check` (postęp transferu jako sygnał stall) wraca w M5.
- [ ] NIE brać: `superpet-diagnose` / `superpet-boot-checkpoints` (brak 6809
  do M6), `cartridge-plugin` (M5), `new-disk`/`disk` (M5), `joystick`
  (dopiero z `Vic20Joystick` w M2-M3, nie wcześniej).

### 9.3 Jak pobierany jest stan (reguła odczytu)

| Co | Jak | Zakaz |
| --- | --- | --- |
| Rejestry | `IDebuggableProcessor.GetRegisters()` po nazwie; szerokość z `EightBitRegisterNames`, nie z wartości | UI/API nigdy nie dotyka pól CPU |
| Pamięć | `IMemoryBus.Read` (peek; efekty uboczne są sprawą busa, nie debuggera); `dump`/`/memory` tylko tędy | brak bezpośredniego dostępu do tablic RAM/ROM |
| Chipy | typowane property maszyny (`Via`, `Pia1`, `Crtc` / `Vic`, `Via1`, `Via2`) — stan timerów/IRQ do diagnostyki | ViewModel nie trzyma referencji do wnętrza chipa |
| Snapshot | `IMachineStateStore<T>`: CPU + pamięć + chipy + klawiatura + datasette + liczniki (`Pia1Cb1Phase`, `IeeeByteCount`/`LastIrqVector`); `Version=1`, restore waliduje fingerprint profilu | snapshot bez testu round-trip nie istnieje |
| Trace magistrali | `BusObserver` (`IsWrite/Address/Value`); `trace-log` konsumuje per instrukcja | observer nie zmienia zachowania busa |
| Liczniki/postęp | `IProcessor.Halted/CycleCount/InstructionCount`; HTTP serializuje jednym `Lock` | brak PC/rejestrów w próbkach `/trace` (kontrakt CPU-agnostyczny) |

### 9.4 Desktop-debug — okrojony, w M3 (nie wcześniej)

- [ ] Brać: `HexEditorViewModel` (peek/poke przez `IMemoryBus`) + pola statusu
  maszyny + LED aktywności przez `Poll*-style` latch (nie subskrypcja eventów).
- [ ] `OpcodeStepperViewModel` tylko jako narzędzie deweloperskie M1 (goły CPU
  na `FlatMemoryBus`, jeden wariant NMOS) — nie część shipping UI.
- [ ] NIE brać: `ChipTester`, `MediaTester`, `FontViewer`,
  `Vic20ProgramProfileSelector` (wracają, gdy dany feature wchodzi do milestone'a).

## 10. Playbook śledztwa chipowego (procedura obowiązkowa)

Każde podejrzenie „chip jest niedokładny" przechodzi kroki 10.1–10.7 w tej
kolejności. Pominięcie kroku wymaga jednego zdania uzasadnienia w raporcie.
Wzorzec kanoniczny: `docs/vic20/via-nmi-irq-diagnosis.md` (od zgłoszenia
„gry nie działają" do dwóch realnych fixów + infrastruktury testowej).

### 10.1 Krok 0 — reprodukcja jako skrypt `.dbg` (zanim cokolwiek dotkniesz)

- [ ] Scenariusz zapisany jako plik skryptu sesji debuggera
  (`<maszyna>-debug <skrypt>`), headless, bez GUI i ręcznych klawiszy.
- [ ] Szablon (VIC-20; dla PET analogicznie z `profile`/`keymap`):
  `roms` → `trace 200000` (init KERNALa ~130–175k instrukcji, wcześniej
  maszyna nic nie słyszy) → `type` + osobny Enter jako
  `key <row> <col> down` / `trace 15000` (kilka pełnych skanów klawiatury)
  / `key <row> <col> up` → `trace` z budżetem na realną operację
  (IEC LOAD ~1KB to miliony instrukcji) → `watch`/`dump`/`status`.
- [ ] Pacing typera (`holdInstructions`/`gapInstructions`) świadomie dobrany —
  za mały gubi znaki w nazwie pliku i udaje bug taśmy/chipa.
- [ ] Skrypt ląduje w `scripts/` i wchodzi do CI jako smoke test (wzorzec:
  `scripts/vic20-boot.dbg` + `test-vic20-boot.sh`).

### 10.2 Krok 1 — rozstrzygnij: wiring czy chip (zanim otworzysz chip)

- [ ] `watch` na rejestrze **flagi** przerwania (`$911D`/`$912D` dla VIA),
  potem `trace`. Flaga się ustawia, a CPU nie reaguje → routing IRQ/NMI
  w maszynie, NIE chip (precedens: VIA1 wpięte w IRQ zamiast NMI).
- [ ] Klawiatura „działa, ale nic nie pisze" → sprawdź parę VIA/port
  w `PortBWritten` przeciw disassembly KERNALa, nie przeciw chipowi VIA.
- [ ] Dopiero gdy flaga się NIE ustawia (a powinna) albo timing dostępu
  jest zły — przejdź do kroku 10.3.

### 10.3 Krok 2 — izolacja warstwami (Layer 0→3)

- [ ] **Layer 0** (chip sam, fake bus): dekodowanie rejestrów, maskowanie
  bitów, reset, wrap liczników, timery. Tu rozstrzygasz zgodność z datasheet.
- [ ] **Layer 1** (chip przez prawdziwy `MemoryBus`, bez uruchamiania kodu):
  routing rejestrów, ROM write-drop, open bus, `Observer` na każdym dostępie.
  Tu łapiesz błędy mapowania adresów.
- [ ] **Layer 2** (prawdziwy ROM: banner KERNALa, `PRINT5` przez macierz):
  tu łapiesz błędy współdziałania. Bug widoczny dopiero tutaj to prawie
  zawsze timing/wiring, nie logika chipa.
- [ ] Reguła: nie przepisuj chipa, dopóki bug nie replikuje się na Layer 0/1.

### 10.4 Krok 3 — oracle zewnętrzny (nie „oko")

- [ ] Programy testowe pisane pod prawdziwy sprzęt (dla VIA: `via_pb7`,
  `via_t1irqack`/BANDITS-VIA1; dla CPU: Klaus Dormann). Wynik vs referencja
  sprzętowa, nie vs „wygląda OK".
- [ ] Źródła VICE w C jako rozstrzygająca specyfikacja
  (precedens: `vic20via1.c: "via->irq_line = IK_NMI"`).
- [ ] Jeśli logika chipa jest poprawna, a test pada → sprawdź **ziarnistość
  tickowania** zanim cokolwiek przepiszesz: hurtowy `Tick(cycles)` po
  instrukcji vs per-cykl przez `CycleElapsed` (precedens: regresje
  `via_pb7`/`via_t1irqack` miały tę jedną przyczynę).

### 10.5 Krok 4 — nie zatruwaj obserwacji pomiarem

- [ ] Flagi obserwuj przez `watch` (czyste odczyty bez side-effectów).
- [ ] `dump`/`/memory` tylko do pamięci bez side-effectów. Odczyt niektórych
  rejestrów (np. `MOS6522.ReadTimer1Low` — T1 low) **czyści własną flagę
  przerwania**: `dump` wstawiony między odczyty konsumuje flagę, którą
  następna linia skryptu chciała zaobserwować.
- [ ] Podejrzany odczyt rejestru → sprawdź w kodzie chipa, czy `Read*` ma
  efekt uboczny, zanim wyciągniesz wniosek ze skryptu.

### 10.6 Krok 5 — raport z werdyktem (każde śledztwo, też negatywne)

- [ ] Tabela: znalezisko → werdykt (**real bug** / misread — zachowanie
  poprawne źle odczytane / documented gap — luka świadomie niezałatana)
  → status (fix + commit / test + plik / odłożone z powodem).
- [ ] „Nie-bugi" dokumentuj tak samo (precedens: Alphoids freeze, rejestry
  VIC `$9000/$9001`) — chronią przed ponownym śledztwem.
- [ ] Każdy real bug dostaje test regresyjny na najniższej warstwie, na
  której się replikuje, + wpis w `docs/chips/<chip>.md`.

### 10.7 Krok 6 — kryterium stopu (kiedy chip jest „dosyć dokładny")

- [ ] Layer 0 zielony + Layer 1 routing + Layer 2 boot z prawdziwego ROM.
- [ ] Odpowiedni program testowy pod prawdziwy sprzęt przechodzi.
- [ ] Coverage brancha 100% **w parze** z testem zachowania na busie/maszynie
  (sam procent to formalność) + ręczna mutacja jednej gałęzi kładzie test.
- [ ] Niespełnione → luka trafia do `docs/chips/<chip>.md` jako jawny
  documented gap z powodem, nie ciche TODO w kodzie.

## 11. Klawiatura PC AT → matryca (prawidłowe podejście)

Jeden potok pozycyjny, zero słowników-stringów, test kompletności per maszyna.
Precedensy z personal-004: `AtKeyboardKey` zadeklarowany w kolejności wierszy
QWERTY (arytmetyka `A + offset` i zakres `A..Z` kłamały — „V pisało `,`,
16 liter martwych"), kopiowana tabela z cpu-vibe-001 nie zgadzała się
z prawdziwym skanem KERNALa, Enter wymagał dwóch rund weryfikacji
(`(3,7)` to CRSR-DOWN — kursor się rusza, ale linia się nie wykonuje;
dopiero `(1,7)` wykonuje — sam wskaźnik linii nie rozstrzyga).

### 11.1 Potok (trzy warstwy, jeden kierunek)

```text
Avalonia Key ──(1 host)──▶ AtKeyboardKey ──(2 profil)──▶ (row,col) ──(3)──▶ matryca
```

- [ ] **(1) Host — jedna, wspólna:** `Avalonia Key → AtKeyboardKey` wyłącznie
  jawnym switchem per-klawisz. Zakaz arytmetyki na ordinalach i wywodzenia
  z nazw enumów (oba bugi już raz wystąpiły). Zamrozić + test „każdy `Key`
  albo mapuje, albo jest na liście jawnie ignorowanych".
- [ ] **(2) Profil — per maszyna:** `AtKeyboardKey → (row,col)?` w klasie
  profilu. `null` = klawisza nie ma w tym modelu (jawnie, nie zgadywane).
  Zabić stringowy słownik host-key (`"KeyA"`, `"Digit0"`): `IPetKeyboardMap`,
  ViewModele i sesje debuggera mówią `AtKeyboardKey` end-to-end, jedno
  tłumaczenie zamiast dwóch.
- [ ] **(3) Matryca — jedna, wspólna:** `Press(cell)/Release(cell)`, wiele
  komórek naraz (Shift+X to dwie komórki, nie modyfikator-hack).
  Klawiatura ekranowa wchodzi tym samym wejściem (komórki), nie bocznym kanałem.
- [ ] Pozycyjność udokumentowana raz: użytkownik z AZERTY naciska pozycję Q
  i dostaje komórkę Q. Mapowanie po znaku na ścieżce fizycznej zakazane
  (rozjeżdża się na ISO/layoutach — `OemBackslashIso` to ostrzeżenie).

### 11.2 Dwa potoki, nie jeden (nie mieszać)

- [ ] **Fizyczny** (11.1): klawisz → komórka. Nie zna znaków.
- [ ] **Tekstowy** (automatyzacja/skrypty): `char → komórka` (`Find`/`TextTyper`
  style). Nie zna klawiszy. Test: wpisz `PRINT5` przez prawdziwą macierz,
  odczytaj PETSCII z ekranu (Layer 2).
- [ ] Weryfikacja komórki jak w personal-004: boot do prawdziwego BASIC,
  wciśnij **każdą z 64 komórek osobno**, odczytaj co KERNAL echo — nie kopia
  z innego projektu, nie zgadywanie. Enter weryfikuj **efektem** (linia się
  wykonała), nie ruchem kursora.

### 11.3 Test kompletności (trzyma całość w ryzach)

- [ ] Per maszyna, test parametryzowany po `AtKeyboardMapping.AllKeys`:
  każdy klucz albo mapuje do komórki zweryfikowanej empirycznie (11.2),
  albo jest na liście jawnie niemapowalnych **z powodem**.
- [ ] Nowy enum bez wpisu = czerwony test, nie cichy null.
- [ ] Zakaz zmiany kolejności `AtKeyboardKey` na alfabetyczną „dla elegancji":
  zmiana ordinali rozwala ukryte zależności; kolejność zostaje, test pilnuje.
- [ ] Mapy to dane profilu (klasa + test, bez Avalonii) — nie XAML, nie code-behind.

## 12. Renderowanie i glify (jak nie powielić błędów)

Łańcuch referencyjny z personal-004 (zweryfikowany): plik `.bin`
(8 B/glif, MSB-first) → `PetCharacterRomLoader` (walidacja wielokrotności 8)
→ `BitmapFont.GetGlyphRow` (bounds-safe, 0 poza zakresem) → rasteryzer
(screen RAM → ARGB8888 w buforze caller-allocated) → `EmulatorScreenControl`
(`WriteableBitmap` Bgra8888, two-pass scaling, PAR, letterbox) → ViewModel
tick (`Run(20000)` + `Tick` + `Render` + `UpdateFrame`). Rasteryzery bez
zależności UI — testowalne z prawdziwym ROM.

### 12.1 Jedno źródło glifów per maszyna + test parowania

- [ ] Wybrać JEDEN model i go nie mieszać: albo font wstrzyknięty
  (`IGlyphFont` z pliku, jak PET), albo odczyt z szyny (`charAddr`, jak VIC).
  Model „z szyny" wymaga asercji/ostrzeżenia przy pustym regionie znaków —
  inaczej ekran to **ciche śmieci zamiast błędu** (tak było w VIC-demo).
- [ ] Profil wskazuje plik fontu (`CharacterRomPath`-style); test parowania:
  profil z kodowaniem wymagającym banku N (`0x100+`-style) MUSI wskazywać
  plik o rozmiarze mieszczącym bank (4 KB, nie 2 KB). Bounds-safe `GetGlyphRow`
  zamienia brak banku w puste komórki — test ma to łapać, nie oko.
- [ ] Glob discovery fontów (`characters-*.bin`-style) MUSI pokrywać wszystkie
  nazwy używane przez profile — precedens: plik z kropką
  (`characters.901640-01.bin`) istniał i działał w ViewModelu, ale FontViewer
  go nigdy nie pokazał. Test: każdy `CharacterRomPath` z definicji profili
  pojawia się w wyniku discovery.

### 12.2 Determinizm i wydajność renderera

- [ ] Blink kursora licznikiem ramek, nie zegarem ściennym (30 klatek ≈ 1–2 Hz
  przy 50–60 fps) — `Tick()` raz na wyświetloną klatkę, nie w `Render()`
  (re-render przy resize nie może przesuwać fazy).
- [ ] Bufor ramki caller-allocated, reuse co klatkę, zero alokacji w `Render()`.
  Precedens do naprawy: `RenderTargetBitmap` alokowany co klatkę w kontroli
  Avalonii — cache'ować przy zmianie rozmiaru.
- [ ] Kolory fosforu/tła w zasobach/w profilu, nie zahardkodowane w rendererze.
- [ ] PAR (proporcje piksela) w profilu; test: 40 i 80 kolumn lądują na tym
  samym aspekcie, nie spłaszczone.

### 12.3 Regresja ekranu od dnia M2

- [ ] Screenshot headless (Avalonia.Skia, bez display OS) po boocie do promptu:
  banner czytelny (OCR z `TestSupport`-style lub hash ramki), geometria
  zgodna z profilem, brak śmieci w regionie znaków.
- [ ] Każdy fix renderera dostaje parę przed/po (ramka referencyjna w repo).

## 13. Zegar CPU i pacing wall-clock (warstwa, której brakowało)

Stan z personal-004: czas emulowany deterministyczny (`IClock`/`EmulationClock`,
`MachineClock` ze snapshotowalną resztą, timing urządzeń w cyklach —
`Pia1Cb1PulsePeriodCycles`, `ClockMHz` kasety), ale częstotliwość w Hz nie
istnieje jako stała maszyny, a UI jedzie stałym budżetem `Run(20_000)`
instrukcji co 20 ms **bez** pacingu do czasu rzeczywistego (świadoma decyzja
z komentarzem w kodzie). Skutek: tempo płynie z hostem, instrukcje to nie
cykle (2–7× rozjazd vs 1 MHz), dźwięk/taśma odczują pierwsze.

### 13.1 Częstotliwość jako dane profilu

- [ ] `ClockHz` w profilu maszyny (PET 1 MHz, VIC-20 NTSC/PAL osobno,
  drugi profil = druga wartość). Jedno miejsce, nie komentarze przy stałych.
- [ ] Wszystkie stałe czasowe maszyny wyprowadzone z `ClockHz`
  (`Pia1Cb1`-style: `ClockHz / 60`), nie zahardkodowane liczby z komentarzem.
- [ ] Budżet ticka UI w **cyklach** (`ClockHz × interval`), nie w instrukcjach.
  Krok instrukcji kończy się w połowie rozliczenia cykli — budżet instrukcyjny
  tego nie widzi.

### 13.2 Throttle (hamowanie, nigdy doganianie)

- [ ] Limiter w hoście (UI/CLI), nie w maszynie: zmierz czas slice'a, dośpij
  resztę do `interval`. Maszyna zostaje deterministyczna i bez zegara ściennego.
- [ ] Przełącznik: throttled (granie, domyślnie) vs full-speed (debugowanie,
  batch, testy). Bez adaptacyjnego „doganiania" opóźnień — tylko hamowanie.
- [ ] Test: N ticków × budżet = dokładnie N × `ClockHz × interval` cykli na
  `CycleCount`, niezależnie od hosta. Test throttla z wstrzykniętym zegarem
  (fake time), nie ze `Thread.Sleep`.

## 14. Aspekty odroczone i jawne nie-cele (nic nie ginie, nic nie pęcznieje M0–M2)

Każdy punkt ma milestone docelowy albo status nie-cel z powodem. Dopisanie
punktu do milestone'a wymaga testu akceptacyjnego w tym samym PR.

- [ ] **Kradzież cykli / DMA → M2 (kontrakt), użycie w M5.** Model „urządzenie
  wstrzymuje CPU" (VIC badlines, FDC) zmienia `StepInstruction` (CPU czeka,
  urządzenia idą). Kontrakt istnieje w Core (`IWaitLine`) — podpiąć w M2 jako
  no-op z testem, pierwsze użycie przy dysku w M5. Dlaczego nie później:
  retrofit wstrzymywania CPU po fakcie łamie wszystkie testy timingu.
- [ ] **Semantyka magistrali → M1 (checklista).** Mirroring, open bus (wartość
  odczytu znikąd), write-drop do ROM, kolejność dekodowania: każdy adres
  0–64K ma zdefiniowane zachowanie + test. Dlaczego w M1: błędy tu udają bugi
  każdego chipa z osobna (patrz 10.2).
- [ ] **RAM po power-on → M0 (decyzja w profilu).** Zero (determinizm, jak
  personal-004) vs losowe z ziarnem (wierność — loaderzy/protekcje wykrywają
  losowość). Jawny wybór per profil, nie domyślny. Dlaczego w M0: zmienia
  wszystkie snapshoty i testy bootu.
- [ ] **NTSC/PAL → M2 (wariant profilu).** Nie tylko `ClockHz`: cykle linii,
  raster, dźwięk. Profil niesie wariant video w całości; test per wariant
  (boot + geometria + zegar). Dlaczego nie jeden wariant „na później": drugi
  wariant dopisany po fakcie rozjeżdża stałe czasowe z 13.1.
- [ ] **ROM-y: licencje i weryfikacja → M0.** Skąd wolno pobrać (źródło + licencja),
  SHA w manifeście, komunikat przy braku/niezgodności, tryb bez ROM do testów
  jednostkowych. Dlaczego w M0: personal-004 trzyma ROM-y bez widocznej polityki.
- [ ] **Formaty plików → M5 (moduły z testami).** Parsory `.tap`/`.d64`/`.crt`/`.prg`
  jako osobne moduły (szablon z sekcji 5): round-trip + uszkodzone pliki
  (ucięte, złe checksumy) + fuzz jednym ziarnem. Dlaczego moduły, nie utilsy:
  formaty żyją dłużej niż maszyny.
- [ ] **Audio-synteza → szkielet w M3, dźwięk w M5.** Szkielet: model generatora
  per chip, sample-rate w profilu, test wektora próbek na sygnale testowym
  (nie na uchu). Dlaczego szkielet wcześnie: M5 bez API projektuje je od zera
  pod presją.
- [ ] **Peryferia (drukarka IEEE, ploter IEC, User Port, RS-232) → M6+ w tej
  kolejności**, bo każde następne wymaga poprzedniego transportu (User Port
  przed RS-232-bitbang, IEC przed drukarką). Do M6 lista zamknięta.
- [ ] **CI/release/ustawienia → M3.** Pipeline: build + szybkie testy na każdy
  PR, `BinaryBoot` nocny; wersjonowanie formatu snapshotów przy release
  (migracja albo twardy błąd, nigdy cicha niekompatybilność); persystencja
  ustawień UI (roms path, profil, głośność). Dlaczego M3: razem z pierwszym
  shipping UI, nie po nim.
- [ ] **Nie-cele (świadomie nigdy):** netplay, nagrywanie wideo z UI,
  edytor kodu maszynowego w emulatorze, porty na inne platformy niż desktop.
  Powód: każdy z nich to osobny produkt z własnym cyklem wydawniczym.

## 15. Wewnętrzna pętla debugowania (strategia na co dzień)

Uzupełnia sekcję 9 (narzędzia) i 10 (forensics chipowy): tam jest *czym*,
tu jest *jak na co dzień*. Kolejność kroków obowiązkowa; wyjście awaryjne
(15.6) zamiast kręcenia się w kółko.

### 15.1 Drzewo narzędzie ← objaw (po które sięgnąć najpierw)

| Objaw | Najpierw | Potem, jeśli mało |
| --- | --- | --- |
| Hang / nieskończony loop | `break-instruction-count` z budżetem + status liczników (stoi CPU czy urządzenia?) | bisekcja 15.4 |
| Zły piksel / śmieci na ekranie | `dump` video RAM + regionu znaków (10.5: tylko bez side-effectów) | screenshot vs ramka referencyjna (12.3) |
| Cichy brak IRQ / przerwanie nie dociera | `watch` na rejestrze flagi + `trace` (10.2: flaga vs routing) | `via-irq-check`-style diagnostyka |
| Zła wartość w pamięci | `watch` na adres + `trace` do pierwszej zmiany (kto pisze?) | `BusObserver`/`trace-log` na sprawcę |
| Bug tylko w długim boocie | snapshot-freeze 15.5, potem debuguj od zamrożenia | minimalizacja skryptu 15.3 |
| Pytanie „co ten kod ROM robi" | skill `rom-forensics`: grep listing → disasm regionu → trace | nigdy sam listing bez trace |

### 15.2 Failing-test-first (zakaz naprawy bez padającego testu)

- [ ] Najpierw minimalny test na **najniższej warstwie, na której bug się
  replikuje** (10.3: Layer 0 > Layer 1 > Layer 2). Czerwony przed fixem.
- [ ] Potem fix. Potem zieleń + pełna regresja warstwy.
- [ ] Fix bez testu = odrzucony w review (Gate 4, sekcja 4) bez dyskusji
  o meritum. Wyjątek: time-box 15.6 wyczerpany → documented gap, nie fix.

### 15.3 Minimalizacja reprodukcji (zanim zaczniesz czytać kod)

- [ ] Połówkowanie: wytnij połowę skryptu `.dbg` — bug zostaje w której połowie?
  Powtarzaj do <20 linii.
- [ ] Odejmowanie urządzeń: odłącz tape/disk/cartridge/joystick po kolei.
  Bug znika po odłączeniu X → sprawcą jest integracja z X, nie chip (10.2).
- [ ] Zamroź snapshot w najwcześniejszym punkcie, w którym bug już widać —
  dalsze debugowanie od zamrożenia (15.5), nie od pełnego boota.
- [ ] Rezultat: test regresyjny to ta zminimalizowana postać, nie oryginalny
  godzinny scenariusz.

### 15.4 Bisekcja w czasie (hangi miliono-instrukcyjne)

- [ ] `break-instruction-count` na końcu budżetu → działa? Połowa budżetu →
  działa? Bisekcja do okna <1000 instrukcji.
- [ ] W oknie: `trace` + `watch` na podejrzanych + `trace-log` do pliku.
- [ ] Podejrzenie pętli CPU bez capa (sekcja 1.2): cap ma zamienić hang
  w szybki błąd z adresem — bisekcja wtedy zwykle zbędna.

### 15.5 Snapshot-freeze (debugowanie deterministyczne)

- [ ] Snapshot tuż przed bugiem = punkt startu eksperymentów: restore, podepnij
  inną obserwację (`watch`/`BusObserver`), powtórz — bez przechodzenia boota.
- [ ] Jeden freeze, wiele hipotez: każda hipoteza to osobny przebieg od tego
  samego snapshotu. Wyniki porównywalne, bo start identyczny.
- [ ] Freeze ląduje w teście regresyjnym, gdy da się go zbudować programowo
  (stan startowy testu zamiast pliku binarnego).

### 15.6 Time-box i eskalacja (hamulec bezpieczeństwa)

- [ ] Limit: 2h śledztwa bez zawężenia do warstwy (10.3) → przerwij, zapisz
  hipotezy i wykonane kroki, odłóż na 24h albo poproś o drugą parę oczu.
- [ ] Limit: 1 dzień bez failing testu (15.2) → bug staje się documented gap
  z wpisem w `docs/chips/` lub mapie portów, nie wisi jako WIP.
- [ ] Po każdym zamkniętym śledztwie (fix czy gap): jedno zdanie do
  sekcji 8/10 — playbook rośnie z każdym bugiem, nie stoi w miejscu.

## 16. Avalonia: pętla, wątki i budżet klatki (sprawne renderowanie)

Stan z personal-004: `DispatcherTimer` 20 ms odpala `machine.Run(20_000)` +
`Render` + `UpdateFrame` **na wątku UI** — długa slice mrozi interfejs.
Ta sekcja to naprawia jako kontrakt M3, nie detal implementacyjny.

### 16.1 Model wątków (decyzja M3, jedna z dwóch)

- [ ] Wariant A (prosty, start M3): emulacja na dispatcherze, ale ze
  **strażnikiem budżetu** — mierz czas `Run+Render`; overrun powyżej
  2× `interval` trzy razy z rzędu → dynamiczne ścięcie budżetu instrukcji
  o połowę + wpis do logu (licznik jak `_tickErrorCount`-style). UI nigdy
  nie mrozi się na dłużej niż jeden overrun.
- [ ] Wariant B (docelowy, gdy A nie wyrabia): emulacja na workerze
  (dedykowany `Task`/wątek), gotowa ramka (`uint[]` z 12.2, reuse bufora)
  marshalowana na UI przez `Post` + `UpdateFrame` + `InvalidateVisual`.
  Maszyna nigdy nie dotykana z dwóch wątków naraz (jeden właściciel;
  debugger/API przez ten sam lock co `DebugApiHost` w 9.1).
- [ ] Wybór zapisany w `docs/architecture/` (1 strona) przed kodem M3.
  Zakaz hybrydy „trochę tu, trochę tu" — jeden właściciel maszyny.

### 16.2 Budżet klatki i degradacja (liczby, nie życzenia)

- [ ] Cel: 50 fps przy pełnym budżecie cykli z 13.1 (`ClockHz × interval`).
- [ ] Kolejność degradacji przy overrunach: (1) skip identycznych ramek
  (hash bufora — statyczny tekst nie płaci za upload), (2) ścięcie budżetu
  cykli (z logiem), (3) drop `Render` co drugą klatkę przy zachowaniu `Run`
  (emulacja idzie dalej, ekran klatkuje). Nigdy odwrotnie.
- [ ] Zero alokacji w pętli: cache prescale/`RenderTargetBitmap` przy stałym
  rozmiarze (precedens: alokacja co klatkę w `EmulatorScreenControl.Render`),
  `UpdateFrame` kopiuje tylko przy zmienionej ramce.
- [ ] Test: 600 ticków na workerze/headless z fake time — `CycleCount` zgodny
  z 13.2, liczba skipów/dropped raporowana, nie ukrywana.

### 16.3 Kontrakt code-behind i kontrolek

- [ ] Code-behind woła wyłącznie: `SetSize` (profil/font/resize),
  `UpdateFrame` (gotowa ramka), eventy wejścia → ViewModel. Zero logiki
  maszyny, zero `new` urządzeń, zero wczytywania ROM.
- [ ] `Views/Controls/` dostają dane push z `Tick` (ramka, status, LED-latch),
  nie ciągną z maszyny. Binding z `x:DataType`, kolory z `App.axaml`
  przez `DynamicResource` (reguły AGENTS.md jako checklist Gate 4).
- [ ] Podgląd XAML (previewer ładuje assembly z katalogu tymczasowego):
  kontrolki nie szukają ROM w konstruktorze — dane tylko przez metody,
  stan demo w design-time bez dotykania maszyny.

### 16.4 Wejście, fokus, brak ROM (UX, które nie crashuje)

- [ ] Jeden punkt wejścia klawiatury (okno): `KeyDown/KeyUp` → 11.1 →
  matryca. Fokus wraca na ekran po kliknięciu menu (jawny `Focus()` w handlerze
  menu). Key-repeat hosta ignorowany (matryca trzyma stan, nie eventy);
  `TextInput` nieużywane na ścieżce fizycznej (11.1: zakaz mapowania po znaku).
- [ ] Brak/niezgodność ROM: ekran zastępczy z czytelnym komunikatem
  (co brakuje + SHA oczekiwane z 14-M0 + gdzie położyć plik), nie wyjątek,
  nie puste okno. Test: profil bez ROM startuje UI i pokazuje komunikat.
- [ ] Ustawienia M3 (persystencja z 14): profil, roms path, throttle on/off,
  skala okna, głośność. Lista zamknięta — każda kolejna wymaga wpisu tutaj.

## 17. Audio: backend Windows, synteza napędzana cyklami

Stan z personal-004: kontrakt pull (`IAudioSource.Render(Span<AudioFrame>)`,
float32, `IAudioOutput` z wątkiem i zasadą nigdy-nie-rzuca) + synteza AY
offline do WAV — ale backend tylko PulseAudio (Windows rzuca
`PlatformNotSupportedException`), VIC i beeper CB2 w ogóle nie dekodowane,
a render nie jest powiązany z cyklami emulowanymi.

### 17.1 Backend per OS (M3, Windows first)

- [ ] WASAPI-sink dla Windows (platforma deweloperska) + istniejąca
  PulseAudio-gałąź; fabryka jak dziś (jedna gałąź per OS, throw by name
  dla reszty). Dopisać głośność/mute do listy ustawień M3 (16.4).
- [ ] Zasada nigdy-nie-rzuca obowiązuje każdy backend: brak serwera audio
  (CI, headless) = cisza + `LastError`, nie crash. Test: sink bez serwera
  startuje i stopuje bez wyjątku.

### 17.2 Synteza napędzana cyklami (nie zegarem ściany)

- [ ] Chip akumuluje cykle emulowane; próbek na tick =
  `cykle / (ClockHz / SampleRate)`, reszta w akumulatorze (ten sam wzorzec
  co `MachineClock` z sekcji 13). Throttle z 13.2 nie rozjeżdża fazy.
- [ ] Kolejność źródeł: VIC oscylatory → beeper CB2 → AY na żywo
  (offline-WAV z `AySoundDemo`-style zostaje jako narzędzie testowe).
- [ ] Szkielet w M3 (format w profilu + test wektora próbek na sygnale
  o znanej częstotliwości, tolerancja ±1 próbka); pełna synteza w M5.

### 17.3 Testy audio (bez „na ucho")

- [ ] Wektor-test per źródło: znany rejestr → znane próbki (tolerancja ±1).
- [ ] Test ciszy: reset chipa = same zera, nie szum.
- [ ] Test backendu bez serwera (17.1) + test przepełnienia bufora sinka
  (nadmiar próbek gubiony z licznikiem, nie crash ani deadlock).
