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

`Vic20Datasette` now takes both `MOS6522` instances (`via1` for motor/sense, `via2` for
read/write) - see its own doc comment for the full detail.

## A real bug found along the way: `MOS6522.CA1` edge visibility

`Vic20Datasette`'s pulse idiom forces CA1 `true → false → true` all within one `Tick()` call (a
real edge-sensitive input only cares about the transition, not how long the level holds) - the
same idiom `PetDatasette` already used successfully against `Pia.CA1`, whose setter does
synchronous edge-detection inline. `MOS6522.CA1` was a plain property - edge detection only
happened later, inside `UpdateControlInputs()`, itself only reachable via an explicit
`Update()`/`Tick()` call, called once per *instruction* in `Vic20Machine.StepInstruction` (batched
`_via1.Tick(cycles)`/`_via2.Tick(cycles)` before the per-cycle datasette loop). A pulse produced
entirely within that trailing per-cycle loop was therefore never sampled by any `Update()` call at
all - confirmed empirically (0 edges observed on VIA1 across a whole tape's worth of pulses,
independent of the wiring question above). Fixed by giving `MOS6522.CA1`'s setter the same
synchronous edge-detection `Pia.CA1` already had - matches real 6522 hardware too. 16/16 existing
`MOS6522Tests` pass unchanged (only two tests and this class ever wrote to `MOS6522.CA1` directly -
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

## Write (SAVE): closed - a virtual save, not a literal analog capture

First attempt: `Vic20Datasette.BeginRecording`/`RecordedPulseCycles` watched VIA2 PB3 directly and
recorded every falling edge's cycle gap. The wiring behind it was real and confirmed (SAVE
genuinely toggles PB3 through a real, found-in-disassembly routine, `TPTOGLE`: it tests the LSB of
the tape write byte and sets VIA2 Timer2 to `$60` (96 cycles) for a 0 bit or `$B0` (176) for a 1,
toggling PB3 on every T2 underflow - a plain two-level FM/biphase encoding, not the Kansas-City
three-symbol scheme PET/LOAD use). It never reliably round-tripped: the recorded stream's value
histogram had real clusters well outside the ~192/~352-cycle periods that math predicts (interrupt
-dispatch jitter on literally every single bit-toggle interrupt was enough to blur it), and no
noise-filtering heuristic tried got a captured recording to decode cleanly back through `LoadTape`.

**The fix: don't capture the analog signal at all - snapshot the logical content instead.** The
real KERNAL's `TAPE` dispatcher redirects the IRQ vector (`$0314`/`$0315`, `CINV`) to its own
tape ISR for the duration of an operation, and restores it when done - LOAD's redirect target is
`READT` ($F98E on this ROM), SAVE's is `WRTZ` ($FCA8, confirmed both via the real disassembly's
`IRQVCTRS` table AND empirically, reading `$0314` across a real boot/LOAD/SAVE). The **instant**
`Vic20Machine.StepInstruction` sees `$0314` become `WRTZ`'s address, the real KERNAL has already
built a genuine 192-byte header block in RAM (type, load address, filename - at the buffer zero
-page `TAPE1` ($B2/$B3) points at, built by SAVE's own header-write routine) and hasn't touched the
program bytes it's about to save yet, so both are safe to read directly, no waiting required.
`Vic20Machine.CaptureSaveIfDispatched` does exactly that: reads the header, reads the payload
(`header.StartAddress..EndAddress`), re-encodes both through `Vic20TapeEncoder` (the exact same
`PetTapeCassetteFormat` pulse train LOAD already decodes reliably), and replaces
`Datasette`'s content via `Vic20Datasette.ReplaceContentAfterSave` (not `LoadTape` - see that
method's doc comment for why: `LoadTape` releases PLAY, matching a human swapping in a physically
different tape, which isn't what a real SAVE writing onto the SAME tape already in the deck does).

Proven, not just plausible - `Vic20MachineTapeTests.SaveThenLoadRoundTripsTheRealProgramBytes`
types a real program, SAVEs it, **overwrites the target RAM with `$FF`** (so a match can only mean
LOAD genuinely rewrote it - an earlier draft of this test skipped that step and passed even when
LOAD silently never ran, the exact false-positive shape this session's own methodology elsewhere
warns about), power-cycles the machine (`Reset()` - the datasette's content survives, matching a
real tape still in the deck), and LOADs it back: the bytes match exactly.

Trade-off, stated plainly: this doesn't reproduce the real ROM's own analog write timing
bit-for-bit - a captured recording made by literally watching PB3 (as the first attempt did) would
still be a more "faithful" artifact if that's ever needed. What it does guarantee is what actually
matters for a working emulator: a real, unmodified SAVE followed by a real, unmodified LOAD
reliably gets you your program back.

## New Tape

"New Tape" (File menu, `Vic20MachineViewModel.NewTape` / CLI `new-tape [name]`) puts a fresh,
empty, writable tape in the deck via `Vic20Datasette.NewBlankTape` - zero pulses, but a real name,
so `Vic20DatasetteStatus`/`Vic20MachineViewModel.TapeLoaded` (both keyed off `TapeName`, not
`HasTape` - see `TapeName`'s doc comment) show it as present ("New Tape - press play"), not "No
tape". Press play, type a program, `SAVE`, then `LOAD` it straight back - the flow
`Vic20MachineTapeTests.SaveThenLoadRoundTripsTheRealProgramBytes` proves end to end.

## GUI

`Vic20MachineViewModel` mirrors `PetMachineViewModel`'s tape widget exactly: `TapeLoaded`/
`TapePlaying`/`TapeIconBrush`, `PlayTape`/`StopTape`/`EjectTape` commands, `LoadTape(path)`, plus
`NewTape()` (PET doesn't get a menu entry for this - it has no SAVE emulation to make a blank tape
useful for). `Vic20MachineView.axaml` has the same dedicated 📼▶⏹ icon+buttons block
`PetMachineView.axaml` does (plus the generic `Devices` bar, which filters the datasette back out
same as PET's). `MainWindowViewModel.LoadTape`/`NewTapeCommand` dispatch to whichever machine type
is current. Verified via `PetEmulator.Screenshot`: the icon row renders identically to PET's
(minus PET's disk icon).

## API surface

- `Vic20Machine.Datasette` (`Vic20Datasette`) - `LoadTape`/`NewBlankTape`/`PressPlay`/`Stop`/
  `Eject`/`Rewind`, `MotorOn`/`Sense`/`HasTape`/`TapeName`/`IsAtEnd`.
- `Vic20Machine.Devices` - includes a `Vic20DatasetteStatus` entry (mirrors `PetDatasetteStatus`).
- CLI (`Vic20DebuggerSession`): `tape <path>`, `new-tape [name]`, `play`, `stop`, `eject`,
  `devices` - same commands/output shape as `PetDebuggerSession`'s (`new-tape` has no PET
  equivalent - PET has no SAVE). `scripts/vic20-tape-loading.dbg` +
  `scripts/test-vic20-tape-loading.sh` mirror PET's tape smoke test exactly.
