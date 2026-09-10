# VIC-20 datassette

Asked directly: "dodaj magnetofon do vic-20, przetestuj odczyt tasmy testowej" (add a datassette
to the VIC-20, test reading a test tape) - then, once the read side worked, "dodaj magnetofon
podobnie jak w PET do ekranu VIC-20, póżniej wygeneruj syntetyczną taśmę testową albo pobierz
istniejacą, póżniej wykonaj testy e2e odczytu i zapisu analogicznie do testowania w PET" (add the
GUI widget too, build/obtain a test tape, and prove read AND write end-to-end).

Mirrors PET's tape stack (`PetEmulator.Pet.Tape.*`) - same cassette pulse encoding, same lifecycle
(`LoadTape`/`PressPlay`/`Stop`/`Eject`/`Rewind`/`Tick`), different chip and pins. Unlike PET, this
repo has no tape WRITE/SAVE emulation for either machine before this session - built from scratch
here (see "Write (SAVE)" below), one wiring layer at a time.

## Wiring: a wrong guess, then the real thing

The first pass wired everything to VIA1 (CA1 = read, CA2 = motor, PA7 = sense), sourced from a
WebSearch summary. `Datasette.MotorOn` came back true and the KERNAL printed `SEARCHING`, which
looked like real progress - but a synthetic tape only ever played fully through without the
KERNAL ever decoding a byte, and `BusObserver`/PC-hotspot tracing during the stall never touched
the real tape-dispatch address this session eventually found in real disassembly. **That WebSearch
answer was wrong.**

The fix came from pulling a REAL, symbol-labeled VIC-20 ROM disassembly directly (Lee Davison,
2005-2012, with Simon Rowe's enhancements - `curl`'d from a public gist, not summarized through a
search snippet) and reading the actual VIA register-bit tables and the real tape-read/write ISRs.
The real wiring:

- **VIA2 CA1** ($912C PCR bit 0) = cassette **read** line - NOT VIA1. VIA1's CA1 is the
  [RESTORE] key. Every read pulse this session's first pass raised on VIA1 was landing on an
  unrelated key line and never reached the real tape-read ISR at all.
- **VIA1 PA6** ($9111/$911F bit 6, active low) = cassette switch **sense** - NOT PA7. Confirmed by
  the real IRQ handler: `LDA VIA1PA2; AND #$40; BEQ ...` branches on bit 6.
- **VIA1 CA2** (PCR bits 1-3, manual-output mode, bit 1 = level, active low) = cassette **motor**
  control - this one was already right.
- **VIA2 PB3** ($9120 bit 3, output) = cassette **write** line - shares the physical pin with
  keyboard column 3 (a real hardware quirk, not a bug), driven by the real KERNAL under VIA2
  Timer2-interrupt timing rather than a level a caller just polls.

`Vic20Datasette` now takes both `Via6522` instances (`via1` for motor/sense, `via2` for
read/write) - see its own doc comment for the full detail.

## A real bug found along the way: `Via6522.CA1` edge visibility

`Vic20Datasette`'s pulse idiom forces CA1 `true → false → true` all within one `Tick()` call (a
real edge-sensitive input only cares about the transition, not how long the level holds) - the
same idiom `PetDatasette` already used successfully against `Pia.CA1`, whose setter does
synchronous edge-detection inline. `Via6522.CA1` was a plain property - edge detection only
happened later, inside `UpdateControlInputs()`, itself only reachable via an explicit
`Update()`/`Tick()` call, called once per *instruction* in `Vic20Machine.StepInstruction` (batched
`_via1.Tick(cycles)`/`_via2.Tick(cycles)` before the per-cycle datasette loop). A pulse produced
entirely within that trailing per-cycle loop was therefore never sampled by any `Update()` call at
all - confirmed empirically (0 edges observed on VIA1 across a whole tape's worth of pulses,
independent of the wiring question above). Fixed by giving `Via6522.CA1`'s setter the same
synchronous edge-detection `Pia.CA1` already had - matches real 6522 hardware too. 16/16 existing
`Via6522Tests` pass unchanged (only two tests and this class ever wrote to `Via6522.CA1` directly -
confirmed via `grep -rn "\.CA1\s*="` across `src`/`tests`).

## Read (LOAD): proven, byte-for-byte

With the wiring fixed, the exact same `PetTapeCassetteFormat` pulse encoding already used for PET
(352/512/672-cycle short/medium/long pulses - confirmed compatible: they fall squarely inside the
296-424/440-576/600-744 microsecond ranges the real KERNAL source documents for these pulse kinds)
worked immediately: a synthetic tape with a real header block (type, load address, filename) and
a payload reached `FOUND <filename>` and `LOADING`, and the payload bytes landed in RAM correctly.

One real quirk found along the way: the first 1-2 bytes of a data block occasionally decode wrong
(the adaptive pulse-timing calibration - `SVXT`/`CMP0` in the real disassembly - needs a byte or
two to settle right after a fresh sync-to-data-block transition). Real BASIC SAVEs are naturally
immune (their own first bytes are link-pointer/line-number filler, not meaningful content) - a
hand-built payload isn't, so `roms/vic20/test-tapes/hello-vic.tap` pads its payload with 3
sacrificial lead-in bytes. Verified stable across 3 repeated boot+LOAD attempts before being
committed (see `Vic20MachineTapeTests.LoadDecodesARealTapeFileByteForByte`, which asserts every
byte of the real payload (`HELLO VIC`) exactly, not just that the KERNAL reached some prompt).

## Write (SAVE): captures real data, round-trip not yet proven

`Vic20Datasette.BeginRecording`/`RecordedPulseCycles` watch VIA2 PB3 and record every **falling**
edge's cycle gap (matching the read side's own falling-edge convention - a rising edge is just the
brief strobe settling back high, not a real symbol boundary). Driving a real `SAVE` (no filename -
"file name is not required to SAVE to device 1" per the real KERNAL, which conveniently sidesteps
`Vic20HostKeyMap` not covering the quote key yet) against a tiny real BASIC program genuinely
produces tens of thousands of real pulses with a clean, uniform steady-state pattern once the
KERNAL's own leader tone gets going.

Two things aren't resolved yet:

- **Shared-pin noise.** PB3 is also keyboard column 3, so any recording armed before the real
  `SAVE` command's own `SEI` takes effect captures genuine keyboard-scan noise as large,
  irregular leading entries. Trimming through the last implausibly-large gap cleans this up, but
  that's a caller-side workaround, not something `Vic20Datasette` does automatically yet.
- **Round-trip.** Playing a trimmed captured recording back into a fresh machine's `LoadTape` does
  not yet decode correctly - `PetTapePulseDecoder` itself throws partway through on the captured
  stream ("expected a byte start marker, found (Medium, Medium)"), meaning the real write
  encoding's actual pulse-width distribution isn't a byte-for-byte match for the read side's
  Long/Medium/Short thresholds the way this session assumed. Not chased further this session -
  the read side (LOAD) was the priority and is fully proven; SAVE's wiring and capture mechanism
  are real and unit-tested (`Vic20DatasetteTests.Recording_CapturesOnlyFallingPb3EdgesAsFullPeriodCycleGaps`),
  but a genuine SAVE→LOAD round trip is a follow-up, not a claimed result.

## GUI

`Vic20MachineViewModel` mirrors `PetMachineViewModel`'s tape widget exactly: `TapeLoaded`/
`TapePlaying`/`TapeIconBrush`, `PlayTape`/`StopTape`/`EjectTape` commands, `LoadTape(path)`.
`Vic20MachineView.axaml` has the same dedicated 📼▶⏹ icon+buttons block `PetMachineView.axaml`
does (plus the generic `Devices` bar, which filters the datasette back out same as PET's).
`MainWindowViewModel.LoadTape` dispatches to whichever machine type is current. Verified via
`PetEmulator.Screenshot`: the icon row renders identically to PET's (minus PET's disk icon).

## API surface

- `Vic20Machine.Datasette` (`Vic20Datasette`) - `LoadTape`/`PressPlay`/`Stop`/`Eject`/`Rewind`,
  `BeginRecording`/`StopRecording`/`RecordedPulseCycles`, `MotorOn`/`Sense`/`HasTape`/
  `TapeName`/`IsAtEnd`/`IsRecording`.
- `Vic20Machine.Devices` - includes a `Vic20DatasetteStatus` entry (mirrors `PetDatasetteStatus`).
- CLI (`Vic20DebuggerSession`): `tape <path>`, `play`, `stop`, `eject`, `devices` - same commands,
  same output shape as `PetDebuggerSession`'s. `scripts/vic20-tape-loading.dbg` +
  `scripts/test-vic20-tape-loading.sh` mirror PET's tape smoke test exactly.
