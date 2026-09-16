# Plan: snapshoty PET, VIC-20, TRS-80, Kaypro

**Odznaczaj każdy checkbox w tym pliku dopiero po tym, jak sam(a)
zweryfikujesz go w tej sesji (build/test/grep) — nie na podstawie tego, że
kod został napisany, ani na słowo poprzedniej sesji/subagenta. To jedyne
źródło prawdy o tym, co zrobione.**

## Zakres tej decyzji

To jest świadoma, wąska decyzja go dla **jednego** punktu Fazy 1 z
`2026-09-16-open-items.md` ("Pełne snapshoty PET, VIC-20, TRS-80, Kaypro"),
wzorem `CpcMachineSnapshot`/`Cpc464Snapshot`/`Cpc6128Snapshot`. **Nie**
otwiera reszty Fazy 1 — wspólne kontrakty urządzeń w Core
(`IKeyboardDevice`/`ICassetteDevice`/`IDiskController`/`IVideoDevice`),
systematyczny capability routing i `IAudioDevice` pozostają nieautoryzowane,
patrz `2026-09-16-cpc-architecture.md`.

## Fundament — już gotowy, zero nowej pracy

Zweryfikowane researchem w kodzie:

- `CpuProcessorBase<TState>.CaptureSnapshot()`/`RestoreSnapshot(CpuDebugSnapshot)`
  (`lib/PetEmulator.Core/Cpu/CpuProcessorBase.cs:59-67`) jest generyczne i
  już odziedziczone przez `Cpu6502Classic`, `Z80Cpu`, `M6800Cpu`,
  `M6809Cpu : M6800Cpu`, `Cpu8080` — **żadna z czterech maszyn nie
  potrzebuje nowego kodu CPU-snapshot**.
- `Z80Sio.CaptureState()/RestoreState()` i
  `Z80PioDevice.CaptureState()/RestoreState()` +
  `Z80PioInterruptChain.CaptureState()/RestoreState()`
  (`lib/PetEmulator.Chips/Z80SIO/Z80Sio.cs:463,500`,
  `lib/PetEmulator.Chips/Z80PIO/Z80PioDevice.cs:224,237`,
  `Z80PioInterruptChain.cs:12,15`) już istnieją — Kaypro (SIO+PIO) reużywa
  wprost.
- `MT6545.CaptureState()/RestoreState()` (`lib/PetEmulator.Chips/MT6545.cs`)
  już istnieje z pracy nad CPC — PET reużywa dla opcjonalnego CRTC.

## Kolejność i uzasadnienie — poprawione

**Pierwsza wersja tego planu szła od najmniejszego zakresu do
największego (TRS-80→Kaypro→VIC-20→PET) — to była zła kolejność.**
Kryterium "najtaniej najpierw" minimalizuje ryzyko *wykonania*, ale
maksymalizuje ryzyko *architektoniczne*: PET jako jedyny z czterech
wymusza na wspólnym kontrakcie (`MachineSnapshot`/`IMachineStateStore<T>`)
rzeczy, których TRS-80/Kaypro/VIC-20 w ogóle nie dotykają — walidację
zgodności profilu przy restore, wiele opcjonalnych (`nullable`) urządzeń
naraz, wieloustrojowy protokół magistrali (IEEE-488), świadome ucięcie
zakresu z rzuceniem wyjątku zamiast cichej utraty stanu. Gdyby PET poszedł
ostatni i któryś z tych wymogów okazał się niezgodny z kształtem
`MachineSnapshot`/`IMachineStateStore<T>` przyjętym wcześniej, trzeba by
przerabiać już zamknięte TRS-80/Kaypro/VIC-20 — dokładnie odwrotność tego,
po co jest wspólny kontrakt.

**Poprawiona kolejność: VIC-20 → PET → TRS-80 → Kaypro.**

1. **VIC-20 pierwszy** — średni zakres, pierwszy raz ćwiczy wzorzec
   "świadome cięcie zakresu + test wymuszający udokumentowane zachowanie"
   (kartridże poza v1) na czymś mniejszym niż PET, i produkuje
   `MOS6522.CaptureState`, potrzebny PET.
2. **PET zaraz potem, nie na końcu** — najtrudniejszy i architektonicznie
   najbardziej wymagający przypadek idzie wcześnie, dopóki kontrakt
   `MachineSnapshot`/`IMachineStateStore<T>` jest jeszcze "świeży" i tani
   do poprawienia, gdyby czegoś brakowało. Reużywa `MOS6522` z VIC-20 i
   gotowe `MT6545`.
3. **TRS-80 trzeci** — teraz niskiego ryzyka: kontrakt już sprawdzony na
   trudniejszym przypadku (PET), więc nullable-device i profile-validation
   wzorce z PET po prostu się stosuje, nie odkrywa na nowo. Produkuje
   `FD1791.CaptureState`.
4. **Kaypro ostatni** — najtańszy, korzysta z `FD1791` (TRS-80) i
   gotowych `Z80Sio`/`Z80PioDevice` snapshotów.

Jedyne twarde ograniczenia kolejności: VIC-20 przed PET (`MOS6522`),
TRS-80 przed Kaypro (`FD1791`) — oba spełnione.

---

## Zadanie 1 — VIC-20 (produkuje `MOS6522.CaptureState`, reużywany przez PET)

**Fakty:** `src/PetEmulator.Vic20/Vic20Machine.cs` — `Cpu6502Classic`
(darmowe), `Vic20MemoryBus` (`_zeroPageRam`, `_builtinRam` — płaskie, bez
rejestru banków; `Vic20ExpansionDeviceRegistry _expansions` —
polimorficzne sloty, **poza zakresem v1**, patrz niżej), `MOS6560 _vic`
(chip wideo+audio: `_registers: byte[RegisterCount]`, `_rasterCounter:
int`, `_audioPhases: double[4]`, `_noiseLfsr: ushort` — bez
`CaptureState`), `MOS6522 _via1, _via2` (bez `CaptureState`; pola
`MOS6522.cs:34-53`: `_orb,_ora,_ddrb,_ddra` byte, `_t1Counter,_t1Latch,
_t2Counter,_t2Latch` ushort, `_shiftRegister,_acr,_pcr,_ifr,_ier,
_latchedPortA,_latchedPortB` byte, `_ca1,_ca2,_cb1,_cb2` bool —
**współdzielony z PET VIA, pisz raz**), `MOS2114 _colorRam` (trywialne,
`_data: byte[]`), `Vic20KeyboardMatrix`, `Vic20Joystick`, `Vic20UserPort`,
`Vic20Datasette`, `Vic20SerialBus`/`_mountedDrives` (protokół IEC do
napędów dyskowych — dzieli maszynerię z PET `PetIeeeBus`, patrz ograniczenie
w Zadaniu 2), `_mountedCartridges` (montowane po konstrukcji, jak dysk w
CPC6128).

**Świadome cięcie zakresu v1:** stan kartridżów/urządzeń
`Vic20ExpansionDeviceRegistry` **nie wchodzi** do snapshotu v1 — capability
`ICartridgeViewModel` istnieje, ale slot jest polimorficzny (dowolny typ
urządzenia rozszerzenia). `CaptureState()` na maszynie z zamontowanym
kartridżem musi to udokumentować jawnie (komentarz + test), nie ciche
gubienie stanu — patrz test niżej.

- [x] `MOS6522.CaptureState()`/`RestoreState()` w
      `lib/PetEmulator.Chips/MOS6522.cs` — impact analysis przed edycją,
      chip współdzielony (VIC-20 VIA1+VIA2, PET VIA w Zadaniu 2).
- [x] `MOS6560.CaptureState()`/`RestoreState()` — rejestry, raster
      counter, fazy audio, LFSR szumu.
- [x] `MOS2114.CaptureState()`/`RestoreState()` — trywialne.
- [x] `Vic20MemoryBus.CaptureState()`/`RestoreState()` — zero-page +
      built-in RAM.
- [x] `Vic20KeyboardMatrix`/`Vic20Joystick`/`Vic20UserPort`/
      `Vic20Datasette.CaptureState()`/`RestoreState()`.
- [x] `Vic20Snapshot : MachineSnapshot` — bez pola na kartridże/expansions
      w v1; XML doc na klasie wprost mówi że stan kartridża nie jest
      objęty.
- [x] `Vic20Machine.CaptureState()`/`RestoreState()`,
      `IMachineStateStore<Vic20Snapshot>`.
- [x] Test: round-trip na nowej instancji, wariant z zamontowanym dyskiem
      IEC, **i** test na maszynie z zamontowanym kartridżem, który jawnie
      asercjuje udokumentowane ograniczenie (np. `CaptureState` rzuca albo
      restore nie odtwarza cartridge — wybierz jedno zachowanie i przetestuj
      je, nie zostawiaj niezdefiniowanego).

**Weryfikacja:**

```text
dotnet build PetEmulator.slnx --nologo
dotnet test tests/PetEmulator.Vic20.Tests/PetEmulator.Vic20.Tests.csproj --nologo
dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --filter "FullyQualifiedName~MOS6522|FullyQualifiedName~MOS6560" --nologo
```

**Commit:** `feat(vic20): add machine snapshot capture/restore`

---

## Zadanie 2 — PET (najtrudniejszy, ale drugi — nie ostatni)

**Fakty:** `src/PetEmulator.Pet/PetMachine.cs` — `Cpu6502Classic _cpu`
zawsze, **plus opcjonalny** `M6809Cpu? _superPet6809Cpu` (SuperPET) z
przełącznikiem `_activeProcessor`/`_activeMemory`
(`SelectProcessor(SuperPetProcessor)`, ~linia 275). `MT6520 _pia1, _pia2`
(PIA, **inny chip niż VIA/CRTC/AY**, bez `CaptureState` — nowa praca),
`MOS6522 _via` (reużywa Zadanie 1), `MT6545? _crtc` (nullable per
`profile.RequiresCrtc`, **już ma `CaptureState`** z pracy nad CPC — tylko
guard na null), `MOS6551? _acia`/`MOS6702? _superPetProtectionDongle`/
`SuperPet6809MemoryBus? _superPet6809Memory` (wszystkie tylko SuperPET,
**poza zakresem v1**, patrz niżej), `PetDatasette`/`PetDatasette2`
(małe — `_pulseCycles`/`_pulseIndex`/`_cyclesUntilNextEdge`/
`_lastMotorOn`/`PlayPressed`/`TapeName`; `PetDatasette2.IsAtEnd` jest dziś
na stałe `true`, czyli efektywnie stub — nie inwestować w jego snapshot
ponad to co już robi), `PetKeyboardMatrix` (trywialne, `byte[10]`),
`PetIeeeBus`+`_ieeeBusBinding`+`_mountedDrives` (protokół IEEE-488: `BusState
_state`, `_listenerAddr,_talkerAddr`, `DAV,NRFD,NDAC,EOI` linie
handshake, `_writeAckDelay`, `_devices: List<IIeeeDevice>` z własnym
stanem silnika napędu każdy — **realnie trudniejsze niż `I8272Snapshot`
CPC6128**, bo to protokół wielu urządzeń na magistrali, nie rejestry
jednego kontrolera), `PetUserPort` (małe), `PetMemoryBus` (`_ram` per
profil, `_expansionRam?`/`_expansionControl` SuperPET — ROM nie wchodzi,
niezmienny per profil), luźne pola maszyny: `_keyboardSelectedRow,
_diskActivityPending, _ieeeByteCount, _pia1Cb1Phase`.

**Świadome cięcie zakresu v1 (analogicznie do "nie deklarować pełnej
zgodności CPC6128 na podstawie samego ekranu Ready"):**

- SuperPET (`_superPet6809Cpu`, `_acia`, `_superPetProtectionDongle`,
  `_superPet6809Memory`) **poza zakresem v1**. `RestoreState` na profilu
  SuperPET z brakującym stanem 6809 musi rzucić czytelny wyjątek, nie
  ciche pominięcie.
- IEEE-488 snapshot **tylko gdy magistrala jest bezczynna** (brak
  transferu w locie w momencie `CaptureState()`) — jeśli `_state` wskazuje
  aktywny transfer, `CaptureState()` rzuca albo dokumentuje utratę
  wierności; wybierz jedno, przetestuj.

To te dwa punkty (walidacja niekompletnego profilu + świadomy rzut zamiast
cichej utraty stanu) mają **najwcześniej** ujawnić, czy
`MachineSnapshot`/`IMachineStateStore<T>` w obecnym kształcie to udźwignie
— stąd PET zaraz po VIC-20, nie na końcu.

- [x] `MT6520.CaptureState()`/`RestoreState()` w `lib/PetEmulator.Chips/` —
      nowy chip, nie reużywalny z niczego wcześniejszego.
- [x] `PetMemoryBus.CaptureState()`/`RestoreState()` — `_ram`,
      `_expansionRam`, `_expansionControl` (tylko gdy profil je ma).
- [x] `PetKeyboardMatrix`/`PetDatasette`/`PetDatasette2`/`PetUserPort.
      CaptureState()`/`RestoreState()`.
- [x] `PetSnapshot : MachineSnapshot` — pola dla `_pia1,_pia2` (MT6520),
      `_via` (reużyty MOS6522), `_crtc` (nullable, reużyty MT6545),
      pamięć, klawiatura, datasety, user port, luźne pola maszyny
      (`_keyboardSelectedRow` itd.). Bez pól SuperPET w v1.
- [x] `PetMachine.RestoreState(PetSnapshot)` **waliduje, że bieżąca
      maszyna została zbudowana z tym samym `PetProfile`** (i, jeśli
      dotyczy, tym samym `ExpansionRomManifest`) zanim cokolwiek przywróci
      — rzuca czytelny wyjątek inaczej. Wzór: testy CPC6128 zawsze budują
      maszynę docelową z tym samym `Cpc6128MemoryBus.RomSize` przed
      `RestoreState`.
- [x] Jeśli `RestoreState` kiedykolwiek zmienia `_activeProcessor` (poza
      zakresem v1 bez SuperPET, ale zostaw komentarz na przyszłość) — musi
      ponownie wywołać `ApplyBusObserver()`, inaczej obserwator busa
      wskazuje starego aktywnego procesora (dokładnie błąd, przed którym
      ostrzega istniejący komentarz `BusObserver` w `PetMachine.cs`
      ~linia 163-176).
- [x] `IMachineStateStore<PetSnapshot>` na `PetMachine`.
- [x] Test w `tests/PetEmulator.Pet.Tests/` — round-trip na nowej
      instancji dla co najmniej dwóch profili (jeden bez CRTC, jeden z
      CRTC — `profile.RequiresCrtc`), wariant z zamontowanym napędem IEC
      w stanie bezczynnym, test że restore na maszynie z **innym**
      profilem rzuca zamiast cicho psuć stan.
- [x] **Kontrola po tym zadaniu:** jeśli powyższe wymusiło zmianę kształtu
      `MachineSnapshot`/`IMachineStateStore<T>` w Core — to jest
      dokładnie ten moment, żeby to zrobić; TRS-80/Kaypro (Zadania 3-4)
      jeszcze nie istnieją, więc nic nie trzeba przerabiać wstecz.

**Weryfikacja:**

```text
dotnet build PetEmulator.slnx --nologo
dotnet test tests/PetEmulator.Pet.Tests/PetEmulator.Pet.Tests.csproj --nologo
dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --filter "FullyQualifiedName~MT6520" --nologo
```

**Commit:** `feat(pet): add machine snapshot capture/restore (no SuperPET, idle-bus IEEE-488 only)`

---

## Zadanie 3 — TRS-80 (niskiego ryzyka — kontrakt już sprawdzony na PET)

**Fakty:** `src/PetEmulator.Trs80/Trs80Machine.cs` — `Z80Cpu Cpu` (snapshot
darmowy), `Trs80MemoryBus Bus` (`_rom: byte[RomEnd]` niezmienny nie
kopiować, `_ram: byte[65536]` płaski, bez bankowania), `Trs80KeyboardMatrix
Keyboard`, `Trs80CassettePlayer? Cassette` (nullable, podmieniany na żywo
przez `LoadTape`), `Trs80FdcWiring? Fdc` (nullable, kontroler `FD1791` —
`lib/PetEmulator.Chips/FD1791.cs`, **bez** `CaptureState`/`RestoreState`
dziś). `Trs80RasterDisplay Video` renderuje z `Bus.Ram` bezpośrednio — nie
ma własnego stanu do przechwycenia.

- [x] `FD1791.CaptureState()`/`RestoreState()` w
      `lib/PetEmulator.Chips/FD1791.cs` — pola: `_driveSelect, _track,
      _sector, _data, _status, _pendingCommand, _recordType` (byte),
      `_pendingTStates, _readSearchTStates, _transferIndex,
      _dataDeadlineTStates, _addressMarkIndex` (int), `_tStateCounter`
      (long), `_writeTransfer, _dataRequestPending, _driveSelectWritten,
      _typeICommand` (bool), `_lastStepDirection`,
      `IntrqAsserted`/`DoubleDensityEnabled`/`InterruptSequence`/
      `MultipleRecordEnabled`, `_drives: IFD1791DiskImage?[DriveCount]`
      (obraz dysku montowany po restore jak w CPC6128, nie w samym
      snapshotcie). Impact analysis przed edycją — chip współdzielony z
      Kaypro przez `FD1793 : FD1791`.
- [x] `Trs80CassettePlayer.CaptureState()`/`RestoreState()` — sprawdź
      dokładne pola pozycji odtwarzania przed pisaniem (nie zgaduj z nazwy
      klasy, przeczytaj plik).
- [x] `Trs80KeyboardMatrix.CaptureState()`/`RestoreState()` — trywialne,
      jeden `byte[]`/macierz stanu klawiszy.
- [x] `Trs80MemoryBus.CaptureState()`/`RestoreState()` — tylko `_ram`, ROM
      nie wchodzi do snapshotu (niezmienny per konstrukcja).
- [x] `Trs80Snapshot : MachineSnapshot` (nowy plik w
      `src/PetEmulator.Trs80/`) — pola `Fdc`/`Cassette` nullable
      (odzwierciedlają nullability na maszynie — wzorzec już sprawdzony w
      Zadaniu 2 na PET), `Memory`, `Keyboard`.
- [x] `Trs80Machine : IMachineStateStore<Trs80Snapshot>`, `CaptureState()`/
      `RestoreState()` wołające każdy składnik; `RestoreState` waliduje
      `Version` i rzuca czytelny wyjątek przy niezgodności (wzór:
      `Cpc464Machine.RestoreState`).
- [x] Test w `tests/PetEmulator.Trs80.Tests/` — round-trip na **nowej**
      instancji maszyny (nie tej samej, wzór z `ebf373d` w historii CPC:
      restore na świeżym obiekcie łapie więcej niż restore na żywym), oraz
      wariant z zamontowanym FDC/dyskiem.

**Weryfikacja:**

```text
dotnet build PetEmulator.slnx --nologo
dotnet test tests/PetEmulator.Trs80.Tests/PetEmulator.Trs80.Tests.csproj --nologo
dotnet test tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj --filter "FullyQualifiedName~FD1791" --nologo
```

**Commit:** `feat(trs80): add machine snapshot capture/restore`

---

## Zadanie 4 — Kaypro (najtańszy, korzysta z Zadania 3)

**Fakty:** `src/PetEmulator.Kaypro/KayproMachine.cs` jest cienkie (`Bus` +
`Processor`); realny stan żyje w `KayproBus.cs`: `_ram: byte[65536]`,
`_rom: byte[0x800]` (niezmienny), `KayproFdcWiring` (`FD1793`, 2 napędy —
**dostaje `CaptureState` za darmo z Zadania 3**, `FD1793` nie ma własnych
pól ponad `FD1791`), `KayproVideo` (**własna** VRAM `_memory:
byte[3072]`, `KayproVideo.cs:15`, nie jest widokiem `_ram` — potrzebuje
osobnego snapshotu), `KayproSioWiring`/`KayproPioWiring` (**już mają**
`CaptureState`/`RestoreState` z niezwiązanej wcześniejszej pracy — zero
nowego kodu), `InterruptLines`, `FdcNmiPulseCount: ulong` (licznik na
busie, przechwycić wprost).

- [x] `KayproVideo.CaptureState()`/`RestoreState()` — jeden `byte[3072]`.
- [x] `KayproSnapshot : MachineSnapshot` — `Ram`, `Video`, `Fdc`
      (`FD1791`/`FD1793` snapshot z Zadania 3), `Sio`, `Pio`
      (`Z80SioSnapshot`/pio snapshoty, już istniejące typy),
      `FdcNmiPulseCount`.
- [x] `KayproMachine`/`KayproBus` — `CaptureState()`/`RestoreState()`,
      `IMachineStateStore<KayproSnapshot>`.
- [x] Test w `tests/PetEmulator.Kaypro.Tests/` — round-trip na nowej
      instancji + z zamontowanym dyskiem.

**Weryfikacja:**

```text
dotnet build PetEmulator.slnx --nologo
dotnet test tests/PetEmulator.Kaypro.Tests/PetEmulator.Kaypro.Tests.csproj --nologo
```

**Commit:** `feat(kaypro): add machine snapshot capture/restore`

---

## Zadanie 5 — domknięcie

- [x] `MachineContractTests.cs` (`tests/PetEmulator.Core.Tests/`) — dodaj
      asercję `IMachineStateStore<TSnapshot>` dla wszystkich sześciu
      realnych maszyn (CPC464/CPC6128 już mają, dodaj PET/VIC-20/TRS-80/
      Kaypro), wzorem istniejącej macierzy `IMachine`.
- [x] `2026-09-16-cpc-architecture.md` — zaznacz w diagramie
      `MachineSnapshot` hierarchy `PetSnapshot`/`Vic20Snapshot`/
      `Trs80Snapshot`/`KayproSnapshot`
      jako istniejące, odhacz punkt 4 Fazy 1 z listy.
- [x] `2026-09-16-open-items.md` — usuń zrealizowany punkt "Pełne
      snapshoty PET, VIC-20, TRS-80, Kaypro".
- [ ] Pełny `dotnet test PetEmulator.slnx --filter
      "TestCategory!=BinaryBoot"` zielony na całym solution.

  Uwaga: wszystkie testy VIC-20 wcześniej podejrzane o spowolnienie
  przeszły osobno; pełny przebieg solution nie zwrócił końcowego
  podsumowania, więc nie jest jeszcze formalnie zaliczony.

**Commit:** `docs: close remaining-machine-snapshots plan`
