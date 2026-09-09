# PET disk-drive testing strategy

Four layers, each proving something the layer below it can't. Every layer that can run in
`dotnet test` does; the shell scripts are a manual/CI smoke tier on top, not a replacement for
any of them.

## Layer 0 — chip/format unit tests

Pure logic, no CPU, no bus. `D64ImageTests` (directory parsing, sector math, `CreateEmpty`),
`PetTapFileRealFixtureTests` (tape format, separate strategy doc TODO). Fast, isolated, first
line of defense for the on-disk format itself.

## Layer 1 — register-level bus integration

Drives the real memory-mapped PIA2/VIA registers through a full `PetMachine`, bypassing the
KERNAL entirely (LISTEN/TALK/SECONDARY bytes poked directly, matching what
`PetIeeeBusBindingTests` already proves at the bare-chip level) - proves the chip↔bus↔
`CbmDosEngine` wiring without needing real ROM code to drive it.

- `PetMachineTests.PollDiskActivity_ReportsRealIeee488Traffic_ThenClearsUntilTheNextByte`
- `tests/PetEmulator.Pet.Tests/Ieee488/PetIeeeBusBindingTests.cs` (PIA2/VIA-level, no PetMachine)
- `tests/PetEmulator.Pet.Tests/Ieee488/PetIeeeBusTests.cs` (bare `PetIeeeBus`, no chips)

## Layer 2 — real KERNAL/BASIC end-to-end

`tests/PetEmulator.Pet.Tests/CbmDos/PetDiskEndToEndTests.cs`. A fully booted machine, real BASIC
commands typed through the keyboard matrix (`TextTyper`), assertions on what a real PET user
would see on screen (or an independently re-opened `D64Image`) - never a peek into
`CbmDosEngine`'s own internals except where explicitly noted. This is the layer that actually
answers "does directory read / file read / file write work from the PET", and the one that
found three real protocol bugs Layer 0/1 couldn't see (all fixed except the last):

1. **PIA1 PA4 cassette sense never wired** (tape, not disk - see the "PRESS PLAY" fix earlier
   this session) - `PortAInput` hardcoded the sense bit high, so `LOAD` could never see a
   datasette's PLAY button. Fixed.
2. **`PetIeeeBus` write-handshake never released** - `AcceptHandshake()`/`CommandHandshake()`
   leave NRFD/DAV asserted with nothing to clear them again; a real listener's firmware would
   release them almost instantly, but our synchronous "process the byte the moment it lands on
   DIO" shortcut never modeled that at all. The KERNAL's own "wait for the listener to be ready
   before the next byte" poll (real IEEE-488 behavior) hung forever. Fixed: `ArmWriteAck()` +
   `Tick()` release NRFD/NDAC/DAV a few cycles after any listener-side write.
3. **`CbmDosEngine.CloseChannel()` not idempotent** - `PetIeeeBus` legitimately calls
   `IIeeeDevice.Close()` twice per ATN cycle (once automatically on the ATN-rising transition,
   once for the UNLISTEN/CLOSE-SA byte that follows) - real, expected bus behavior, not a bug in
   the bus. `CloseChannel()` wasn't safe to call twice: the second call reread the just-consumed
   filename bytes as a DOS command and overwrote the status channel with a bogus syntax error.
   Fixed with a `_channelOpen` guard.
4. **Known, unfixed: idle-release write corrupts multi-byte filenames.** `OnDioWrite`'s
   `DataOut` case treats *every* PIA2 Port B write as a delivered byte, with no gate on DAV
   actually being asserted first. The KERNAL's own between-byte "release DIO to idle" write
   (0xFF - "nothing driven") reaches `OnDioWrite` too and decodes to 0x00 after the bus's
   inversion, landing in the filename as a literal null: `LOAD"HELLO",8` reaches
   `CbmDosEngine` as `"H\0E\0L\0L\0O\0"` and never finds the file. The real fix is a state-machine
   change (capture a byte only on a genuine DAV-asserted edge, not on every raw port write) - a
   correctness-sensitive rewrite of the write path, not a one-line patch, so it's left as a
   documented, precisely-diagnosed gap (see `PetDiskEndToEndTests.KnownFilenameByteCorruptionBug`)
   rather than a guessed quick fix. It only affects the *write* (LISTEN) direction with more than
   one meaningful byte, which is why:

| Test | Status | Why |
|---|---|---|
| `SaveCompletesAndReturnsToReady` | **passes** | SAVE only writes (no TALK/read), and its own success path never depends on the filename search succeeding *before* KERNAL reports done - real disk write, real completion signal (second `READY.`, no error text) |
| `LoadReadsARealFilesBytesCorrectly` | `[Explicit]` | hits bug 4 directly - `LOAD"HELLO",8` can never find the file |
| `SaveThenLoadRoundTripsARealProgramThroughARealDisk` | `[Explicit]` | the SAVE half works; the reload's `LOAD"TESTFILE",8` half hits bug 4 |

Run the `[Explicit]` tests deliberately (`dotnet test --filter "FullyQualifiedName~LoadReads"`,
e.g.) once bug 4 is fixed - they're real assertions, not placeholders, just gated off the default
run until they can pass.

## Layer 3 — scripted smoke tests

`scripts/pet-disk-loading.dbg` + `scripts/test-pet-disk-loading.sh` (mirroring
`scripts/pet-tape-loading.dbg`/`test-pet-tape-loading.sh`): attach a real fixture, run a bounded
trace, grep the log for expected lines. Shallow by design (mount + a few dozen instructions, not
a full BASIC boot) - a fast manual/CI sanity check, not a substitute for Layer 2's real
assertions.
