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
answers "does directory read / file read / file write work from the PET", and the one that found
four real protocol bugs Layer 0/1 couldn't see (three fixed, confirmed against
[VICE](https://github.com/libretro/vice-libretro)'s real `src/parallel/parallel.c` - the
reference PET/CBM emulator's IEEE-488 bus implementation):

1. **PIA1 PA4 cassette sense never wired** (tape, not disk - see the "PRESS PLAY" fix earlier
   this session). Fixed.
2. **`PetIeeeBus` write-handshake never released** - `AcceptHandshake()`/`CommandHandshake()`
   left NRFD/DAV asserted with nothing to clear them again; the KERNAL's own "wait for the
   listener to be ready before the next byte" poll (real IEEE-488 behavior) hung forever. Fixed:
   `ArmWriteAck()` + `Tick()` release NRFD/NDAC/DAV a few cycles after any listener-side write,
   mirroring `Tick()`'s existing read-side `_dataInFetchDelay`.
3. **`CbmDosEngine.CloseChannel()` not idempotent** - `PetIeeeBus` legitimately calls
   `IIeeeDevice.Close()` twice per ATN cycle (once automatically on the ATN-rising transition,
   once for the UNLISTEN/CLOSE-SA byte that follows - both real, expected bus behavior). The
   second call reread the just-consumed filename as a DOS command and overwrote the status
   channel with a bogus syntax error. Fixed with a `_channelOpen` guard.
4. **Idle-release write corrupted multi-byte filenames** - `OnDioWrite`'s `DataOut` case treats
   every PIA2 Port B write as a delivered byte with no gate on DAV actually being asserted.
   Comparing against VICE's real `parallel.c` confirmed the textbook-correct fix (capture a byte
   only on a genuine DAV-asserted edge, `In1_DAV_true` in VICE's own state machine) - but VICE
   *also* has a dedicated `OldPet` state distinguishing PET 2001/3000-class hardware's own
   simpler, non-DAV-driven KERNAL write routine from the standard one, and this repo's BASIC 2
   KERNAL genuinely never toggles PIA2 CB2 (DAV out) per filename byte in practice (confirmed:
   CRA/CRB were written exactly once, to a static "idle high" value, for an entire filename
   transfer) - it paces itself with fixed write/idle-release cycles instead, exactly the "old
   PET" quirk VICE special-cases. A real PETSCII filename never contains a literal 0x00 byte, so
   `CbmDosEngine.ReceiveByte` drops one while `_waitingForFilename` is true (never during SAVE's
   actual data phase, where 0x00 is a legitimate BASIC line terminator) - fixed, narrowly scoped,
   with the ambiguity documented in place.

With all three fixed, SAVE and small LOADs (a reloaded few-line BASIC program) now complete
fully from real BASIC - see `SaveCompletesAndReturnsToReady` and
`SaveThenLoadRoundTripsARealProgramThroughARealDisk`, both genuinely passing (the round trip
types the program, `SAVE`s it, `NEW`s memory clear, `LOAD`s it back, `RUN`s it, and checks the
real evaluated output on screen - proof the whole path works, not just that the KERNAL stopped
hanging).

**Still open**: loading a large real file (`LOAD"HELLO",8`, a 4,486-byte PRG from
`games-1.d64`) stalls partway through - progress was tracked at a fixed byte offset (326) that
never advances even after tens of millions of further instructions, not merely slow. Root cause
not yet identified; `LoadReadsARealFilesBytesCorrectly` is `[Explicit]` documenting exactly this.
Small transfers never reach whatever triggers it.

## Layer 3 — scripted smoke tests

`scripts/pet-disk-loading.dbg` + `scripts/test-pet-disk-loading.sh` (mirroring
`scripts/pet-tape-loading.dbg`/`test-pet-tape-loading.sh`): attach a real fixture, run a bounded
trace, grep the log for expected lines. Shallow by design (mount + a few dozen instructions, not
a full BASIC boot) - a fast manual/CI sanity check, not a substitute for Layer 2's real
assertions.
