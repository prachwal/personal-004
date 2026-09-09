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

5. **Large real-file `LOAD` stalled at a fixed byte offset (326 of 4,486)** - root-caused with a
   new bus-level debugging tool, `PetMachine.BusObserver`/`PetMemoryBus.Observer` (a
   `Action<BusAccess>` fired on every real Read/Write, modeled on personal-002's
   `Z80Cpu.BusCycleObserver`; deliberately built at the `PetMemoryBus` level, not inside
   `src/PetEmulator.Cpu6502/`, so it needs none of that project's subagent routing) plus a full
   instruction-level PC+bus trace saved to disk around the exact stall. The trace showed the
   ~60Hz keyboard-scan IRQ (~550-600 instructions, 1,000+ cycles) landing mid-read and outlasting
   `PetIeeeBus`'s read-side settle delay (32 cycles): the emulation silently pre-fetched and
   re-asserted DAV for the *next* byte while the KERNAL's read-wait loop was still parked inside
   the ISR, permanently hiding the DAV release it needed to see on resume. Disassembling the real
   ACPTR/read-byte KERNAL routine (`da65` on the real `kernal-2.901465-03.bin`, $F18C-$F1D1)
   confirmed it has no SEI/CLI guard either - real hardware only avoids this race because a real
   drive's own controller firmware is far slower than either delay. Fixed by raising the settle
   delay (`PetIeeeBus.SettleDelayCycles`, used by both the read-side prefetch and
   `ArmWriteAck`'s write-side release) from 32 to 4,000 cycles - comfortably past a keyboard-scan
   ISR's worst case, comfortably under the ~16,667-cycle jiffy period so it doesn't perceptibly
   slow real transfers. A first attempted fix (make the read-side prefetch wait for a genuine
   `SetNdacAccepted`-driven acknowledgment instead of arming unconditionally) made things *worse*
   (stalled after 1 byte, not 325) and was reverted before this fix was found.

With all four fixed, SAVE, small LOADs, and large real-file LOADs now complete fully from real
BASIC - see `SaveCompletesAndReturnsToReady`, `SaveThenLoadRoundTripsARealProgramThroughARealDisk`,
and `LoadReadsARealFilesBytesCorrectly` (no longer `[Explicit]`; loads all 4,486 bytes of "HELLO"
from `games-1.d64` and checks every byte against an independently re-opened `D64Image`), all
genuinely passing. The round-trip test types a program, `SAVE`s it, `NEW`s memory clear, `LOAD`s
it back, `RUN`s it, and checks the real evaluated output on screen - proof the whole path works,
not just that the KERNAL stopped hanging. Note: `LoadReadsARealFilesBytesCorrectly` now needs a
generous instruction budget and `[CancelAfter]` (~1,250 instructions/byte at the fixed settle
delay) - the honest cost of a delay long enough to actually be safe, not a residual bug.

## Layer 3 — scripted smoke tests

`scripts/pet-disk-loading.dbg` + `scripts/test-pet-disk-loading.sh` (mirroring
`scripts/pet-tape-loading.dbg`/`test-pet-tape-loading.sh`): attach a real fixture, run a bounded
trace, grep the log for expected lines. Shallow by design (mount + a few dozen instructions, not
a full BASIC boot) - a fast manual/CI sanity check, not a substitute for Layer 2's real
assertions.
