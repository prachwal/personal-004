# VIC-20 datassette

Asked directly: "dodaj magnetofon do vic-20, przetestuj odczyt tasmy testowej" (add a datassette
to the VIC-20, test reading a test tape). Mirrors PET's own tape stack
(`PetEmulator.Pet.Tape.*`, see `docs/pet-debug-tools.md`) - same cassette pulse encoding, same
lifecycle (`LoadTape`/`PressPlay`/`Stop`/`Eject`/`Rewind`/`Tick`), different chip and pins.

## Wiring (confirmed, not guessed)

VIC-20 cassette hardware sits on VIA1 ($9110), confirmed via WebSearch (c64-wiki, RetroIsle VIC-20
memory maps) and cross-checked against `docs/vic20-disassembly/kernal.asm`'s real PCR/IER writes
(`$911C`/`$911E`, matching a CA1-edge-interrupt setup):

- **CA1** (input): cassette read line - one pulse-boundary edge per bit-cell transition.
- **CA2** (output): cassette motor control, active low - manual-output PCR mode (`$0C` = on,
  `$0E` = off), same convention as PET's PIA1 CB2.
- **PA7** (input): cassette switch sense, active low - same convention as PET's PIA1 PA4.

Empirically confirmed against the real KERNAL: typing `LOAD` immediately reports
`Datasette.MotorOn == true` (real KERNAL turns the motor on right away, before it even needs
PLAY) and the real ROM prints `SEARCHING` (`Vic20MachineTapeTests
.LoadWithATapeAttached_TurnsTheMotorOnAndReachesSearching`).

## Real bug found and fixed along the way: `Via6522.CA1` edge visibility

`PetDatasette`'s pulse idiom forces CA1 `true → false → true` all within one `Tick()` call (a
real edge-sensitive input only cares about the transition, not how long the level holds). PET's
`Pia.CA1` setter already does synchronous edge-detection inline, so this works. `Via6522.CA1` was
a plain property (`get => _ca1; set => _ca1 = value;`) - edge detection only happened later,
inside `UpdateControlInputs()`, itself only reachable via an explicit `Update()`/`Tick()` call.
Since `Vic20Machine.StepInstruction` calls `_via1.Tick(cycles)` **once per instruction**, batching
all of that instruction's cycles, and only *then* runs the per-cycle datasette loop afterward, a
`true → false → true` sequence produced entirely within that trailing loop was never sampled by
any `Update()` call at all - CA1 always read back `true` by the time VIA1 got around to checking
it, so it never saw a single edge (confirmed empirically: 0 edges detected on VIA1 over an entire
tape's worth of pulses, despite `IsAtEnd` correctly reaching `true`).

Fixed by giving `Via6522.CA1`'s setter the same synchronous edge-detection Pia's already had
(matches real 6522 hardware too - an edge-sensitive input is asynchronous, not sampled only at
whatever cadence software happens to call `Update()`). Confirmed safe: `Via6522.CA1` was written
to only by two existing `Via6522Tests` cases (both already followed with an explicit `Update()`,
so they see the same result either way) and this new `Vic20Datasette` - `grep -rn "\.CA1\s*="`
across `src`/`tests` found nothing else. 16/16 `Via6522Tests` pass unchanged after the fix; the
new `Vic20DatasetteTests` (mirroring `PetDatasetteTests`' shape - motor gating, edge timing,
sense, eject/rewind, all pure unit-level against a bare `Via6522`) are 9/9 green.

## What ISN'T verified: real KERNAL byte-level decode of a synthetic tape

PET's own tape stack was verified against a **real captured Datasette recording**
(`roms/pet/test-tapes/tower-and-dragon-town.tap`) - even PET's own end-to-end test
(`PetMachineTests.PressPlay_LetsLoadProceedPastThePressPlayPrompt`) only proves the KERNAL reaches
`SEARCHING` after `PRESS PLAY`, not a full byte-for-byte decode into RAM.

This session tried to go one step further for VIC-20 (build a synthetic tape - `PetTapeCassetteFormat`'s
352/512/672-cycle pulse widths, a header block, a tiny raw-byte payload - and prove the real ROM
decodes it into RAM), the same technique `PetEmulator.Pet.Tests.Tape.PetTapeTestEncoder` uses for
PET's own pulse-format unit tests. It didn't converge: the tape played through fully (every pulse
consumed, `IsAtEnd` true, CA1 edges genuinely reaching VIA1 after the fix above), the KERNAL never
progressed past `SEARCHING` and RAM was never written. A `BusObserver`/PC-hotspot trace during
playback never once reached the address this session identified in `kernal.asm` as the real
CA1-interrupt tape dispatch (`lda $911D` / `and #$02` at `$FEAD`, disassembly around line 4381) -
whatever loop the ROM is actually spending time in during `SEARCHING` was not that one, and without
symbol-annotated disassembly or a real reference `.tap` file to check against, pinning down the
exact real bit-cell timing (or entry point) this specific KERNAL revision expects wasn't decisively
resolved in this session.

**Net effect**: the mechanism (motor/sense/CA1-edge wiring, pulse sequencing, lifecycle) is real
and tested; a synthetic tape's *exact bytes* landing in RAM through the real ROM's own decoder is
not proven. Should this matter later, the fastest path is the same one that cracked PET's tape
support and this session's own PET cursor-address bug: get a **real captured VIC-20 `.tap` file**
(same convention as `roms/pet/test-tapes/`) and diff its actual timing/structure against what
`PetTapeCassetteFormat` assumes, rather than more blind disassembly reading.

## API surface

- `Vic20Machine.Datasette` (`Vic20Datasette`) - `LoadTape`/`PressPlay`/`Stop`/`Eject`/`Rewind`,
  `MotorOn`/`Sense`/`HasTape`/`TapeName`/`IsAtEnd`.
- `Vic20Machine.Devices` - now includes a `Vic20DatasetteStatus` entry (mirrors
  `PetDatasetteStatus`), so the Desktop status bar and CLI `devices` command both pick it up with
  no further wiring.
- CLI (`Vic20DebuggerSession`): `tape <path>`, `play`, `stop`, `eject`, `devices` - same commands,
  same output shape as `PetDebuggerSession`'s.
