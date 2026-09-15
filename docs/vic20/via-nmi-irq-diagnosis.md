# VIC-20 VIA/NMI investigation - diagnosis, fixes, test results

Started from a user report: "gry nie działają, brakuje np. dźwięku albo obrazu" (games don't work,
missing sound or image). This document records the full investigation - what turned out to be real
bugs (fixed, committed), what turned out to be correct behavior misread as a bug, and the test
infrastructure built along the way to verify all of it against real hardware, not just "looks ok".

## Methodology

Every test in this document was run headless, through the repo's own scripted debugger - no GUI,
no manual keypresses, fully reproducible from a text file:

```bash
dotnet run --no-build --project src/PetEmulator.Cli -- vic20-debug <script.txt>
```

`Vic20DebuggerSession` (script commands) delegates anything it doesn't recognize to the generic
`MachineDebugger` (`trace`/`watch`/`dump`/...) - see `.claude/skills/gitnexus-*` /
`src/PetEmulator.Cli/Vic20DebuggerSession.cs` for the full command reference.

**Loading script template** (parameterize `<disk.d64>` and `<PROGRAM>`; this exact shape was used
for both `via_pb7` and `via_t1irqack`):

```text
roms roms/vic20
disk roms/vic20/test-disks/<disk.d64>
trace 200000
type LOAD"<PROGRAM>",8,1
key 1 7 down
trace 15000
key 1 7 up
trace 3000000
dump 1E00 1E80
type RUN
key 1 7 down
trace 15000
key 1 7 up
watch 911D
trace 500000
status
dump 1E00 1F60
```

Why each piece matters (each was a real bug found the hard way - see §6 below for the two that
became actual fixes):

- **`trace 200000` before typing anything.** The machine boots from a cold reset; the KERNAL's own
  init (ROM checksum, RAM-size scan, VIC/screen setup) takes roughly 130,000-175,000 instructions
  before the keyboard scan is even live. Typing before that point gets silently dropped - the
  machine simply isn't listening yet.
- **`type LOAD"..."` then a separate `key 1 7 down` / `trace 15000` / `key 1 7 up` for Enter**,
  instead of embedding a newline in the typed text. `Vic20DebuggerSession`'s script reader splits
  the script file on newlines, so a literal `\n` inside a `type` argument can't survive being
  written into a script file - Enter has to be a distinct matrix keypress. `(1, 7)` is the real
  VIC-20 RETURN key's matrix position (`Vic20HostKeyMap.ReturnRow/ReturnCol` -
  `docs/vic20/rendering-fixes.md`'s Bug 5). `trace 15000` between down/up gives the KERNAL's jiffy
  keyboard scan (~2,100 instructions/scan) several full scans to actually see the key held, instead
  of releasing it before any scan happens.
- **`trace 3000000` after Enter, before checking the screen.** A real IEC serial LOAD is slow by
  design (byte-by-byte handshaking) - a ~1KB test program can take a few million instructions to
  finish loading. Checking too early just shows `SEARCHING FOR ...` instead of a completed load.
- **`type LOAD"..."`'s own per-character pacing** comes from `Vic20TextTyper.Type`'s default
  `holdInstructions`/`gapInstructions` (12,000 as of this session - see §6) - too small a value
  here reproduces the exact "dropped character mid-filename" bug documented below.
- **`watch 911D` before the final `trace`**, not a one-shot `dump` afterward. `MachineDebugger`'s
  `dump` command reads memory the same way running code does - reading certain VIA registers (T1
  counter low, in particular) clears their own interrupt flag as a side effect
  (`MOS6522.ReadTimer1Low`). A `dump` placed between two other register reads can silently consume
  a flag the next line of the script was trying to observe. `watch` on the *flag* register itself
  (`$911D`/`$912D`, plain reads, no side effect) avoids that trap entirely - this cost real time to
  discover during the Alphoids investigation (§4) before it became the default approach here.

## Summary

| Finding | Verdict | Status |
| --- | --- | --- |
| VIA1 interrupts routed to IRQ instead of NMI | **real bug** | fixed, `fix/vic20-via1-nmi-irq` commit `c012e5c` |
| `SetNMI` polarity backwards on first attempt | caught before commit | fixed in the same commit |
| `MOS6522` PB7 one-shot toggled forever instead of a single pulse | **real bug** | fixed, same commit |
| `PetEmulator.Screenshot`'s routed `--type` hung for hours | **real bug** (Avalonia Headless dispatcher hazard) | fixed, commit `e5bc64c` |
| "Alphoids" cartridge freezes at `$1242` forever | **not a bug** | it's a real "press any key" wait loop (`$CB`, real KERNAL keyboard-scan variable) - resolved by pressing a key |
| Joystick simulation didn't unstick Alphoids | expected | Alphoids waits on the **keyboard**, not the joystick |
| `Vic20TextTyper`'s default hold/gap (5,500 instructions) too thin | **real bug** | default bumped to 12,000 (uncommitted) |
| `via_pb7` VICE regression test vs real-hardware reference | **partial mismatch found** | open - cycle-interleaved VIA ticking implemented for VIC-20 (§9), zero regressions, but this specific mismatch is unchanged; needs its own investigation |
| `via_t1irqack` (`BANDITS-VIA1`) vs real-hardware reference | was: IFR sticks at `$C0` forever instead of cycling `C0 C0 00 C0 00 00` | **fixed** (§10) - `MOS6522` T1-latch-high write now clears the T1 interrupt flag, matching VICE's `viacore.c` and real hardware; IFR now cycles `00/C0` and screen output shows the expected mixed reversed/non-reversed pattern |
| $9000/$9001 (VIC origin X/Y) never read by the renderer | documented gap | not fixed (rare in practice) |
| No CPU cycle-stealing during VIC display fetch | confirmed **correct** (matches VICE - real VIC-20 has no C64-style "bad lines") | no action needed |
| Printer / RS-232 modem support | genuinely absent | not started |

## 1. VIA1 → NMI, not IRQ (fixed)

Real VIC-20 hardware routes VIA1's interrupt output to the 6502's **NMI** pin (RESTORE key, and
any software timing that must run regardless of the I flag); VIA2 and cartridge expansion devices
share the ordinary maskable **IRQ** line. Confirmed against VICE's own C source:

```c
// vic20via1.c
via->irq_line = IK_NMI;
// vic20via2.c
via->irq_line = IK_IRQ;
```

`Vic20Machine.StepInstruction` folded both VIAs into one `_cpu.SetIRQ(...)` call - VIA1-driven
interrupts were silently maskable and could be reordered/dropped relative to VIA2's, instead of
always preempting like real NMI does. Fixed to:

```csharp
_cpu.SetNMI(!_via1.IRQ);   // see polarity note below
_cpu.SetIRQ(_via2.IRQ || _memoryBus.ExpansionIrq);
```

**Polarity gotcha caught during the fix**: this CPU core's `SetNMI` has the *opposite* boolean
convention from `SetIRQ` - confirmed by `Cpu6502.Tests/InterruptTests.Nmi_TriggersOnFallingEdge`
(`SetNMI(true)` = pin idle/high, `SetNMI(false)` = pin asserted/low, latching on the true→false
transition). The first attempt passed `_via1.IRQ` straight through and was backwards; fixed to
`!_via1.IRQ`. `Cpu6502.PublicMethods.cs`'s own XML doc on `SetNMI` was stale (claimed the same
polarity as `SetIRQ`) - corrected in the same commit.

Renamed/re-asserted `Vic20MachineTests.Via1TimerIrq_IsPropagatedToThe6502IrqVector` to
`...NmiVector`, now checking `$FFFA`/`$FFFB` instead of `$FFFE`/`$FFFF`.

## 2. MOS6522 PB7 one-shot pulse (fixed)

VICE's `viacore.c` stops processing T1 underflows entirely once a one-shot interrupt has fired
(`alarm_unset` in the one-shot branch) - so PB7 (`ACR` bit 7 pass-through) gets exactly one
low-to-high pulse per timer load, not a continuous square wave. This repo's `MOS6522.UpdateTimers`
toggled `_t1Pb7` unconditionally on every underflow regardless of mode, so one-shot PB7 kept
producing a continuous wave forever after the first firing. Fixed: the toggle now only happens in
the free-run branch; the one-shot branch sets `_t1Pb7 = true` once and leaves it (matches "goes low
on load, high on time-out, stays high until reloaded").

## 3. `PetEmulator.Screenshot` routed-input hang (fixed)

`window.KeyPress`/`KeyRelease` (Avalonia.Headless) internally call `HeadlessWindowExtensions`'s own
`RunJobsOnImpl`, which itself calls the same unbounded `Dispatcher.UIThread.RunJobs()` this file's
own comment already warned about - a live, self-re-enqueuing `DispatcherTimer` means the job queue
is never empty, so that call never returns. `viewModel.Dispose()` (which stops
`MainWindowViewModel`'s 20ms render timer) only ran at the very end of `Main`, after the `--type`
loop already had a chance to call `window.KeyPress` - so a real run sat **blocked since the
previous day** (near-zero cumulative CPU - genuinely blocked, not spinning) the first time `--type`
ran without `--direct` on a machine with real on-screen gameplay content.

Fixed by moving `viewModel.Dispose()` to run immediately after setup, before the tick loop and
before `--type` ever executes - this tool never needed the live timer anyway (it drives every
`Tick()` call manually). Verified: the exact repro now completes in ~2s instead of hanging,
producing the same machine state (PC/cycles/instructions) as the pre-existing `--direct` route.

## 4. The Alphoids "freeze" - not a bug

Traced with the CLI debugger (`vic20-debug`, `watch`/`dump`/`trace`): the cartridge boots, VIC
registers get configured correctly, the screen renders real content - then the CPU sits in a tight
loop forever:

```asm
1242: A5 CB     LDA $CB
1244: C9 40     CMP #$40
1246: F0 FA     BEQ $1242
```

`$CB` is a **real KERNAL zero-page variable** (`docs/vic20/disassembly/kernal.asm:1676-1719`,
routine `LEB1E`/`LEB74`, called from the jiffy IRQ): the index of the last key found down by the
keyboard-matrix scan, `$40` meaning "nothing pressed". This is an ordinary "press any key" title
screen, not a hang. Simulating **joystick** input (fire + right, held from boot) had no effect,
because Alphoids is waiting on the **keyboard**, not the joystick - confirmed by simulating a real
key press (`key 1 1 down`) instead: `$CB` changed `$40 → $09` and the game immediately continued
into real gameplay code.

Screenshot captured post-fix showing the game actually running (score/lives HUD, player sprite,
joystick panel) - see session artifacts (not checked into the repo).

## 5. VIC $9000/$9001 origin registers - documented gap, not fixed

Cross-checked `vic-cycle.c` (VICE's cycle-based VIC-I core) in full: real hardware's `vic.regs[0]`
(X origin, bits 0-6 + bit7 interlace enable) and `vic.regs[1]` (Y origin, coarse `/2`) gate the
horizontal/vertical "flip-flops" that open the visible display window. This repo's `MOS6560`
latches both registers generically (`_registers[0x00]`/`[0x01]`) but no property or
`Vic20RasterDisplay` code ever reads them - the screen always renders at a fixed default origin.
Real games/demos that reposition the screen (rare, mostly demoscene split-screen tricks) would
render incorrectly. Not fixed - flagged as a known gap; VICE's own `VIC20/vic_9000test` (see below)
targets exactly this but needs +8K RAM and PAL, and is cycle-precise in a way this repo's simpler
per-instruction `Tick()` model may not reach.

Also confirmed (good news, not a bug): VICE's `vic_cycle()` never stalls the CPU for VIC fetches -
real VIC-20 has no C64-style "bad lines" DMA cycle-stealing. This repo's `MOS6560.Tick` (raster
counter only, CPU always runs full speed) already matches that.

## 6. Test infrastructure built this session

- **`roms/vic20/test-disks/vic20-via-pb7.d64`**, **`vic20-via-t1irqack.d64`** - real VICE-project
  regression-test disks (`github.com/libsidplayfp/VICE-testprogs`, `VIC20/via_pb7` and
  `VIC20/via_t1irqack`), picked because they exercise exactly the `MOS6522` behavior touched above.
  Both ship a hardware-verified reference table/expected output in their upstream `readme.txt`.
  See `roms/vic20/test-disks/README.md` for provenance and program names.
- **`dotnet run --project src/PetEmulator.Cli -- d64-dir <path.d64>`** - new CLI command, lists a
  `.d64`'s directory (name/type/size/lock) via the repo's own `D64Image.ReadDirectory()`, no machine
  needed. Replaces an earlier ad-hoc hand-decode of the CBM DOS directory sectors done in a
  throwaway Python script - verified names on both disks:
  - `vic20-via-pb7.d64`: `UNEXPANDED`, `EXPANDED`
  - `vic20-via-t1irqack.d64`: `BANDITS-VIA1`, `BANDITS-VIA2`, `BANDITS-VIA1-8K`, `BANDITS-VIA2-8K`
- **`Vic20TextTyper`'s default `holdInstructions`/`gapInstructions`** bumped `5_500 → 12_000`.
  Root cause of the earlier "LOAD gets truncated" symptom (documented separately - see below): a
  jiffy keyboard-scan period is ~6,900 cycles ≈ ~2,100 instructions at this repo's average
  cycles/instruction ratio, so 5,500 instructions of hold/gap left too thin a margin around the
  KERNAL's own debounce loop (`LEB40`) - one character (`P` in `UNEXPANDED`) was dropped even after
  the machine had fully booted. `tests/PetEmulator.Vic20.Tests/Serial/Vic20DiskEndToEndTests.cs`
  had already independently discovered this and passed `12_000` explicitly at every call site;
  promoted that proven value to the default so callers that don't override it (including
  `Vic20DebuggerSession`'s CLI `type` command) get the same reliability.

### The "LOAD doesn't work" episode (now fixed, kept for the record)

Two compounding bugs, found by watching `$C5`/`$C6` (KERNAL keyboard-scan/buffer zero-page state)
after every single typed character:

1. **Missing boot-wait**: the first test script went straight from `disk ...` to
   `type LOAD"UNEXPANDED",8,1` with zero instructions run first. Booting to a fully live KERNAL
   (VIC configured, keyboard scan active) takes ~130,000-175,000 instructions (established earlier
   this session while investigating Alphoids); the first ~12 of 20 typed characters landed on a
   machine that wasn't listening yet and were silently lost (`?SYNTAX ERROR` on the truncated
   remainder). Fixed by adding `trace 200000` before typing.
2. **Typer margin** (above) - one more character dropped even after the boot-wait fix.

## 7. `via_pb7` test result vs the real-hardware reference

Loaded and ran via the CLI (`disk` + `type` + `key 1 7` for Enter + `trace`), screen dump decoded
screen-code → ASCII and compared row-by-row against the upstream `readme.txt`'s hardware-verified
table (`(gpz) reference data checked on my vic20`):

| ACR (start) | ACR (end) = $00/$40 | ACR (end) = $80/$c0 |
| --- | --- | --- |
| $00, $40 | matches | **mismatch** - our `b`/`a` marker lands one read later than real hardware |
| $80, $c0 | matches | matches |

Precise reading: when the test **starts** with PB7 already enabled (`ACR` start = `$80`/`$c0`),
every result matches real hardware exactly. When it **starts** with PB7 disabled (`$00`/`$40`) and
the test switches ACR to enable PB7 mid-sequence (end = `$80`/`$c0`), our emulated Port B shows the
PB7 bit one read later than real hardware does. Not yet root-caused - open item. Likely candidate:
the `(_acr & 0x80) != 0` pass-through check in `MOS6522.ReadPortB`/`PortBOutput` reacting one step
late relative to the interleaving of the ACR write and the timer-load write in this exact sequence

- needs a `watch`-instrumented step-by-step trace of the test's own four `LDA $9120`-equivalent
reads to pin down which one diverges.

`via_t1irqack` (`BANDITS-VIA1`/`BANDITS-VIA2`, directly relevant to the NMI/IRQ split above) has not
been run yet.

## 8. `via_t1irqack` (`BANDITS-VIA1`) test result - mismatch found

Loaded and ran the same way as `via_pb7` (`disk` + boot-wait `trace 200000` + `type LOAD` + `key 1 7`
for Enter + `type RUN` + `key 1 7`), then watched `$911D` (VIA1's IFR) across the following
500,000 instructions (~75+ frames at this repo's cycle/instruction ratio).

**Screen output** (decoded, row 0 + row 3):

```text
row0: [C0][C0][C0]           (three reverse-video '@' - screen code 0xC0 = 0x80 | 0x40, and 0x40 is the
                               second-copy '@' glyph)
row3: '                  VIA1'
```

Upstream `readme.txt` expects one of two hardware-verified patterns (MOS vs Synertek VIA - both
are "correct", just different real chips):

```text
--@ (first two characters are inverted)
-@@ (first character is inverted)
```

**Neither reference pattern has all three characters reversed.** Our result (`C0 C0 C0`, all three
reversed) matches neither - a real, reproducible discrepancy.

**IFR trace** - expected (`readme.txt`): `C0 C0 00 C0 00 00` (a cycling six-sample sequence, one
per raster-IRQ). Observed via `watch 911D`:

```text
[21081] 911Dh 00h -> C0h
```

**Exactly one transition in the entire 500,000-instruction window, then permanently stuck at
`$C0`** - never clears back to `$00`, never re-fires. CPU is not halted (`status`: `halted=False`,
still executing) - it's alive but the interrupt-flag-clear-on-high-latch-write mechanism this test
specifically targets (see §1/§2 above - the same `MOS6522` code area already touched by this
session's fixes) isn't producing the expected repeating pattern.

**Not yet root-caused.** Plausible connection to the still-open `via_pb7` ACR-transition-timing
mismatch (§7) - both point at subtle ordering/timing issues around `MOS6522`'s ACR-write and
T1-high-latch-write interaction, but that's a hypothesis, not a confirmed shared cause.

`BANDITS-VIA2` (the VIA2 counterpart, ordinary IRQ path - not touched by the NMI/IRQ split) has not
been run yet, and would be a useful control: if VIA2's version matches the reference while VIA1's
doesn't, that narrows the bug specifically to VIA1's interaction with the NMI change; if both
mismatch the same way, the bug is in shared `MOS6522` logic, not the NMI/IRQ routing.

## 9. §7/§8 root-caused: coarse-grained VIA ticking, not a logic bug

Traced `via_t1irqack` (`BANDITS-VIA1`) instruction-by-instruction with `break-instruction-count` +
`watch` (avoiding the `$9114`/T1CL side-effect trap - see Methodology) around the exact IFR
transition (`instructions=3875009`). Disassembled the test's own NMI handler at `$11EE` (installed
via the indirect vector `$0318/$0319 = $11EE`, confirmed via `dump 0300 0320`) directly from a
`dump 11E0 1260`:

```asm
1200: AD 1D 91  LDA $911D      ; read IFR -> A=C0
1203: 8D 00 1E  STA $1E00      ; screen col0 = C0
1206: BD EA 11  LDA $11EA,X    ; table byte
1209: 8D 16 91  STA $9116      ; T1 LATCH LOW (not counter!)
120C: AD 1D 91  LDA $911D      ; read IFR -> still C0
120F: 8D 01 1E  STA $1E01      ; screen col1 = C0
1212: BD EC 11  LDA $11EC,X    ; table byte
1215: 8D 17 91  STA $9117      ; T1 LATCH HIGH (not counter!)
1218: AD 1D 91  LDA $911D      ; read IFR -> still C0
121B: 8D 02 1E  STA $1E02      ; screen col2 = C0
```

All three `LDA $911D` come back `$C0` in this repo - matching the doc's `row0: [C0][C0][C0]`
symptom. Real hardware (per the upstream reference table) returns `$00` for exactly one of these
three reads, depending on ACR start state - a genuine race between the live T1 underflow and the
`STA $9116`/`STA $9117` writes that land *between* two of the `LDA $911D` reads.

**Root cause, confirmed, not a logic bug in `MOS6522`:** `Vic20Machine.StepInstruction`
(`src/PetEmulator.Vic20/Vic20Machine.cs`) runs the CPU's *entire* instruction first
(`_cpu.StepInstruction()`, which internally is cycle-stepped -
`Cpu6502.CycleStepped.Core.cs`'s `while (!_sync)` loop calls each opcode's per-cycle handler and
`_clock.Advance(1)`), and only **after** the whole instruction completes does it tick the VIAs by
the instruction's total cycle count in one lump (`_via1.Tick(cycles); _via2.Tick(cycles);`). A
`STA $9116`/`STA $9117` (4 cycles) lands in `MOS6522.Write` instantly at "cycle 0" of that
instruction from the VIA's own internal-counter point of view, while the *real* T1 decrement that
should be interleaved somewhere within those same 4 cycles only happens afterward, in bulk. This
model can never reproduce a real 6522's write-vs-underflow race, because the write and the
decrements are never actually concurrent in simulated time - only in wall-clock CPU cycles.

`via_pb7`'s §7 mismatch ("PB7 bit lands one read later than real hardware" when ACR transitions
from PB7-disabled to PB7-enabled mid-sequence) is the same root cause: an ACR write and a T1
underflow that need to interleave at the cycle level instead resolve in the fixed "CPU instruction,
then VIA catch-up" order this repo's stepping model imposes.

**This is an architecture limitation, not a quick patch.** Confirmed via
`impact({target: "StepInstruction", direction: "upstream", file_path: "src/PetEmulator.Vic20/Vic20Machine.cs"})`
and `impact({target: "Tick", direction: "upstream", file_path: "lib/PetEmulator.Chips/MOS6522.cs"})`:
both **CRITICAL** risk, 47 and 50 impacted symbols respectively, spanning PET *and* VIC-20 (both
machines drive their VIAs the same coarse-grained way), cassette/Serial/CbmDos timing paths, and
every debugger/CLI/desktop entry point that steps a machine. A real fix needs the CPU to expose a
per-cycle stepping primitive (the internal cycle loop already exists - it's just not surfaced) and
`Vic20Machine`/`PetMachine` to tick VIAs interleaved with each individual memory access instead of
in a post-instruction lump. Not attempted here - flagged for a scoped decision (see chat) instead of
an unreviewed rewrite of both machines' timing cores.

### Follow-up: implemented for VIC-20, tests still don't match reference

Given the go-ahead to pursue the fix: `Cpu6502` (`lib/PetEmulator.Cpu6502/Processor/`) now exposes
`public event Action? CycleElapsed`, firing once per elapsed CPU cycle from inside
`StepInstructionCore`'s cycle loop, right after that cycle's own bus access and `_clock.Advance(1)`,
confirmed to fire exactly once per `CycleCount` delta, including the (pre-existing, zero-cost)
interrupt-injection path, by a dedicated test (`tests/PetEmulator.Cpu6502.Tests/CycleElapsedHookTests.cs`).
`Vic20Machine`'s constructor now subscribes `_via1.Update(); _via2.Update();` to that event instead
of the old `_via1.Tick(cycles); _via2.Tick(cycles);` lump call in `StepInstruction()` - VIA state
now advances in lockstep with the CPU's own cycles, so a register write on cycle N of an
instruction is now correctly preceded by that same instruction's cycles 0..N-1 of VIA-internal
timer decrement, instead of all N-of-N decrements happening only after the whole instruction
(including the write) completes. Zero regressions: full `PetEmulator.Vic20.Tests` (153/153) and
`PetEmulator.Cpu6502.Tests` (337/337, 1 pre-existing unrelated skip) pass; `detect_changes({scope:
"all"})` reports `risk_level: "low"`, `affected_count: 0`.

**Re-ran both VICE regression disks after the fix - byte-identical output to before, on both.**
Traced why: `via_t1irqack`'s IFR-setting T1 underflow happens while the CPU is executing an
unrelated tight polling loop (`$118C`/`$118E`, no VIA access at all that cycle) - not during any of
the instructions that read/write VIA1 registers. Intra-instruction reordering only changes anything
when a VIA-touching instruction's cycles literally straddle the moment T1 crosses zero; here they
never do, so the fix (real and correct in general) has no effect on this specific captured
execution.

## 10. §8 (`via_t1irqack`) actually fixed - real cause found in VICE's own source

The "vendor ambiguity" read of the `readme.txt` two-reference-pattern table (below, in this
section's first draft) was wrong - corrected here. Fetched VICE's actual C source
(`vice/src/core/viacore.c`, `VICE-Team/svn-mirror` on GitHub, `gh api search/code` +
`gh api .../contents/...`) instead of reasoning from the reference table alone, and found the real
answer sitting in a comment on the `VIA_T1LH` (T1 latch-high) store case:

```c
case VIA_T1LH:          /* Write timer A high order latch */
    via_context->via[addr] = byte;
    update_via_t1_latch(via_context, rclk);

    /* CAUTION: according to the synertek notes, writing to T1LH does
       NOT change the interrupt flags. however, not doing so breaks eg
       the VIC20 game "bandits". also in a seperare test program it was
       verified that indeed writing to the high order latch clears the
       interrupt flag, also on synertek VIAs. (see via_t1irqack) */

    /* Clear T1 interrupt */
    via_context->ifr &= ~VIA_IM_T1;
    update_myviairq_rclk(via_context, rclk);
    break;
```

So this isn't vendor ambiguity - it's a **documented, single, real-hardware-verified behavior that
contradicts the official 6522 datasheet**: writing the T1 **latch** high byte ($9117 on VIC-20 VIA1,
`MOS6522`'s `T1LatchHigh`/offset `0x07`) clears the T1 interrupt flag, just like writing the
**counter** high byte does - even though the datasheet says only the counter-high write should. The
VICE devs' own comment names this exact test (`via_t1irqack`) as their proof, and notes real
"Bandits" needs it too - matching this repo's own findings in §7-§9 precisely (the ISR's `STA
$9117` at `$1215`, decoded above, is exactly that "ack via latch-high write" trick).

**Fix**: `MOS6522.Write`'s `T1LatchHigh` case (`lib/PetEmulator.Chips/MOS6522.cs`) now calls
`ClearInterrupt(Timer1Interrupt)` after updating the latch, matching VICE. Confirmed via
`impact({target: "Write", file_path: "lib/PetEmulator.Chips/MOS6522.cs", direction: "upstream"})`
(CRITICAL/110 symbols - expected for a hot shared method; the one-line change itself matches
verified real-hardware behavior, not a guess). Zero regressions: `PetEmulator.Chips.Tests`
(286/286), `PetEmulator.Vic20.Tests` (153/153), `PetEmulator.Pet.Tests` (350/350) all still green.

**Re-ran `via_t1irqack` (`BANDITS-VIA1`) after the fix:**

- IFR (`$911D`) now **cycles** `00→C0→00→C0→...` repeatedly for the rest of the trace window,
  instead of latching at `$C0` forever - matches the shape of the upstream reference
  (`C0 C0 00 C0 00 00`, a repeating pattern), not just one static value.
- Screen row 0 (`$1E00-$1E02`) now reads `C0 C0 00` - a **mix** of reversed/non-reversed cells,
  matching the shape of the real-hardware reference (one of the three reads returns a cleared
  flag), instead of the pre-fix `C0 C0 C0` (all three reversed, matching neither reference pattern).

`via_pb7` (§7) re-ran unchanged (byte-identical dump, same as the §9 follow-up's earlier run) - this
fix is specific to the T1-latch-high IFR-ack path `via_t1irqack` exercises and `via_pb7` doesn't;
§7's "PB7 bit lands one read later" mismatch remains open and needs its own investigation.

## Open items

- Root-cause the `via_pb7` ACR-transition-timing mismatch (§7) - still open; §10's `T1LatchHigh`
  IFR-clear fix didn't touch it (confirmed: byte-identical output before/after).
  `via_t1irqack` (§8) is now fixed - see §10.
- Run `BANDITS-VIA2` as a control to narrow whether `via_pb7`'s remaining mismatch is VIA1-specific
  (NMI-related) or shared `MOS6522` logic.
- `via_wrap`, `vic_9000test`, `joystick` (VICE-testprogs) have no ready `.d64` - would need
  packaging via this repo's own `D64Image.CreateFile` before they can be loaded the same way.
- $9000/$9001 origin registers and interlace mode remain unread by the renderer.
- No printer or RS-232/modem emulation exists for VIC-20 at all.
