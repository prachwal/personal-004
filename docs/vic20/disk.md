# VIC-20 IEC Disk

## IEC Wiring

The VIC-20 uses the bit-serial CBM IEC bus, not the PET's parallel IEEE-488
bus. The KERNAL shifts each byte LSB-first, eight bits per transfer.

| IEC signal | VIC-20 connection | Register or control bit |
| --- | --- | --- |
| ATN out | VIA1 PA7 | `$9111` / `$911F`, bit 7 |
| CLK in | VIA1 PA0 | `$9111` / `$911F`, bit 0 |
| DATA in | VIA1 PA1 | `$9111` / `$911F`, bit 1 |
| CLK out | VIA2 CA2 | `$912C` PCR, bit 1 |
| DATA out | VIA2 CB2 | `$912C` PCR, bit 5 |

There is no separate ATN input pin in this VIC-20 wiring. IEC lines are
active-low open-collector signals: zero asserts a line and one releases it.
The binding must therefore poll the VIA inputs and output control levels and
edge-detect them; `MOS6522` has no output-change event for CA2 or CB2.

The mapping is confirmed by the commented VIC-20 KERNAL disassembly:

- `vic-20-rom.asm`, lines 996-1006 and 1073-1083: VIA1 PA7 is ATN out,
  PA0 is CLK in, and PA1 is DATA in.
- `vic-20-rom.asm`, lines 1111-1117: VIA2 CA2 is CLK out and CB2 is DATA
  out through PCR bits 1 and 5.
- `vic-20-rom.asm`, lines 11163-11239 and 11433-11516: serial output and
  input shift eight bits LSB-first.
- `vic-20-rom.asm`, lines 11348-11376: `FCIOUT` feeds the serial sender.
- `vic-20-rom.asm`, lines 15124-15128: `SETLFS` stores the logical file,
  device number, and secondary address; disk device 8 is documented at lines
  15108-15118.
- `vic-20-rom.asm`, lines 11140-11142 and 11301-11305: ATN is asserted and
  released through VIA1 PA7.

Sources:

- [Lee Davison's commented VIC-20 ROM disassembly](https://gist.githubusercontent.com/cbmeeks/65c0f2acc1f0ad041c236637732aac8c/raw/8a13786a47f6cbbb8b86a465fce82500921a1ec0/vic-20-rom.asm)
- [VICE VIC-20 IEC implementation](https://github.com/VICEEmulator/VICE/blob/master/src/vic20/vic20iec.c)

The implementation must remain bit-serial and must not reuse the PET
`PetIeeeBus` byte-level DAV/NRFD/NDAC handshake.

## Debugging the real LOAD"$",8 stall

`Vic20DiskEndToEndTests` (Layer 2) hung on `LOAD"$",8` at "SEARCHING FOR $" -
diagnosed by instruction-level tracing (`IDebuggableProcessor.GetRegisters()["PC"]`
sampled from `Vic20Machine.BusObserver`, same technique as
`PetEmulator.Pet.Diagnostics.InstructionTracer`) against the real KERNAL
disassembly (`vic-20-rom.asm`, same source as above).

### Bug 1 (fixed): EOI-acknowledge pulse too narrow to poll

The KERNAL sends the OPEN filename byte deferred, flushed with EOI set by
`FUNLSN`→`LIST1`→`SRSEND` (`vic-20-rom.asm:11163`). The EOI path
(`LAB_EE5A`:11177, `LAB_EE60`:11186, `LAB_EE66`:11195) waits for the
listener to pulse DATA low then high again - a distinct "EOI acknowledge"
separate from the per-byte handshake. Instruction tracing showed the CPU
permanently parked at `LAB_EE60`'s `LSR` (PC=$EE63), reading VIA1 $911F
~150,000 times with the value never changing.

Root cause: `Vic20SerialBus.TickIncomingEoiProbe` fired the acknowledge pulse
(`_deviceDataLow = true`) for `BitPhaseCycles` (26 cycles) - a constant
meant for the *outgoing bit-transmission* phase, reused here by mistake.
`LAB_EE60`'s own polling loop (`JSR SERGET; LSR; BCS`) costs ~24 CPU cycles
per iteration, so a 26-cycle pulse is barely one iteration wide and can
land entirely between two samples - the KERNAL's own polling never
observes it, so `LAB_EE60` spins forever. Fixed by giving the acknowledge
pulse its own, wider constant (`EoiAcknowledgePulseCycles = 100`),
independent of the bit-transmission timing. Verified by rerunning the same
instruction trace: execution progresses past `LAB_EE60`/`LAB_EE66`, the full
filename byte shifts in, and the UNLISTEN sequence completes.

### Bug 2 (fixed): talker never pulses CLK to start the read phase

After bug 1's fix, `LOAD"$",8` progresses through the entire OPEN
(LISTEN+filename+UNLISTEN) sequence and into `FTALK`/`FTKSA` addressing
device 8 as talker for the directory read. It now hangs at `LAB_EEDD`
(`vic-20-rom.asm:11338`, inside "wait for bus end after send",
`LAB_EED3`:11333) - `JSR SERGET; BCS LAB_EEDD`, i.e. waiting for CLK to go
**low**. Confirmed by the same instruction-trace technique: PC=$EEE0/$EEDD
dominate essentially all instructions once the run budget is large enough
to reach this point (Layer 2 test's 2,000,000-instruction budget never gets
past it).

`LAB_EED3` calls `SEROUT0` (host asserts its own DATA line low) and never
releases it again anywhere in this sequence - `FACPTR` (`vic-20-rom.asm:11444`)
only calls `SEROUT1` (release) *after* observing CLK go high at
`LAB_EF21`:11449, i.e. after this exact wait already succeeded. This means
`Vic20SerialBus`'s `_outgoingAwaitingStart` trigger in `Tick()`
(`!_hostDataLow && CLK`) can never fire here, because `_hostDataLow` stays
permanently true through this whole phase - it is not the right condition to
gate the talker's first action on.

What was missing: `SelectDataMode()`'s `DataIn` branch (talker mode) had no
equivalent to `ArmIncomingAcknowledge()`'s immediate DATA-low pulse on the
listener side. Real IEC protocol has the newly-addressed talker pulse CLK
low briefly, unprompted, right after ATN releases, to acknowledge its
address - before the later per-byte ready/timeout handshake `FACPTR` itself
implements (`LAB_EF21` onward). Fixed by adding an analogous "talker
acknowledge" step (`_outgoingAcknowledgePulseCycles`,
`TalkerAcknowledgePulseCycles = 100`) fired from `SelectDataMode()` when it
selects `BusState.DataIn`, pulsing `_deviceClockLow` before
`_outgoingAwaitingStart`'s existing per-byte trigger runs at all. Verified:
execution now progresses past `LAB_EEDD` into `FACPTR`'s real bit-receive
loop (`LAB_EF58`/`LAB_EF66`) - confirmed via `Vic20SerialBus`'s own internal
tick trace, not just PC sampling: `_outgoingBitIndex` genuinely advances
0→7 and wraps to the next byte, CLK/DATA toggle at the expected cadence.
Three existing unit tests (`Vic20SerialBusTests.Talk_ShiftsLsbFirst...`,
`...Talker_WaitsForEoiAcknowledge...`, `Vic20SerialBusBindingTests.Bus_talker_levels...`)
encoded the old, ack-pulse-free timing and needed a matching
`TalkerAcknowledgeTicks`/100-tick advance inserted after entering talk mode -
updated alongside the fix, all green.

**A tried, reverted change**: widening `BitPhaseCycles` (the per-bit send
timing shared by the outgoing/FACPTR-read direction) from 26 to 100, on the
theory that the same one-iteration-wide polling aliasing that broke the EOI
pulse (bug 1) also affected ongoing bit transfer. It compiled, didn't move
the real hang any further, and broke three unit tests that encode the
original 26-cycle timing - reverted to 26. The bit-receive loop's own timing
is not the current bottleneck (see bug 3 below); don't re-attempt this
without new evidence.

### Bug 3 (open): hangs after the first few directory bytes, cause not yet found

With bugs 1 and 2 fixed, `LOAD"$",8` gets measurably further: the screen
shows `LOADING` (a KERNAL message that only prints once real transfer
starts), and `Vic20SerialBus`'s own tick trace shows multiple bytes
genuinely shifting through `_outgoingBitIndex` 0→7. But the real end-to-end
tests (`Vic20DiskEndToEndTests`) still fail - `LIST` shows no directory
content even after an 8,000,000 and separately a 50,000,000-instruction
budget (the latter ~2.5 real minutes), with the screen never advancing past
`LOADING` to `READY.` or an error message.

An earlier read of a truncated (6,000-tick-capped) internal trace looked
like a permanent stall with `_hostDataLow` stuck asserted after byte 3 -
that reading was wrong: the trace cap simply ran out mid-byte, and
`_hostDataLow` asserted-between-FACPTR-calls is real, expected protocol
(`FACPTR` reasserts it via `SEROUT0` at the end of each byte, releasing it
again via `SEROUT1` only at the *next* call).

**CbmDosEngine's own "$" directory-listing implementation was verified
correct, in isolation, bypassing the bus entirely** - driving
`PetIeeeDiskDrive` directly through the real LISTEN+filename+UNLISTEN+TALK
call sequence (`OpenForRead(0)`, `Write(0x24)` ('$'), `Close()`,
`OpenForWrite(0)`, repeated `TryRead`) produces a real, well-formed,
correctly-terminated 43-byte stream: `01 04 29 04 01 04 20 "BLOCKS FREE.
664" 20 "BLOCKS FREE. 664" 00 00 00` (load address $0401, one directory
line - the trailer only, since the test disk has zero files - both name-field
and suffix genuinely say "BLOCKS FREE. 664" by the real CBM DOS directory
format, not a duplication bug), `DataAvailable` correctly `False` only after
the last byte. **This rules out the DOS/command layer** - the answer to "is
the command implementation correct" is yes, confirmed in isolation.

That leaves the bug entirely in `Vic20SerialBus`'s bus/transport layer. A
zero-page trace of the real KERNAL's own bookkeeping (`STATUS`=$90,
`EAL`=$AE/$AF, `CNTDN`=$A5 - real addresses, confirmed against the
disassembly's own EQU table) during a full run showed `STATUS=$40` (bit6,
the EOI/timeout flag) **already set at the very first 1,000-instruction
sample after typing finished**, `EAL=$101B` (not a plausible BASIC load
address), and *zero further changes* to any of the three for the rest of a
3,000,000-instruction window - consistent with the CPU parking in the idle
keyboard-wait loop (`LAB_E5E8`/`GETQUE`, found earlier) having already left
FLOAD2 entirely. A parallel check confirmed *zero bytes* ever get written to
the $0401+ load buffer across a full 30,000,000-instruction run (tracked via
`BusObserver`, watching for writes in that range) - not slow progress, a
hard, total block on any byte ever being stored.

**That "premature EOI" hypothesis was tested directly and disproven.** Two
clean-room tests drive `Vic20SerialBus` (real `PetIeeeDiskDrive`/
`CbmDosEngine`, real 43-byte directory, no CPU involved) through the exact
real LISTEN+OPEN+filename+UNLISTEN+TALK+TKSA command sequence, reading bytes
with the same bit-accurate `Tick()`-driven helper the passing unit tests
already use:

- Idealized 2-ATN-cycle sequence (assert for LISTEN+OPEN, release for the
  filename byte, assert again for UNLISTEN+TALK+TKSA, release for the read).
- The *real* 3-ATN-cycle sequence, confirmed by tracing actual writes to
  `$911F`/`$9111` during a live CPU run (`W $911F=$C2` assert, `=$40`
  release, `=$C0` assert, `=$40` release, `=$C3` assert, `=$40` release -
  FUNLSN's own `LIST1`/`SCATN` pathway releases ATN right after UNLISTEN,
  before `FLOAD2` re-asserts it for TALK+TKSA - a transient `BusState.Idle`
  gap the earlier idealized sequence didn't have).

**Both reproduce the full, correct 43-byte stream, byte-for-byte identical
to the isolated-engine read, with EOI landing exactly on byte index 42 (the
last byte) - never early.** `Vic20SerialBus`'s command decode, ATN-cycle
handling (including the extra transient Idle gap), and EOI timing are all
provably correct for this exact protocol sequence, driven at idealized,
uniform tick cadence.

### Observability added: `Vic20SerialBus.Activity`

Mirroring `PetIeeeBus.Activity`/`IeeeBusActivity` (`line-change`/`byte`
events), `Vic20SerialBus` now raises `Activity` (`Vic20SerialActivity`) for
every ATN/CLK/DATA line change (host- and device-driven, each tagged), every
`BusState` transition, every decoded command byte, and every listen/talk data
byte (talk events include the EOI flag). Correlating this with real KERNAL
PC (`IDebuggableProcessor.GetRegisters()["PC"]`) during a live run is what
resolved the questions below - `PetIeeeBus` had this from the start;
`Vic20SerialBus` didn't, which is why this investigation needed throwaway
reflection-based instrumentation each time. Kept as a permanent, zero-cost
(nullable multicast delegate) addition.

### Real-CPU trace: the bus is correct even under live, variable-cost timing

Wiring `Activity` to a live `LOAD"$",8` run (not a clean-room replay) shows
**all 43 bytes delivered correctly, byte-for-byte identical to the isolated
tests, EOI on exactly the last one** - the earlier "premature EOI" and
"real-CPU-timing corrupts the bus" hypotheses are both wrong. Right after
the EOI byte, the log goes completely silent (no more `Activity` events at
all, confirmed unchanged between a 2,000,000 and a 25,000,000-instruction
run - not a budget issue, a hard stop) with `_outgoingAwaitingStart=true` and
its own trigger condition (`!_hostDataLow && CLK`) permanently satisfied -
this is `Tick()` calling `BeginOutgoingByte()` every single cycle, forever,
with `TryRead()` failing silently every time (buffer genuinely, correctly
exhausted) and producing no observable line change - not itself incorrect
per real IEC protocol (a talker with nothing left *should* go silent), but
it means correctness from here depends entirely on the KERNAL's own
Timer2-based EOF timeout in `FACPTR` (`LAB_EF29`/`LAB_EF2E`/`LAB_EF3C`).

Reading `Via2.Timer2Counter` during this exact silent window found it sitting
at ~13,000-16,000 and counting down normally (`ACR=$40`, PB6-pulse mode is
*not* selected, so the normal per-cycle countdown path is active) - nowhere
near the ~256 `FACPTR` arms via `STA VIA2T2CH,#$01`. That specific PC
(`LAB_EF29`) never appears in the post-EOI trace at all. **The CPU has left
`FACPTR`/`FLOAD2` entirely** and is running something else that happens to
also use VIA2's Timer2 for an unrelated purpose - consistent with the very
first PC-dominance finding in this investigation (`LAB_E5E8`/`GETQUE`, the
BASIC input-wait loop) being correct after all: the machine plausibly does
reach idle, just never visibly prints `READY.` first.

### A fix attempt that was wrong, caused a real regression, and was reverted

Hypothesis: `Vic20Machine` never drives VIA1's `CA1` pin (grep confirmed no
`_via1.CA1 = ...` anywhere), so IRQ-driven KERNAL bookkeeping controlled by
"clear CA1 interrupt" in the real IRQ handler (`LAB_FEC7`) never runs. Added
a `PetMachine.ClockPia1Cb1`-style periodic square wave on VIA1 CA1.

**This was wrong and reverted.** `Vic20Datasette`'s own doc comment (already
in this codebase, real-disassembly-sourced) states plainly: "VIA1's CA1 is
the [RESTORE] key" - not a jiffy-clock source at all. Driving it with a
periodic square wave spuriously toggles what the KERNAL reads as RESTORE-key
edges, and running the full suite with the change in place turned the 2
known disk-e2e failures into **4** failures - it broke
`Vic20MachineTapeTests.SaveThenLoadRoundTripsTheRealProgramBytes`, a
previously-passing, timing-sensitive tape test. Reverted immediately;
confirmed back to the clean 57/59 baseline afterward. Real VIC-20 hardware's
actual jiffy-clock source (most likely VIA1 Timer1 free-run, not CA1) was
not identified or implemented - this remains a genuine, still-open gap, but
CA1 is not where it lives.

### Bug 3 (fixed): missing 9th CLK pulse, then a deeper mid-transfer desync

The "CPU has left FACPTR entirely, reached GETQUE/idle" conclusion above was
**wrong** - caught by re-verifying on request ("sprawdź swoją diagnozę")
with a corrected diagnostic: PC plus a VIA1/VIA2 access log over a bounded
post-EOI window, instead of the single misleading `Via2.Timer2Counter`
reading that drove the earlier (wrong) conclusion. The CPU is genuinely still
inside FACPTR, parked at `LAB_EF66`.

**Missing 9th CLK-low pulse.** The real KERNAL bit-receive loop
(`vic-20-rom.asm:11483-11511`, `LAB_EF54`/`LAB_EF58`/`LAB_EF66`) does
"wait CLK high, sample bit" then **unconditionally** "wait CLK low" *before*
checking "8 bits done yet" - this wait-for-low runs after every bit,
including the 8th. `Vic20SerialBus.TickCore()` released CLK right after bit 8
and stopped there, leaving the KERNAL waiting for a falling edge it would
never see. Fixed by adding `FinalClockPulseCycles` (100 cycles, same
one-shot-pulse aliasing margin as bugs 1/2): the talker now pulses CLK low
once more after the 8th bit before settling into "awaiting next byte" (see
that constant's doc comment in `Vic20SerialBus.cs`). All three
`Vic20SerialBusTests`/`Vic20SerialBusBindingTests` cases that encode the old,
no-extra-pulse timing got a matching wait inserted; still green. **This fix
alone did not unblock the real end-to-end tests** - see below for why.

**The real remaining hang: a mid-transfer desync, not an EOI-byte problem.**
Correlating `Vic20SerialBus.Activity`'s byte-dequeue count with instruction-
level PC tracing (`Vic20Machine.BusObserver`, PC filtered to the
`$EE00-$EFFF` KERNAL-serial range, sampled every step) found the earlier
"stuck after EOI" reading was **also wrong**, for a subtler reason: the
device's Activity events only fire when `BeginOutgoingByte()` *dequeues* a
byte from the talker device - not when the CPU has actually finished
receiving it. `Vic20SerialBus`'s per-bit CLK/DATA toggling inside
`TickCore()` runs on a **blind fixed-cycle countdown** (`BitPhaseCycles`,
then 26) with zero feedback on whether the KERNAL's own polling loop
(`LAB_EF58`/`LAB_EF66`, each pass a variable number of real 6502 cycles -
branch/page-crossing jitter, not the idealized fixed-`Tick()`-count unit
tests) has actually sampled the current level. Because the device ticks
unconditionally, cycle-for-cycle, regardless of CPU progress, it can - and,
empirically, does - race ahead of a CPU that loses even one edge to jitter.

A windowed diagnostic (ring-buffer PC+Activity timeline, correlated byte
counters) caught this directly on a live 8.8M-instruction run: the CPU
permanently parks in `LAB_EF66` at instruction step 1,453,225, with the
bus's own dequeue counter at **42/43** at that exact moment - i.e. the CPU is
still working on byte 42 and never leaves this loop again. Yet by the end of
the same run the counter reads **43/43**, and a live reflection dump of
`Vic20SerialBus`'s internal fields shows `_outgoingAwaitingEoiAcknowledge=
true`, `_outgoingBitIndex=0`, CLK and DATA both released - the device raced
ahead, finished the whole transfer including byte 43's EOI dequeue, and is
now idling, waiting for a listener ACK that will never come because the
*listener* (the real KERNAL) is still stuck waiting for a CLK edge on byte 42
that already happened and was missed.

Root cause of `BitPhaseCycles = 26`: that value was borrowed from
`SRSEND`'s own bit-hold comment ("26 cycles, 23us on a PAL VIC") - but
`SRSEND` is the VIC's *outgoing* (VIC-to-drive) send routine, a different
direction from where this constant is actually used (`DataIn`/talker: the
emulated *drive* sending bytes to the VIC, read by `FACPTR`). A real 1541's
bit-clocking rate is drive-firmware timed, not bound to this VIC-side
constant - reusing it was a plausible-looking but never disassembly-verified
guess for this direction, and it left only 1-2 debounce-loop passes of
margin against real jitter.

**Fix: widen `BitPhaseCycles` from 26 to 100** (same margin as the other
three one-shot-pulse constants). Verified two ways:

- The same windowed PC+Activity diagnostic now reports no permanent
  `LAB_EF54-EF7F` stall at all (`stuckSinceStep=-1`) across an 15.8M-
  instruction run, and the captured screen shows a real, correct directory
  listing after `LOAD"$",8` + `LIST` (`SEARCHING FOR $` → `LOADING` →
  `READY.` → `LIST` → the disk's directory content), not a stall.
- Both real `Vic20DiskEndToEndTests` (`LoadDirectoryThenList_...`,
  `SaveNewLoadRun_...`) pass for the first time this session.

An earlier attempt to widen this same constant (to 100) was tried and
reverted mid-session as making "zero difference" - that test predates both
the `FinalClockPulseCycles` fix and this exact byte-42/43 desync being
isolated, and only checked real-e2e pass/fail (which was still blocked by
bug 3's missing pulse at the time) rather than this windowed instruction-
level trace. Re-verify with that same technique before ever changing this
constant again - a blind pass/fail check on this test suite cannot
distinguish "no effect" from "fixed one bug, still blocked by another."

`Vic20SerialBusTests`/`Vic20SerialBusBindingTests`'s hardcoded `26`-cycle
waits were updated to a shared `BitPhaseTicks = 100` constant matching the
new value; all 8 tests green.

### Bug 4 (fixed): directory listing never included the disk's name

With bug 3 fixed, `LoadDirectoryThenList_ShowsTheMountedDisksName` still
failed - not a hang, a content mismatch (test completes in ~5s, `LIST` shows
no trace of the disk's name). Root cause, found by dumping the actual post-
`LIST` screen text: `CbmDosEngine.GenerateDirectoryListing()`
(`src/PetEmulator.Pet/CbmDos/CbmDosEngine.cs`) jumped straight from the
`$0401` load-address bytes to file entries (then the `BLOCKS FREE` trailer),
**never emitting the real CBM DOS header line** (`0 "<name>" <id> <type>`)
that every real directory listing leads with. This is a DOS/content-
generation gap, shared by both the PET and VIC-20 disk drives (`CbmDosEngine`
is common code) - unrelated to the IEC transport-layer bugs above, and not
something a hang-focused instruction trace would ever surface.

Fixed by emitting that header line first, using `D64Image.DiskName`/
`DiskId` (already tracked by the image, just never read here), with the
real line-number field set to 0 (not a block count) as real listings do.
Purely additive at the front of the emitted byte stream - no existing
consumer asserts exact directory byte offsets (checked: neither
`PetDiskEndToEndTests` nor `D64ImageTests` do), so this could not silently
break PET's own directory listing. `impact()` flagged this symbol HIGH risk
(3 shared-engine callers, used by both PET and VIC-20 disk drives) - assessed
as safe because the change only prepends bytes, and confirmed via the full
test suite.

### Current status

Both real `Vic20DiskEndToEndTests` pass. `dotnet test` on the full solution
confirms no regressions elsewhere (see the session's final full-suite run).
