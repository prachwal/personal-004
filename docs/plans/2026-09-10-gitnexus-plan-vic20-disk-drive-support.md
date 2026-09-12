# GitNexus Engineering Plan

> Task: Add VIC-20 disk drive support (1541-class drive over the CBM serial/IEC bus), reusing PET's CbmDosEngine/D64Image DOS-device layer.
> Evidence verified at commit 703a83a2204e3f768274f199fc1979172b70b882; GitNexus index fresh at plan start (1 commit behind noted mid-session from an unrelated concurrent index write, not load-bearing for this plan's findings).
> Evidence provenance schema 2; global dirty digest sha256:0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd; cited-path manifest 13 sorted entries; exact generated plan path excluded.

## Objective (§1)

Give `Vic20Machine` a mountable disk drive (device #8 convention) so a booted
VIC-20 can `LOAD"$",8` / `LOAD"NAME",8` / `SAVE"NAME",8` against a real `.d64`
image, mirroring what `PetMachine.MountDisk`/`MountNewDisk` already do for the
PET, with the same Desktop UI affordance (`Devices`, disk icon/status, New/Load
disk commands).

## Current Behaviour (§2-3) - architecture folded in

**PET disk stack (fully working, reusable almost entirely as-is):**
`PetMachine` [verified, src/PetEmulator.Pet/PetMachine.cs:132-141] owns a
`PetIeeeBus` (byte-level LISTEN/TALK/UNLISTEN/OPEN/CLOSE command decode +
DAV/NRFD/NDAC handshake state machine, `src/PetEmulator.Pet/Ieee488/PetIeeeBus.cs:16-377`)
that talks to any `IIeeeDevice` [verified, `.../Ieee488/IIeeeDevice.cs`] -
`PrimaryAddress`/`OpenForRead`/`OpenForWrite`/`Close`/`Write`/`TryRead`. The
only real device today is `PetIeeeDiskDrive` [verified,
`.../CbmDos/PetIeeeDiskDrive.cs`], a thin adapter over `CbmDosEngine`
[verified, `.../CbmDos/CbmDosEngine.cs:8-476`] which implements CBM DOS
command/file semantics directly against a `D64Image` in RAM - no real 1541
firmware/CPU is emulated. `PetIeeeBus` is wired to real chips by
`PetIeeeBusBinding` [verified, `.../Ieee488/PetIeeeBusBinding.cs`]: PIA2's 8-bit
data port carries DIO, PIA2 CA2/CB2 drive NDAC/DAV, and VIA Port B bit 2
(`DDRB`-gated) drives ATN - i.e. genuinely a **parallel, byte-at-a-time**
IEEE-488 electrical model, which is what real PET hardware has.

`CbmDosEngine`/`D64Image`/`IIeeeDevice`/`PetIeeeDiskDrive` contain **no
PET-specific coupling** [verified - none of the four files reference
`PetMachine`, `PetMemoryBus`, or any PET-only chip]; they operate purely on
bytes in/out plus a device number. This is the reusable core.

**VIC-20 machine (current state):** `Vic20Machine` [verified,
`src/PetEmulator.Vic20/Vic20Machine.cs:22-213`] wires `MOS6560` (VIC), two
`MOS6522` VIAs (`Via1`@$9110, `Via2`@$9120), color RAM, keyboard matrix and
`Vic20Datasette` (cassette). It has no IEEE/serial bus of any kind today.
`docs/vic20/migration-plan.md` [verified, lines 12-13 and "Czego NIE robić w
v1"] explicitly scoped disk out of v1 ("VIC-20 miał inny format taśmy niż PET
- osobna decyzja później" for tape, disk not mentioned at all - never
attempted). `MOS6522` [verified, `lib/PetEmulator.Chips/MOS6522.cs`] exposes
`PortAWritten`/`PortBWritten` callbacks and **plain get/set properties** for
`CA1`/`CA2`/`CB1`/`CB2`/`CA2Output`/`CB2Output` - **no output-change events**
like `MT6520`'s `Ca2OutputChanged`/`Cb2OutputChanged` that `PetIeeeBusBinding`
relies on. A VIC-20 binding must therefore **poll** CA2Output/CB2Output (and
whichever VIA pins carry CLK/DATA) once per `Tick()`/cycle and edge-detect
itself, rather than subscribing to a push event.

**Desktop UI pattern to mirror:** `PetMachineViewModel.NewDisk`/`LoadDisk`
[verified, `.../ViewModels/PetMachineViewModel.cs:145-152`] call straight
through to `PetMachine.MountDisk`/`MountNewDisk`; `MainWindowViewModel`
[verified, `.../ViewModels/MainWindowViewModel.cs:101-137`] adds the file-picker
async wrapper and routes by `CurrentMachine` type. `IDeviceStatus` [verified,
`lib/PetEmulator.Core/IDeviceStatus.cs`] already lives in the shared core
specifically "once a second machine (VIC-20) needed a shared per-machine
device-status contract" - `PetIeeeDriveStatus` is a 15-line record-shaped
class implementing it.

**The one real hardware gap - not glossed over:** real VIC-20/C64 disk drives
attach over the **CBM serial (IEC) bus**, not parallel IEEE-488. IEC has only
3 signal lines (ATN/CLK/DATA) and the KERNAL shifts each byte out/in **one bit
at a time**, unlike PET's 8-bit-wide DIO port. `PetIeeeBus`'s state machine is
byte-granular by construction (it never models individual DAV/NRFD/NDAC bit
pulses within a byte - it doesn't need to, because real IEEE-488 hardware
transfers a full byte per handshake). A serial-IEC bus genuinely needs a
**bit-level** state machine (shift in/out 8 bits, track CLK/DATA edges per
bit, EOI signaled by a deliberate timing gap) before it can hand a decoded
byte to the exact same `IIeeeDevice`/`CbmDosEngine` used today. This is new
work, not a port of `PetIeeeBus`.

## GitNexus Findings (§4-5)

- `query("PET disk drive IEEE-488 CbmDosEngine D64Image")`: confirmed
  `MountDisk`/`AttachDevice`/`CbmDosEngine` process chain (`NewDisk ->
  AttachDevice`, `NewDisk -> CbmDosEngine`), all PET-side.
- `context("Vic20Machine")`: 7 direct (d=1) callers - `Vic20MachineViewModel`
  ctor, `Vic20DebuggerSession.EnsureMachine`, 5 tests (4 tape tests + 1
  keyboard-boot test). `Vic20Machine` implements shared `IMachine` (6
  implementations - dispatch-boundary caveat noted, lower-bound impact).
- `context("PetIeeeBus")`: confirmed exact (`epistemic: exact`) - all incoming
  calls are `PetMachine`'s constructor and `PetIeeeBus*Tests`; no VIC-20-side
  reference anywhere, confirming zero accidental coupling to port from.
- `impact("Vic20Machine", upstream)`: **risk CRITICAL**, 20 impacted symbols,
  d=1: 7 (`Vic20MachineViewModel` ctor, `Vic20DebuggerSession.EnsureMachine`,
  5 tests), d=2: 11 (incl. `MainWindowViewModel` ctor, several
  `Vic20DebuggerSession` commands), d=3: 2 (`MainWindow` ctor,
  `Vic20DebuggerSession.Execute`). **Flagging per this repo's CLAUDE.md: HIGH/
  CRITICAL risk must be surfaced before editing.** The risk is inherent to
  `Vic20Machine` being the machine's composition root (every consumer touches
  it) - not a sign the change is unsafe, but every d=1 caller's existing
  behavior (ctor signature, `Reset`/`StepInstruction`/`Devices` shape) must
  stay compatible; additive-only changes (new property, new method) keep this
  green.

## Proposed Changes (§6)

1. **`src/PetEmulator.Vic20/Serial/Vic20SerialBus.cs`** (new) - bit-serial
   IEC ATN/CLK/DATA state machine, same responsibility shape as `PetIeeeBus`
   (LISTEN/TALK/OPEN/CLOSE command decode, device registry) but built around
   shifting 8 bits per byte instead of one parallel handshake per byte. Holds
   `List<IIeeeDevice>` (type reused unchanged) and dispatches decoded command/
   data bytes to devices exactly like `PetIeeeBus.ProcessCommandByte`/
   `OnDioWrite` do today. `Tick()` advances bit-shift timing (mirrors
   `PetIeeeBus.Tick`'s settle-delay pattern).
2. **`src/PetEmulator.Vic20/Serial/Vic20SerialBusBinding.cs`** (new) - wires
   `Vic20SerialBus` to VIA1/VIA2 pins. **Exact CLK/DATA/ATN-to-VIA-pin mapping
   is an open question (§12)** - must be pinned from the real VIC-20 KERNAL
   IEC routines (SETLFS/OPEN/IECOUT/IECIN family) before this file is written,
   same methodology already used in this repo for keyboard row/col
   (`Vic20Machine.cs:59-67`) and tape encoding (`Vic20Machine.cs:156-206`).
   Must poll `CA2Output`/`CB2Output`/`CA1`/`CB1` once per `Tick()` and
   edge-detect locally (no `Ca2OutputChanged`-style event exists on
   `MOS6522` - confirmed above) rather than adding output-change events to the
   shared `MOS6522` chip (avoids any risk to the PET side, which also uses
   `MOS6522` for its own VIA).
3. **`Vic20Machine`** (`src/PetEmulator.Vic20/Vic20Machine.cs`) - additive
   only, mirroring `PetMachine`'s shape 1:1:
   - new fields `_serialBus`/`_serialBusBinding`, constructed after `_via1`/
     `_via2` and before `Reset()`.
   - `_serialBusBinding.Tick()` (or `.Tick(cycles)`) called from
     `StepInstruction()` alongside the existing `_via1.Tick`/`_via2.Tick`
     calls.
   - `public void MountDisk(string path, int deviceNumber = 8)` and
     `public void MountNewDisk(string path, string diskName, string diskId =
     "00", int deviceNumber = 8)` - byte-identical bodies to
     `PetMachine.MountDisk`/`MountNewDisk` (§2-3), just against
     `_serialBus.AttachDevice` and reusing `PetIeeeDiskDrive`/
     `PetIeeeDriveStatus` unchanged (both are transport-agnostic already -
     confirmed above).
   - `Devices` extended from `[new Vic20DatasetteStatus(_datasette)]` to
     `[new Vic20DatasetteStatus(_datasette), .. _mountedDrives]`, same pattern
     as `PetMachine.Devices`.
   - Ctor signature unchanged (`string romsRoot, Vic20DisplayConfig?
     displayConfig`) - no d=1 caller needs updating.
4. **`Vic20MachineViewModel`**/**`MainWindowViewModel`**
   (`src/PetEmulator.Desktop/ViewModels/`) - add `NewDisk`/`LoadDisk` methods
   that delegate to `Vic20Machine.MountNewDisk`/`MountDisk`, copied from
   `PetMachineViewModel`'s equivalents (§2-3); wire the same file-picker
   commands `MainWindowViewModel` already has for PET, routed by
   `CurrentMachine` type (existing dispatch pattern in that file).

## Implementation Sequence (§7)

1. **Pin the IEC wiring** (prerequisite, no code): read the real VIC-20/1541
   KERNAL disassembly's serial routines to confirm exact VIA1/VIA2 pin
   assignment for ATN OUT/IN, CLK OUT/IN, DATA OUT/IN (candidates: VIA1
   PA6/PA7 + CA2/CB1, VIA2 PB - needs verification, not assumed here). Record
   findings the same way `Vic20Machine.cs`'s existing comments cite exact
   disassembly addresses. Resolves §12 open question 1.
2. `Vic20SerialBus` (device-agnostic bit-shift state machine) + unit tests
   modeled on `PetIeeeBusTests.cs` (command decode, handshake, EOI) but for
   bit-serial framing.
3. `Vic20SerialBusBinding` wiring VIA1/VIA2 per step 1's findings + unit
   tests modeled on `PetIeeeBusBindingTests.cs`.
4. `Vic20Machine.MountDisk`/`MountNewDisk`/`Devices` extension + wire
   `_serialBusBinding` into the ctor/`StepInstruction`/`Reset`. `impact()` on
   `Vic20Machine` first (already run in this plan - CRITICAL/additive-only
   confirmed safe); re-run after the diff to confirm no d=1 signature broke.
5. Layer 2 end-to-end test mirroring `PetDiskEndToEndTests` (boot real KERNAL
   -> `LOAD"$",8` directory listing, then a `SAVE`/`NEW`/`LOAD`/`RUN`
   round-trip) - this is the real correctness gate; nothing before it proves
   the bit-serial framing is actually right.
6. Desktop `Vic20MachineViewModel`/`MainWindowViewModel` UI wiring +
   `Vic20DebuggerSession` CLI commands (mirror `mount-disk`/`new-disk` if
   `PetDebuggerSession` has them - verify at implementation time).
7. `detect_changes({scope:"all"})`, full `dotnet test`, docs update
   (`docs/vic20/migration-plan.md`'s "Czego NIE robić" line removing disk from
   the exclusion list; add a `docs/vic20/disk.md` mirroring
   `docs/pet/disk-testing-strategy.md`'s layer structure) before commit.

## Test Strategy (§8)

- **Layer 0** (new): `Vic20SerialBusTests.cs` - command byte decode, LISTEN/
  TALK/UNLISTEN handshake, EOI signaling, at the bit-shift level (register
  pokes into the bus, no VIA/CPU involved) - mirrors
  `tests/PetEmulator.Pet.Tests/Ieee488/PetIeeeBusTests.cs`.
- **Layer 1** (new): `Vic20SerialBusBindingTests.cs` - VIA pin <-> bus line
  mapping, mirrors `PetIeeeBusBindingTests.cs`.
- **Layer 2** (new): `Vic20DiskEndToEndTests.cs` - real KERNAL, real ROMs,
  `TextTyper`-driven `LOAD"$",8` / `SAVE"NAME",8` / reload round trip, mirrors
  `tests/PetEmulator.Pet.Tests/CbmDos/PetDiskEndToEndTests.cs` scenario-for-
  scenario (that file's own doc comments already document two false-positive
  traps to avoid: bare-byte-presence checks and reused READY-count
  thresholds - carry both fixes forward, don't reintroduce either).
- **Layer 3**: `scripts/test-vic20-disk-loading.sh` mirroring
  `scripts/test-pet-disk-loading.sh` (exists per `scripts/` listing).
- Regression: full `dotnet test` (PET disk tests untouched - confirms zero
  behavioral change to `PetIeeeBus`/`CbmDosEngine`/`D64Image`).
- Verification commands: `dotnet build`, `dotnet test` (existing, confirmed
  runnable this session), `node .gitnexus/run.cjs detect-changes --scope all
  --repo .` before commit.

## Implementation Context (§11)

```yaml
implementation_context:
  task_summary: 'Add VIC-20 disk drive support (1541-class, CBM serial/IEC bus) by reusing PET''s CbmDosEngine/D64Image/IIeeeDevice/PetIeeeDiskDrive device layer behind a new bit-serial Vic20SerialBus + Vic20SerialBusBinding (VIA1/VIA2), mirroring PetMachine.MountDisk/MountNewDisk and the Desktop disk UI.'
  acceptance_criteria:
    - 'Vic20Machine.MountDisk(path, deviceNumber=8) and MountNewDisk(...) mount a real .d64 the same way PetMachine does'
    - 'A booted VIC-20 running real KERNAL/BASIC can LOAD"$",8, LOAD"NAME",8, and SAVE"NAME",8 against that image (Layer 2 test, TextTyper-driven, no register pokes)'
    - 'Vic20Machine.Devices reports mounted drives via the existing IDeviceStatus contract, mirroring PetMachine.Devices'
    - 'Desktop UI (Vic20MachineViewModel/MainWindowViewModel) exposes New/Load disk the same way it does for PET'
    - 'Zero behavioral change to PetIeeeBus/CbmDosEngine/D64Image/PetMachine (full PET regression suite still green)'

  evidence_provenance:
    schema_version: 2
    head_commit: '703a83a2204e3f768274f199fc1979172b70b882'
    generated_plan_path: 'docs/plans/2026-09-10-gitnexus-plan-vic20-disk-drive-support.md'
    global_dirty_digest:
      algorithm: 'sha256'
      canonicalization: 'gitnexus-evidence-provenance-v2 NUL-framed UTF-8 records'
      value: '0a9c85780067d9afcd0764f307b60891e3cee927ee11eaeb5ec7826d10fd82cd'
    cited_path_manifest:
      - path: 'docs/vic20/migration-plan.md'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:09d788c2cbc6c727c1c3e2a0a9cf7fb822487fc5151d2cf6f83b35f052ac02af'
        index_digest: 'sha256:09d788c2cbc6c727c1c3e2a0a9cf7fb822487fc5151d2cf6f83b35f052ac02af'
        worktree_digest: 'sha256:09d788c2cbc6c727c1c3e2a0a9cf7fb822487fc5151d2cf6f83b35f052ac02af'
        untracked_digest: absent
      - path: 'lib/PetEmulator.Chips/MOS6522.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:0f132662660fea60952eff998ac50fe769004d7bdda43bdd3f3c3c9864b079f5'
        index_digest: 'sha256:0f132662660fea60952eff998ac50fe769004d7bdda43bdd3f3c3c9864b079f5'
        worktree_digest: 'sha256:0f132662660fea60952eff998ac50fe769004d7bdda43bdd3f3c3c9864b079f5'
        untracked_digest: absent
      - path: 'lib/PetEmulator.Core/IDeviceStatus.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:a545b29f5e8e32187748a4263c82c3468fadd6965782395b0aeed69dcf08ad8b'
        index_digest: 'sha256:a545b29f5e8e32187748a4263c82c3468fadd6965782395b0aeed69dcf08ad8b'
        worktree_digest: 'sha256:a545b29f5e8e32187748a4263c82c3468fadd6965782395b0aeed69dcf08ad8b'
        untracked_digest: absent
      - path: 'src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:7e3c0e3af038bd65ebd2e86d6b1d4a67f8963ab1ccd5b2288260ce88105a87f0'
        index_digest: 'sha256:7e3c0e3af038bd65ebd2e86d6b1d4a67f8963ab1ccd5b2288260ce88105a87f0'
        worktree_digest: 'sha256:7e3c0e3af038bd65ebd2e86d6b1d4a67f8963ab1ccd5b2288260ce88105a87f0'
        untracked_digest: absent
      - path: 'src/PetEmulator.Desktop/ViewModels/PetMachineViewModel.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:353e193d02fabe4d6ecb984745608afbfc32b232338a17d5ea9af6e926502806'
        index_digest: 'sha256:353e193d02fabe4d6ecb984745608afbfc32b232338a17d5ea9af6e926502806'
        worktree_digest: 'sha256:353e193d02fabe4d6ecb984745608afbfc32b232338a17d5ea9af6e926502806'
        untracked_digest: absent
      - path: 'src/PetEmulator.Pet/CbmDos/CbmDosEngine.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:8e1722d76702ddf7e0db50a9c0cf7bb3cfd73f2d6f8dd66e97a201334b15acc2'
        index_digest: 'sha256:8e1722d76702ddf7e0db50a9c0cf7bb3cfd73f2d6f8dd66e97a201334b15acc2'
        worktree_digest: 'sha256:8e1722d76702ddf7e0db50a9c0cf7bb3cfd73f2d6f8dd66e97a201334b15acc2'
        untracked_digest: absent
      - path: 'src/PetEmulator.Pet/CbmDos/PetIeeeDiskDrive.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:8991932a2dcaf5dea052df37bd0b289e26b34e6483a6ede972fdf33843a15b75'
        index_digest: 'sha256:8991932a2dcaf5dea052df37bd0b289e26b34e6483a6ede972fdf33843a15b75'
        worktree_digest: 'sha256:8991932a2dcaf5dea052df37bd0b289e26b34e6483a6ede972fdf33843a15b75'
        untracked_digest: absent
      - path: 'src/PetEmulator.Pet/Devices/PetIeeeDriveStatus.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:b10160ccb81aa855fe47ef3e8df0ba5525cd99c8d59c69f96107fc715acb9fae'
        index_digest: 'sha256:b10160ccb81aa855fe47ef3e8df0ba5525cd99c8d59c69f96107fc715acb9fae'
        worktree_digest: 'sha256:b10160ccb81aa855fe47ef3e8df0ba5525cd99c8d59c69f96107fc715acb9fae'
        untracked_digest: absent
      - path: 'src/PetEmulator.Pet/Ieee488/IIeeeDevice.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:9113819324c32a17533e59a4d7178118ad605f04cd00260b307d9758d04e378b'
        index_digest: 'sha256:9113819324c32a17533e59a4d7178118ad605f04cd00260b307d9758d04e378b'
        worktree_digest: 'sha256:9113819324c32a17533e59a4d7178118ad605f04cd00260b307d9758d04e378b'
        untracked_digest: absent
      - path: 'src/PetEmulator.Pet/Ieee488/PetIeeeBus.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:c73c9d8532505e29d0f84024325fefc79f701207b70ec4300fef0d57aa547df5'
        index_digest: 'sha256:c73c9d8532505e29d0f84024325fefc79f701207b70ec4300fef0d57aa547df5'
        worktree_digest: 'sha256:c73c9d8532505e29d0f84024325fefc79f701207b70ec4300fef0d57aa547df5'
        untracked_digest: absent
      - path: 'src/PetEmulator.Pet/Ieee488/PetIeeeBusBinding.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:3a90ed89c14f493199d4e978162a8dc3c553f707fa06aba64696cc3d6d0cea46'
        index_digest: 'sha256:3a90ed89c14f493199d4e978162a8dc3c553f707fa06aba64696cc3d6d0cea46'
        worktree_digest: 'sha256:3a90ed89c14f493199d4e978162a8dc3c553f707fa06aba64696cc3d6d0cea46'
        untracked_digest: absent
      - path: 'src/PetEmulator.Pet/PetMachine.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:97b7a1a5597838678b3f150959189bfe4ce4573d86cf228aaeb80628df515beb'
        index_digest: 'sha256:97b7a1a5597838678b3f150959189bfe4ce4573d86cf228aaeb80628df515beb'
        worktree_digest: 'sha256:97b7a1a5597838678b3f150959189bfe4ce4573d86cf228aaeb80628df515beb'
        untracked_digest: absent
      - path: 'src/PetEmulator.Vic20/Vic20Machine.cs'
        object_kind: {head: regular, index: regular, worktree: regular, untracked: absent}
        state: clean
        rename_from: null
        rename_to: null
        head_digest: 'sha256:8c69ca980aa3157f81acd3a4ae1beab394784340d7ecce2acea127cfc6c5742d'
        index_digest: 'sha256:8c69ca980aa3157f81acd3a4ae1beab394784340d7ecce2acea127cfc6c5742d'
        worktree_digest: 'sha256:8c69ca980aa3157f81acd3a4ae1beab394784340d7ecce2acea127cfc6c5742d'
        untracked_digest: absent

  files_to_modify:
    - file: 'src/PetEmulator.Vic20/Serial/Vic20SerialBus.cs'
      symbols: ['Vic20SerialBus']
      intended_change: 'New: bit-serial IEC ATN/CLK/DATA state machine, device registry reusing IIeeeDevice'
    - file: 'src/PetEmulator.Vic20/Serial/Vic20SerialBusBinding.cs'
      symbols: ['Vic20SerialBusBinding']
      intended_change: 'New: wires Vic20SerialBus to VIA1/VIA2 pins (mapping TBD, see open_questions)'
    - file: 'src/PetEmulator.Vic20/Vic20Machine.cs'
      symbols: ['Vic20Machine']
      intended_change: 'Additive: _serialBus/_serialBusBinding fields, MountDisk/MountNewDisk methods, Devices list extension, Tick wiring in StepInstruction - ctor signature unchanged'
    - file: 'src/PetEmulator.Desktop/ViewModels/Vic20MachineViewModel.cs'
      symbols: ['Vic20MachineViewModel']
      intended_change: 'Add NewDisk/LoadDisk delegating to Vic20Machine, mirroring PetMachineViewModel'
    - file: 'src/PetEmulator.Desktop/ViewModels/MainWindowViewModel.cs'
      symbols: ['MainWindowViewModel']
      intended_change: 'Route New/Load-disk commands to Vic20MachineViewModel when CurrentMachine is VIC-20, mirroring the existing PET routing'
    - file: 'docs/vic20/migration-plan.md'
      symbols: []
      intended_change: 'Remove disk from the "Czego NIE robić w v1" exclusion list once shipped'

  tests:
    - file: 'tests/PetEmulator.Vic20.Tests/Serial/Vic20SerialBusTests.cs'
      scenarios:
        - 'LISTEN+secondary+data bytes -> device.Write receives decoded bytes (mirrors PetIeeeBusTests.ListenAndDataOut_SendsBytesToDevice)'
        - 'TALK -> device.TryRead bytes clocked back out, EOI asserted on last byte'
        - 'ATN asserted mid-transfer -> transfer aborted, command mode resumed (mirrors PetIeeeBusTests reset/ATN cases)'
    - file: 'tests/PetEmulator.Vic20.Tests/Serial/Vic20SerialBusBindingTests.cs'
      scenarios:
        - 'VIA pin writes drive ATN/CLK/DATA into the bus (exact pins per open_questions resolution)'
        - 'Bus handshake lines read back correctly via VIA port input (mirrors PetIeeeBusBindingTests)'
    - file: 'tests/PetEmulator.Vic20.Tests/CbmDos/Vic20DiskEndToEndTests.cs'
      scenarios:
        - 'Boot real KERNAL, LOAD"$",8 -> directory text appears on screen (mirrors PetDiskEndToEndTests.LoadReadsARealFilesBytesCorrectly shape, adapted to VIC-20 BASIC)'
        - 'SAVE"NAME",8 then NEW then LOAD"NAME",8 then RUN -> program genuinely re-executes (mirrors PetDiskEndToEndTests.SaveThenLoadRoundTripsARealProgramThroughARealDisk, including its before/after-count false-positive fix)'

  verification_commands:
    - 'dotnet build'
    - 'dotnet test'
    - 'node .gitnexus/run.cjs detect-changes --scope all --repo .'

  risks:
    - 'Vic20Machine is a CRITICAL-risk upstream target (impact(): 20 symbols, 7 direct callers) - any change must stay additive (new members only, ctor signature untouched) or every d=1/d=2 caller listed in §4 needs re-verification.'
    - 'Bit-serial IEC timing is unforgiving (PetIeeeBus itself needed a documented SettleDelayCycles fix - 32 to 4,000 cycles - to stop a real KERNAL ISR from racing the handshake); expect an analogous timing bug on first real LOAD attempt, budget time for BusObserver-driven tracing exactly as that fix was found.'
    - 'MOS6522 lacks CA2Output/CB2Output-changed events (unlike MT6520) - the binding must poll-and-edge-detect per Tick(); getting the poll cadence wrong (called from the wrong place, or not every cycle) will drop bit edges.'

  assumptions:
    - 'PetIeeeDiskDrive/CbmDosEngine/D64Image need zero code changes to serve a second, differently-wired bus - re-verify at implementation time by grep for any accidental PetMachine/PIA/VIA reference before wiring Vic20SerialBus to them (source inspection this session found none, but was not exhaustive over CbmDosEngine''s full 476 lines).'
    - 'scripts/test-pet-disk-loading.sh exists as the Layer 3 script to mirror - confirmed present in scripts/ listing this session, not opened.'

  open_questions:
    - 'Exact VIA1/VIA2 pin assignment for IEC ATN/CLK/DATA in/out on this repo''s modeled VIC-20 - not established anywhere in the current codebase or docs; must come from the real KERNAL IEC-routine disassembly before Vic20SerialBusBinding can be written (see §7 step 1). Do not guess from memory/generic VIC-20 references - this repo''s existing keyboard/tape wiring was previously found backwards this same way (see Vic20Machine.cs:59-67''s doc comment) and each was fixed only after checking a real disassembly.'
    - 'Whether Vic20DebuggerSession/PetDebuggerSession already have disk-mount CLI commands to mirror - not checked this session (PET CLI disk commands were out of this plan''s primary-symbol budget).'
    - 'Bit-vs-byte granularity for Vic20SerialBus: a fully bit-accurate shift-register model is the safe default (matches how the real KERNAL bit-bangs), but confirm no cheaper byte-level shortcut exists once step 1''s disassembly reading is done - do not assume one without checking, since IEC (unlike IEEE-488) has no parallel data port to shortcut through.'

  avoid:
    - 'Do not port PetIeeeBus''s DAV/NRFD/NDAC byte-handshake model as-is onto the VIC-20 - it is IEEE-488-parallel-specific and does not match IEC''s bit-serial electrical protocol.'
    - 'Do not add Ca2OutputChanged/Cb2OutputChanged-style events to the shared MOS6522 chip class to make binding easier - it is used by both PET VIA and VIC-20 VIA1/VIA2; poll-and-edge-detect in the new binding instead (see risks).'
    - 'Do not change Vic20Machine''s constructor signature - 7 direct callers (impact() CRITICAL) depend on it; add members additively.'
    - 'Do not repeat full repository discovery already captured in §2-5 above.'
```

## Assumptions and Open Questions (§12)

**Assumptions** (re-verify cheaply before relying on them):
- `CbmDosEngine`/`D64Image` carry no hidden PET coupling across their full
  bodies (only spot-verified this session, not read end-to-end).
- `scripts/test-pet-disk-loading.sh` exists as the Layer 3 pattern to mirror
  (seen in a directory listing, not opened).

**Open questions** (must resolve before/during implementation, see §7 step 1
and §11 `open_questions` for detail):
- Exact VIA1/VIA2 pin mapping for IEC ATN/CLK/DATA on this repo's VIC-20 model
  - not in the codebase or docs today; needs real KERNAL disassembly, not
    memory/guesswork.
- Whether `Vic20DebuggerSession`/`PetDebuggerSession` already expose disk-mount
  CLI commands to mirror.
- Confirm no byte-level shortcut exists for the serial bus once the
  disassembly is read (default to bit-accurate).

**Explicitly deferred (not in scope of this task):** multiple disks/second
drive unit, disk-image write-back to the host `.d64` file (PET doesn't have
this either per `PetDiskEndToEndTests`'s own doc comment - same limitation
inherited, not a new one), fastloaders/warp-load, PAL VIC-20 disk timing
differences.

## Definition of Done (§13)

- `Vic20Machine.MountDisk`/`MountNewDisk` exist and mount a real `.d64`.
- A real, unmodified VIC-20 KERNAL+BASIC boot can `LOAD"$",8` and see a real
  directory listing, and `SAVE"NAME",8` / `NEW` / `LOAD"NAME",8` / `RUN`
  genuinely round-trips a program (Layer 2 test green, TextTyper-driven, no
  register pokes - same rigor as `PetDiskEndToEndTests`).
- `Vic20Machine.Devices` reports the mounted drive via `IDeviceStatus`.
- Desktop UI can create/load a VIC-20 disk the same way it does for PET.
- Full `dotnet test` green, including the entire existing PET suite
  unchanged (proves zero regression to the reused DOS layer).
- `detect_changes({scope:"all"})` reviewed before commit; any HIGH/CRITICAL
  risk re-confirmed against the additive-only constraint in §6/§9.
- `docs/vic20/migration-plan.md` and a new `docs/vic20/disk.md` updated.
