# SuperPET Waterloo 6809: consolidated investigation notes

Reference doc pulling together everything learned fighting the SuperPET 6809 Waterloo firmware
into one place, organized by topic rather than chronologically. For the turn-by-turn story of how
each of this was found (including ruled-out hypotheses - FIRQ, NMI, a missing 6502 boot phase -
kept for honesty), see `docs/pet/superpet-6809-boot-hang.md`. For a raw instruction-level listing
of the ROM itself, see `docs/pet/disassembly/waterloo-*.asm` (Capstone-generated, real 6809
disassembler - see that directory's README).

## Status

- **Boot hang: fixed.** The 6809 boots past the point it used to freeze forever, all the way
  through an interactive "Waterloo microSystems...Select:" menu.
- **Keyboard: fixed.** `Cbm8032KeyboardMap` (the class the SuperPET's business keyboard uses) had
  four wrong cells (Enter, Backspace, Space, KeyL) inherited from a plain CBM 8032 table that was
  never actually correct for this board. All fixed and regression-tested against a live ROM.
- **Disk loading: open.** Selecting a language module (`PASCAL`, `BASIC`, ...) and pressing Enter
  shows `Loading 'disk/1.PASCAL'` then `Program not found`, even with the matching `.d64` mounted
  and the file present on it. Confirmed the failure happens *before* any IEEE-488 traffic - see
  "Disk loading (open)" below for the trail so far.

## Hardware/ROM facts (reference)

- Three ROM chips: `waterloo-a000-bfff.970018-12.bin` ($A000-$BFFF), `waterloo-c000-dfff.970019-12.bin`
  ($C000-$DFFF), `waterloo-e000-ffff-970034-12.bin` ($E000-$FFFF) - the "50Hz SuperPET" matched set
  (confirmed via web research earlier in this investigation).
- Real SuperPET hardware is dual-CPU (6502 + 6809), **mutually exclusive** - only one CPU owns the
  bus at a time, confirmed against VICE's own `petcpu.c` (`DMA_ON_RESET`/`DMA_FUNC` macros): when
  the CPU switch is set to 6809, the 6809 takes over immediately at reset via
  `cpu6809_reset()` + `h6809_mainloop()`, no meaningful 6502 boot phase runs first. This
  emulator's model (start `M6809Cpu` directly from its own reset vector when
  `profile.InitialProcessor == Motorola6809`) matches that.
- Interrupts: VICE passes the 6809 the **same shared `CPU_INT_STATUS`** structure as the 6502
  (`h6809_mainloop(CPU_INT_STATUS, ...)`) - i.e. only a combined IRQ line, no FIRQ/NMI wiring at
  the VICE-PET integration level either. This emulator's `PetMachine.StepInstruction()` matches:
  `cpu.SetIRQ(_pia1.IRQ || _pia2.IRQ || _via.IRQ || (_acia?.Irq ?? false))`, no `SetFIRQ`/`RequestNmi`
  calls anywhere for the SuperPET 6809. Confirmed correct, not a gap.
- $E800-$EFFF (PIA1/PIA2/VIA/CRTC, standard PET I/O block) sits *inside* the 6809's $A000-$FFFF ROM
  address range but must take priority over the ROM contents there - VICE's `petmem.c` does this
  explicitly (`for (i = 0xe8; i < 0xf0; i++) { _mem6809_read_tab[i] = _mem_read_tab[i]; ... }`).
- SuperPET-specific I/O: `$EFFC` bank-select (16 banks of 4K expansion RAM at `$9000-9FFF`, **bit 7
  low write-protects the system latch** - this emulator's `SuperPet6809MemoryBus` doesn't model
  that bit, a known small gap, not yet confirmed to matter), MOS 6702 protection dongle at
  `$EFE0-3` (already correctly modeled here - shift-register based, not a static ID byte, see
  `docs/chips/MOS6702.md`), MOS 6551 ACIA at `$EFF0-3`.

## Boot stage map (verified checkpoints)

Built with the permanent `SuperPetBootCheckpoints` tool (`src/PetEmulator.Pet/Diagnostics/`,
CLI: `superpet-boot-checkpoints <maxInstructions> [outputFile]`) - bounded by construction, unlike
`superpet-diagnose`/`trace` which both OOM past ~10-12M instructions (unbounded accumulation).

| Stage | PC | Reached? | Note |
| --- | --- | --- | --- |
| Reset | `$FF80` | yes, instr 0 | |
| ExpansionBankClear | `$BC29` | yes, ×2 | `$EFFC` cleared twice ~19k instructions apart - unexplained re-init, not investigated further |
| KeyboardRingBufferInit | `$DD60` | yes, ~instr 22010 | Arms PIA1 CRB ($3D - CB1 IRQ enable); ring buffer `$012C`/`$012E` set to `$0130` (empty) |
| ChecksCb1FlagAtDE0B | `$DE0B` | yes, periodically | Plain mainline `JSR`, not an interrupt handler (stack depth `S` varies between hits) - checks PIA1 CRB bit7 |
| KeyboardMatrixScanLoop | `$DF01`-`$DF32` | yes, continuously | Real polled matrix scan (`W $E810`=row-select, `R $E812`=column-read), wired through `PetMachine`'s PIA1 callbacks |
| MenuBannerPrint | `$AA66` | yes, ~instr 25438 (post-fix) | Prints "Waterloo microSystems...Select:" + module list |
| MenuChoiceDispatch | `$AA09` | yes, ~instr 25436 (post-fix) | Table-dispatches by pressed letter |
| OldDeadPollLoop | `$D6B4` | **no** (post-fix) | The original hang site - confirmed never reached again after the fix |
| RingBufferEnqueue + 3 call sites | `$DDAD`/`$C6C0`/`$DE3F`/`$DE68` | no (without a real keypress) | Correct: nothing to enqueue if no key is down |
| MenuDispatchTrampoline | `$A9F0` | **no** | `LDX <$2A; JMP ,X` - originally assumed "the sole menu entry point"; wrong assumption, menu is reached some other way |

## Bug 1: the boot-hang fix (I/O hole)

`SuperPet6809MemoryBus` mapped the `waterloo-e000-ffff` ROM over the *entire* `$E000-$FFFF` range
with no hole for the standard PET I/O block, so writes to PIA1/PIA2/VIA/CRTC were silently dropped
as "ROM, read-only". The write that arms PIA1's CB1 interrupt (`$DD60`'s `STB $E813` = `$3D`) was a
no-op, so the interrupt that lets the keyboard-buffer feed run could never fire; the machine spun
forever in a two-instruction loop at `$D6B4`/`$DD82`.

Fixed in `src/PetEmulator.Pet/SuperPet6809MemoryBus.cs`: added `IsStandardPetIoRegister(address)`
and route both `Read`/`Write` through `_petBus` for PIA1/PIA2/VIA/CRTC, checked *before*
`TryFindFirmware` - matches VICE's own approach exactly (see "Hardware/ROM facts" above).

## Bug 2: the real cause the menu never appeared (interrupt frame byte order)

The I/O-hole fix alone made the CPU stop freezing but the menu still never rendered. Root cause,
found by running the exact same ROMs through VICE (a known-working reference emulator) and
bisecting by cycle count where the two diverged: `M6809Cpu.PushFullFrame`/`PushFastFrame`
(`lib/PetEmulator.Cpu6809/M6809Cpu.Instructions.cs`) pushed the 12-/3-byte interrupt frame in the
**exact reverse** of real 6809 hardware order (CC first, PC last, instead of PC first, CC last).
`Rti()`'s pop order (`[CC,A,B,DP,X,Y,U,PC]`, correct and matching real hardware) then read PC from
a stack slot that was never actually written - the CPU resumed at garbage (zeroed RAM) the very
first time IRQ fired, crashing silently before Waterloo's own code ever reached the banner-print
routine.

This predates this whole investigation and would break **any** full-frame 6809 interrupt use
(IRQ/NMI/SWI/SWI2/SWI3/CWAI), not just this ROM - the SuperPET boot hang just happened to be the
first thing in this repo that triggered one and depended on `RTI` actually working. All 91
pre-existing `Cpu6809.Tests` passed before *and* after the fix unchanged - they only ever exercised
push+pop round-trips, which are order-agnostic as long as both sides stay internally consistent,
so the bug was invisible to them by construction.

Regression pin: `InterruptTests.IRQ_PushesTheFullFrameInRealHardwareByteOrder` /
`FIRQ_PushesTheFastFrameInRealHardwareByteOrder` (`tests/PetEmulator.Cpu6809.Tests/M6809/`) - these
read back the *raw pushed bytes* and check them against the documented hardware layout directly,
not just a round-trip.

## Bug 3: `Cbm8032KeyboardMap` had four wrong cells

Found live, once the machine could actually be typed into. All four were "confirmed
behaviorally, not by a text sweep" per that file's own old comment - unlike every letter/digit,
which *was* swept - and turned out wrong for this specific board:

| Key | Old (wrong) | Real | How confirmed |
| --- | --- | --- | --- |
| Space | `(4,7)` | `(0,5)` | `(4,7)` is a dead/unwired cell (no effect); `(0,5)` advances the cursor one column, no glyph |
| Backspace | `(6,5)` | `(7,6)` | `(6,5)` is one of 11 cells that funnel into the ROM's disk-load fallback; `(7,6)` moves the cursor back one column |
| Enter | `(3,6)`, then briefly `(5,4)` | `(3,4)` | `(3,6)` is actually `'@'`. `(5,4)` passed a *weaker* test (cursor moves down a row) but is cursor-down, not Enter - typing `"S"` then pressing it left the cursor one column past the `S`, not reset to column 0. `(3,4)`, tested the same way, replaces the whole menu with the SETUP module's own screen (`BAUD`/`PARITY`/`STOPBITS`/...) - proof it actually submitted the line |
| KeyL | `(4,0)` | `(3,5)` | `(4,0)` is an 8-column tab jump on this board (not `'l'` like every *other* CBM 8032 variant); found live from a user report ("L jumps me right"), confirmed against the same sweep data |

**The methodology that mattered**: a bare keypress with nothing typed first can't distinguish "the
line was really submitted" from "the cursor just moved" - an empty line submitted just redraws the
same menu, which looks identical to a cursor-home/reset action. Only checking for the command's
*actual effect* (a module's screen replacing the menu, a typed character being removed, an 8-column
jump instead of one) tells them apart. This exact pitfall, and the exact same fix, had already
happened once in this repo for the VIC-20 (see `Vic20HostKeyMap.cs`'s own comment on the
`(3,7)` vs `(1,7)` Enter mix-up, and `Vic20MachineTests.Enter_ActuallyExecutesTheTypedLine_NotJustCrsrDown`)

- worth remembering before trusting any "cursor moved" observation as proof of Enter on a
matrix-scanned machine.

Regression tests: `Cbm8032KeyboardMapTests.cs` (pins every entry's claimed row/column) and
`Cbm8032KeyboardMapRomVerificationTests.cs` (the permanent version of the sweep - runs every
letter/digit/Space/Backspace/Enter through a *live, real ROM* and checks the actual resulting
screen effect, not just the map's own claim).

## Disk loading (open)

Selecting a module and pressing Enter shows `Loading 'disk/1.PASCAL'` then `Program not found`,
even with `roms/pet/test-disks/superpet/Waterloo2-Language-No6702-1.d64` mounted on device 8 and
containing a `PASCAL` PRG file (confirmed via direct D64 directory read - the file is really
there, this isn't a missing-file problem).

**Confirmed**: zero IEEE-488 bus traffic occurs at any point during the failed load
(`disk-stall-check` stalls immediately; a full instruction trace shows zero accesses to
PIA2 `$E820-E82F` or VIA `$E840-E84F` anywhere in the load attempt). The failure is decided
*locally*, before any real attempt to talk to a drive.

**Traced so far** (see `docs/pet/disassembly/waterloo-a000-bfff.asm` /
`waterloo-c000-dfff.asm` for the raw listing at each address):

1. `$AC30` (in the a000 ROM): prints a "Loading '...'" message, then `JSR $B0AE` and checks the
   returned `D` via `BEQ` - zero means print `"Program not found"` (string at `$AD1F`), fall
   through otherwise.
2. `$B0AE` is not code - it's a **system-call jump table** (each entry `7E xx xx` = `JMP $xxxx`).
   The call from step 1 lands on entry 0: `JMP $B23B`.
3. `$B23B`: allocates 54 bytes of locals (`LEAS -$36,S`), then `JSR $D412` and checks *its* result
   the same way (`BEQ` on zero).
4. `$D412` (in the c000 ROM): `JSR $B636`.
5. `$B636` turns out to be two tiny helpers, not the real I/O routine: `SetError(2, "eof")` and
   `SetError(3, "I/O time-out")` (both string constants live right after it at `$B666`/`$B66A`) -
   i.e. this just clears/sets an error-status byte at direct-page `$6A` and a flag at `$0300`.
   The actual disk-talk code is further into `$D412` (past the `JSR $B636`, from `$D417` on) -
   **not yet decoded**.

**Live VICE comparison attempted, blocked by environment, not by VICE itself**: tried mounting the
same `.d64` in VICE and driving it through the same menu selection to capture the *real* IEEE-488
sequence for comparison. `keybuf` (VICE's monitor command for stuffing the keyboard buffer) turned
out to only feed the 6502 KERNAL's software buffer, not the physical matrix - no effect on the
6809's own polled scan. Switched to `-remotemonitor` + `xdotool` sending real X11 key events to
VICE's window (the same path a human pressing keys would use) - the remote monitor never responded
to commands sent while the emulation was free-running, in this headless Xvfb-without-a-window-manager
setup. Not a dead end in principle - a real X session (or a working window manager under Xvfb) would
likely let this work - just not achieved in this environment.

**Working hypotheses, unconfirmed**:

- Waterloo may use its own I/O abstraction over IEEE-488 (the `"disk/1.PASCAL"` display format is
  not standard CBM DOS syntax - no real PET KERNAL shows a `LOAD` this way) rather than a plain
  KERNAL-style `LOAD`/`OPEN` sequence `CbmDosEngine` would recognize even if bytes were sent.
- The decision to print "Program not found" happens well before anything resembling a `TALK`/
  `LISTEN` IEEE-488 handshake - whatever check fails, it's checking something *local* first (a
  flag, a table lookup, a "is a drive configured" check) rather than actually asking the drive.

**Next step, concretely**: finish decoding `$D412` from `$D417` onward (raw bytes already pulled,
not yet decoded) - it's the direct continuation after the two `SetError` helpers return, and is
the most likely place the real device-talk (or the local check that skips it) lives.
