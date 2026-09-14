# SuperPET 6809 boot hang - investigation, diagnosis, and fix

**Status: fixed, menu renders.** Two bugs, found in sequence:

1. `SuperPet6809MemoryBus` mapped the `waterloo-e000-ffff` firmware image over the *entire*
   declared `$E000-$FFFF` range with no I/O hole, so writes (and reads) to PIA1/PIA2/VIA/CRTC
   ($E810/$E820/$E840/$E880) never reached the real chips - silently treated as "ROM, read-only."
   This froze the boot forever in a two-instruction loop (`$D6B4`/`$DD82`). Fixed in
   `SuperPet6809MemoryBus.Read`/`Write` by punching a hole for the standard PET I/O block.
2. The real, deeper cause of the menu never appearing: `M6809Cpu.PushFullFrame`/`PushFastFrame`
   pushed the interrupt frame in the exact reverse of real 6809 hardware order, so `RTI` resumed
   execution at garbage instead of the interrupted PC - the CPU crashed into zeroed RAM the first
   time IRQ ever fired, before Waterloo's own code ever reached the banner-print routine. Found by
   running the exact same ROMs through VICE (a known-working reference emulator) side-by-side and
   bisecting the divergence by cycle count. Fixed in `M6809Cpu.Instructions.cs`.

A third, unrelated bug (`Cbm8032KeyboardMap`'s Enter/Backspace/Space matrix cells were wrong -
inherited from a plain CBM 8032, not this SuperPET board) was found immediately after, once the
machine could actually be typed into, and fixed via a live full-matrix sweep.

See "Root cause, precisely" / "The fix" (bug 1), "Real root cause, found via VICE side-by-side
comparison" (bug 2), and "Keyboard mapping" (bug 3) sections below for the full trail; the
sections between them were written *during* the investigation and are kept as the record of how
it was found - including hypotheses that turned out wrong (FIRQ, NMI, a missing 6502 boot phase),
left in place rather than deleted so the reasoning trail stays honest.

## Root cause, precisely

`SuperPet6809MemoryBus.Write`/`Read` (before the fix) checked, in order: bank-select register,
protection dongle, ACIA, expansion-RAM window, then **firmware ROM** (`TryFindFirmware`), only
falling through to the shared `_petBus` (which correctly routes PIA1/PIA2/VIA/CRTC) if none of
those matched. The `waterloo-e000-ffff-970034-12.bin` ROM's declared requirement is
`(0xE000, 0x2000)` - the full $E000-$FFFF range, byte for byte, with no gap carved out for the
standard PET I/O chips that live inside that range on real hardware ($E810 PIA1, $E820 PIA2,
$E840 VIA, $E880 CRTC - all inside $E800-E8FF). `TryFindFirmware` therefore claimed those
addresses first: reads returned stale ROM bytes, writes were silently dropped as "ROM is
read-only," and the write that matters most - `STB $E813` (PIA1 CRB) writing `$3D`, which sets
CRB bit0 (CB1 IRQ enable) - never reached `MT6520`. Confirmed live before the fix: reading back
`$E813` after that STB returned the ROM's raw byte, not `$3D`; `via-irq-check 60000` reported
`PIA1.IRQ true on 0/60000` for the entire run, matching (and explaining) the `$012C`/`$012E`
finding below - the flag genuinely never had a chance to latch, chip logic was never at fault.

Two earlier hypotheses tested and ruled out along the way, kept for the record:

- **FIRQDISABLE via `$EFFC` bit 6** - the source describing that bit is explicit that it's a
  Super-OS/9 aftermarket-MMU-board feature; "/FIRQ pin is unused in the stock SuperPET." Not the
  cause here (this emulator models stock hardware).
- **NMI** - the 6809 soft-vector table (`$0100`+, indexed off the ROM's `$FFB0-FFBF` BSR-dispatch
  stubs) showed NMI's slot (`$010C`) literally `$0000` (unset) at the point of the hang; NMI is not
  what Waterloo uses here. **FIRQ's slot (`$0106`) was set to a real handler at `$DE0B`** -
  disassembling it confirmed it checks PIA1 CRB bit 7 (the CB1 flag) and, when set, chains through
  `$DE1A -> $DE3F -> JSR $DDAD` (the ring-buffer enqueue routine) - i.e. **FIRQ *is* the intended
  mechanism**, just never reachable because the CRB write that arms it never landed on the real chip.

## The fix

`src/PetEmulator.Pet/SuperPet6809MemoryBus.cs`: added `IsStandardPetIoRegister(address)` (checks
`PetMemoryBus.Pia1Base`/`Pia2Base`/`ViaBase`/`CrtcBase` with their real chip lengths) and route
both `Read` and `Write` through `_petBus` for those addresses, checked before `TryFindFirmware` -
mirrors the ACIA/dongle/bank-select pattern already in the same methods.

Verified after the fix:

- `$E813` readback after the CRB-arming `STB` now returns `$BD` (`$80` flag bit | `$3D` control
  bits) - the CB1 flag genuinely latches.
- `via-irq-check 60000` now reports `PIA1.IRQ true on 33288/60000` (was `0/60000`) - regression-pinned
  in `PetDebuggerSessionTests.ViaIrqCheck_ReportsPia1AssertingPeriodically_NowThatTheSuperPet6809BootHangIsFixed`.
- The old infinite `$D6B4`/`$DD82` two-instruction loop is gone. `superpet-diagnose` at increasing
  instruction budgets now shows genuine forward progress instead of an identical frozen state -
  `pc-final=$B7BC` at 3,000,000 instructions vs `pc-final=$B82F` at 8,000,000 (the same A/B check
  that originally proved the hang was frozen, now proving it isn't).
- Full "renders the Waterloo `Select:` menu and accepts a keypress" was **not** confirmed - by
  8,000,000 instructions (the largest budget `superpet-diagnose`/`trace` could run without OOM,
  see Tooling gap below) the screen still showed only a blinking cursor, and one attempt to type a
  digit via `TextTyper`/`Cbm8032KeyboardMap` mid-run produced no visible echo. The machine now
  reaches and repeatedly runs a real keyboard-matrix scan loop at `$DF2F` (`W $E810=row-select`,
  `R $E812=column-read`, wired through `PetMachine`'s `_pia1.PortAWritten`/`PortBInput` exactly as
  expected) - so the boot hang itself is fixed, but whether the SuperPET's keyboard-matrix row/col
  mapping matches what this scan loop expects is unverified and is the natural next question if
  the menu still doesn't respond to input under manual testing.

## Boot-stage checkpoint map (added to answer "does the menu ever show up")

Per-instruction manual disassembly doesn't scale past a few thousand steps, and the existing
reporting tools OOM past ~10-12M instructions (see "Tooling gap" below) - so a permanent,
bounded-by-construction tool was added instead of guessing further:
`src/PetEmulator.Pet/Diagnostics/SuperPetBootCheckpoints.cs` /
`SuperPetBootCheckpoints.WellKnown` names every ROM address this investigation has pinned down by
hand, in expected boot order; `SuperPetBootCheckpoints.Run(machine, maxInstructions)` steps the
machine, and every time PC lands on a known checkpoint records a full register + watched-memory
snapshot (capped per checkpoint, so a checkpoint inside a tight loop can't blow memory). CLI:
`superpet-boot-checkpoints <maxInstructions> [outputFile]`.

A 20,000,000-instruction run (`docs` note: reran twice while refining checkpoint descriptions;
figures below are from the final run) shows:

| Stage | PC | Reached? | First hit (instr / cycles) | Note |
| --- | --- | --- | --- | --- |
| Reset | `$FF80` | yes | 0 / 0 | |
| ExpansionBankClear | `$BC29` | yes, twice | 251, 41336 | `$EFFC` cleared a second time ~19k instructions after the first - boot re-runs some init, not yet explained |
| KeyboardRingBufferInit | `$DD60` | yes | 22010 / 103131 | `$E813` (CRB) reads `$80` right after - flag already latched, confirming the I/O-hole fix works live |
| ChecksCb1FlagAtDE0B | `$DE0B` | yes, twice | 22029, 41489 | `S` differs by ~0x700 between the two hits - not a fixed-size interrupt frame, so this is a plain `JSR` from mainline code (probably a periodic "any key ready" poll), not an interrupt handler - corrects this investigation's earlier assumption that it was the FIRQ target |
| KeyboardMatrixScanLoop | `$DF2F` | yes, repeatedly | 22064 onward | The real, polled (not IRQ-driven) keyboard matrix scan; wired correctly through `PetMachine`'s PIA1 callbacks |
| Address09FD | `$09FD` | yes | 23240 / 110116 | Later becomes the *installed* FIRQ soft-vector target (`$0106`), but at this first hit `$0106` still reads the default `$FFB1` stub - so this occurrence is ordinary call flow, not a fired FIRQ; whether it's ever reached as the live FIRQ target is still open (FIRQ is never wired by `PetMachine` regardless) |
| OldDeadPollLoop | `$D6B4` | **no** | - | Confirms the original hang site is never even reached anymore post-fix |
| RingBufferEnqueueCallA/B/C | `$C6C0`/`$DE3F`/`$DE68` | **no** | - | |
| RingBufferEnqueue | `$DDAD` | **no** | - | No byte is ever actually enqueued - consistent with no real keypress happening |
| SwiSoftVectorTarget | `$BC73` | **no** | - | |
| IrqDefaultStub | `$FFB1` | **no** | - | |
| MenuDispatchTrampoline | `$A9F0` | **no** | - | The sole entry point into the whole menu subsystem - never reached in 20,000,000 instructions |
| MenuBannerPrint | `$AA66` | **no** | - | "Waterloo microSystems...Select:" is never printed |
| MenuChoiceDispatch | `$AA09` | **no** | - | |

Reading this: the boot hang fix is solid (keyboard ring-buffer init, PIA1 arming, and the polled
matrix scan all demonstrably run correctly, repeatedly, exactly as designed). What's still
unexplained is *upstream* of all of this - nothing in the ~20,000,000-instruction window ever
enters the menu subsystem at all, with or without a keypress, which real hardware would do
unconditionally near the start of boot. Since nothing in the ROM statically references
`$A9F0`/`$AA66`/`$AA09` except the table they're stored in (checked via `grep`/`xxd` across all
three ROM images), entry must be through direct-page `$2A` (`LDX <$2A; JMP ,X`) - and that DP cell
was never observed being written or read in any traced window either. **Next step, precisely**:
add checkpoints/watches around instr 251-22010 (the largest still-unmapped stretch of this boot -
everything between the first `$EFFC` clear and the keyboard ring-buffer init) to find what, if
anything, is supposed to seed direct-page `$2A` and why it doesn't.

## Real hardware reference: $E800-EFFF I/O block and the two SuperPET-specific devices

Checked against outside sources while chasing the menu-never-renders gap (not this emulator's own
docs/code):

- **`$EFFC` bank-select register**: bits 0-3 select 1 of 16 4Ki banks of the 64K expansion RAM,
  mapped into `$9000-9FFF`; **bit 7 low write-protects the system latch**. This emulator's
  `SuperPet6809MemoryBus.Write` masks the written value to 4 bits (`value & (BankCount - 1)`) and
  ignores bit 7 entirely - the write-protect behavior isn't modeled. Not yet confirmed to matter for
  the menu-never-renders gap, but a real, verified discrepancy from documented hardware.
  ([Mike Naberezny - Super-OS/9](https://mikenaberezny.com/hardware/superpet/super-os9-mmu/),
  [6502.org PET index - SuperPET](https://6502.org/users/andre/petindex/superpet.html))
- **MOS 6702 protection dongle** (`$EFE0-$EFE3`): not a static ID byte - internally eight
  independent shift registers (one per output bit, each a different length/polarity), and "reading
  the byte means reading the outputs of the 8 shift registers." Waterloo's language modules probe
  it and are documented to crash if it's not detected as present. This emulator's
  `lib/PetEmulator.Chips/MOS6702.cs` already models exactly this (per-bit shift registers, XOR
  feedback on accepted odd/even write pairs) rather than a stub - built from the same
  reverse-engineered VICE model these sources describe, not a static-response shortcut. Confirmed
  live (see below) that the dongle is never even touched before the menu-dispatch gap, so it isn't
  currently in the causal chain either way.
  ([Forum64 - MOS6702 dissected](https://forum64.de/index.php?thread%2F116391-mos6702-superpet-dongle-dissected=),
  [visual6502.org blog](http://blog.visual6502.org/2013/02/mos-6702-superpet-security-dongle.html),
  [6502.org PET index - SuperPET](https://6502.org/users/andre/petindex/superpet.html))

## Tooling bug found while re-checking the above: `BusObserver` watched the wrong bus in 6809 mode

While re-verifying "is the dongle/bank-select register ever touched" against `trace-log`, the
$EFFC write this doc's own "Root cause, precisely" section describes (confirmed real via the
boot-checkpoint tool's direct memory reads) turned out **invisible** in a fresh `trace-log` run -
the instruction at $BC29 (`STB $EFFC`) advanced PC correctly but logged no bus access at all.

Cause: `PetMachine.BusObserver` always read/wrote `_memoryBus.Observer` (the shared PET bus),
regardless of `_activeMemory`. While the SuperPET 6809 is selected, `_activeMemory` is
`_superPet6809Memory` (`SuperPet6809MemoryBus`) - a *separate* object with its own `Observer`
property - and anything that class handles itself before falling through to `_memoryBus` (the
bank-select register, the protection dongle, ACIA-via-6809, ROM-claimed writes) never touched
`_memoryBus.Observer` at all. `DiagnoseSuperPet6809Startup`'s device-access log was unaffected (it
sets `_superPet6809Memory.Observer` directly, bypassing the public property), which is why the
`$BC29 W $EFFC=$00` finding earlier in this doc was always correct - but `trace-log`/
`InstructionTracer` and any future Observer-based tool were silently missing this traffic.

Fixed in `PetMachine.cs`: `BusObserver`'s setter now routes to whichever of `_memoryBus`/
`_superPet6809Memory` is currently active (`ApplyBusObserver`, also called from
`SelectProcessor` so a live processor switch keeps the subscription instead of losing it) - never
both at once, so there's no double-fire through `SuperPet6809MemoryBus`'s own fallthrough calls
into `_petBus`. Verified: `trace-log` now shows `W $EFFC=$00` correctly. Re-ran the
dongle/menu-table/`$2A` "never touched" checks with the fix in place - all three came back
unchanged (still never touched), so this was a real observability bug but not the cause of the
menu-never-renders gap; it does mean every `trace-log`-based finding in this doc from before the
fix should be treated as *possibly under-reporting* 6809-specific bus traffic, not wrong outright
(RAM/standard-I/O-hole addresses always fell through to `_petBus` regardless, so those findings -
including the boot-checkpoint table below - were unaffected). Regression-pinned in
`PetMachineTests.BusObserver_SeesSuperPet6809SpecificBusHandling_NotJustTheSharedPetBus` and
`BusObserver_MovesToWhicheverBusIsActive_AcrossALiveProcessorSwitch`.

## Real root cause, found via VICE side-by-side comparison

Everything above (I/O hole, checkpoint tool, boot progressing further) was real progress, but the
menu never actually rendered until this was found. Downloaded and analyzed VICE's real
`vice/src/pet/` source (a known-working SuperPET+6809 emulator) and, critically, **ran the exact
same three Waterloo ROM files through VICE itself** (`xpet -model SuperPET -superpet -cpu6809`) -
it renders the full "Waterloo microSystems...Select:" menu, proving the ROMs are correct and the
gap was entirely in this emulator.

Bisected VICE's monitor (`step`/`screen` commands) by cycle count to find exactly when the menu
banner appears (between cycle 57622 and 68359), then compared this emulator's own trace at the
same absolute cycle count - it was still mid-way through the screen-clear loop, running measurably
behind. Tracing back from there found the CPU landing at `$01C0` - genuinely zeroed, never-written
RAM - immediately after an `RTI`. Dumping the pushed interrupt frame's raw bytes showed the
push order was completely scrambled: PC's own bytes sat where CC/A belonged, X and Y were swapped.

**Root cause**: `M6809Cpu.PushFullFrame()`/`PushFastFrame()` pushed the interrupt frame in the
exact reverse of real 6809 hardware order (CC first, PC last, instead of PC first, CC last).
`Rti()` pops in the correct, standard `[CC,A,B,DP,X,Y,U,PC]` order - but since the frame had been
pushed backwards, RTI restored PC from a stack slot that was never actually written, sending
execution into zeroed RAM the very first time IRQ ever fired (during the keyboard ring-buffer
init at `$DD60`) - crashing silently before the ROM ever reached the banner-print code. This bug
predates this whole investigation and would have broken **any** 6809 IRQ/NMI/SWI/CWAI use, not
just this ROM - it just happened that the SuperPET boot hang was the first time anything in this
repo triggered a full-frame interrupt and then relied on RTI actually working.

Fixed in `lib/PetEmulator.Cpu6809/M6809Cpu.Instructions.cs`: both push methods now push in the
correct order. All 91 pre-existing `Cpu6809.Tests` still passed after the fix unchanged (they only
ever exercised push+pop round-trips, which are order-agnostic as long as both sides stay
consistent - this bug was invisible to them by construction). Added
`InterruptTests.IRQ_PushesTheFullFrameInRealHardwareByteOrder` and
`FIRQ_PushesTheFastFrameInRealHardwareByteOrder`, which read back the raw pushed bytes and check
them against the documented hardware layout directly - the actual regression pin.

**Verified**: `superpet-boot-checkpoints` now hits `MenuBannerPrint` ($AA66) and
`MenuChoiceDispatch` ($AA09) within ~25,500 instructions, unconditionally (no keypress needed).
Screen dump shows the full menu:

```text
Waterloo microSystems
Select :
  setup   monitor   apl   basic   edit   fortran   pascal   development
```

One earlier assumption in this doc turned out incomplete: `MenuDispatchTrampoline` ($A9F0, the
`LDX <$2A; JMP ,X` this doc originally called "the sole entry point into the whole menu subsystem")
is still never hit - the real boot reaches the banner via a different path. That specific claim
was wrong, not the fix.

## Keyboard mapping: Enter/Backspace/Space were also wrong (found immediately after, live)

With the machine finally reaching an interactive prompt, typing surfaced a second, unrelated bug:
pressing Enter echoed `@` instead of moving to a new line. `Cbm8032KeyboardMap`'s `Enter`/
`Backspace`/`Space` positions were inherited from personal-001 (a plain CBM 8032) and marked in
that file's own comment as "confirmed behaviorally, not by a text sweep" - unlike every letter/
digit, which *was* swept and verified. They were wrong for this SuperPET board's real matrix.

Found the correct cells with a full 80-cell sweep against a live, freshly-booted machine (press
each of the 10x8 matrix positions in turn with nothing typed, diff the resulting screen against an
untouched boot):

- **Space**: `(4,7)` (old) is a dead/unwired cell (no effect at all) - correct is `(0,5)`.
- **Backspace**: `(6,5)` (old) is one of eleven cells that all funnel into the same disk-load
  fallback the ROM uses for a scan result it doesn't recognize (i.e. also dead) - correct is
  `(7,6)` (verified: moves the cursor back one column).

Enter needed a **second** round, and the first answer from the same bare sweep was wrong - the
exact same pitfall this repo already hit once for the VIC-20 (see `Vic20HostKeyMap.cs`'s own
comment on the same mistake): with nothing typed, the cursor is already at column 0, so "moves
down one row" can't distinguish a real newline from plain cursor-down - both leave the cursor at
column 0 by coincidence. `(5,4)` passed that weaker test but turned out to be cursor-down: typing
`"S"` first, then pressing it, left the cursor one column past the `S` on the next row instead of
resetting to column 0. Re-tested by typing a letter *first* this time and checking whether the
menu actually reacts to it: `(3,4)` replaces the whole menu with the "setup" module's own screen
(BAUD/PARITY/STOPBITS/PROMPT/LINEEND/RESPONSE) after typing `"S"` - proof it actually submitted
the line, not just moved the cursor. That's the real Enter.

Fixed in `Cbm8032KeyboardMap.cs` (`Enter` = `(3,4)`, `Backspace` = `(7,6)`, `Space` = `(0,5)`);
regression-pinned in `Cbm8032KeyboardMapTests.cs` (full `TestCaseSource`-driven coverage of every
entry in the map's table, not just these three). Live-verified: typing `"SETUP"` then Enter
renders the full setup-module screen, not `'@'` and not a silent cursor move.

A fourth cell was also wrong, reported live once typing worked: `KeyL` at `(4,0)` - its position on
every *other* CBM 8032 variant - is actually an 8-column tab jump on this specific board, not
`'l'`; real `'l'` is `(3,5)`. Cross-checked the rest of the table's 43 plain-cell entries against
the same sweep data at the same time - every other letter/digit/Minus/ShiftLeft position already
matched, so `KeyL` was the only other error inherited from the plain-CBM-8032 source table.

## Tooling gap found while verifying

`SuperPetStartupDiagnostics`/`DiagnoseSuperPet6809Startup` and `MachineDebugger.Trace` both
accumulate the *entire* requested run in memory (a `List<TracedInstruction>` / one giant
`StringBuilder`) rather than ring-buffering like `InstructionTracer` does (`InstructionTracer`
already caps at 4,000). Both threw `OutOfMemoryException` past roughly 10-12 million instructions,
which is why the fuller "does it reach the interactive menu" check above is incomplete. Worth
capping both the same way `InstructionTracer` already is, if this investigation continues.

Asked directly: "zweryfikuj botowanie super pet dla 6809 - nie działa" (verify SuperPET 6809 boot -
it doesn't work). Confirmed: it doesn't reach an interactive state. This documents what was
verified, what was ruled out, and the one concrete lead left unexplored.

## Reproduction

```text
profile superpet
roms roms/pet
superpet-diagnose 3000000
```

(`dotnet run --project src/PetEmulator.Cli -- debug <script>`)

Result: `pc-final=$DDA8`, `instructions=3000000`, `halted=False` - the machine never leaves a small
polling loop first reached around instruction 59,990. Cheaper repro: `superpet-diagnose 60000`
already shows it (compare `report.Instructions[59999]` across two runs with different instruction
counts - identical PC/A/B/X/Y/U/S/DP/P at any instruction count ≥ ~60,000 proves it's frozen, not
just slow).

## The loop

Decoded directly from `roms/pet/cbm-8032/waterloo-c000-dfff.970019-12.bin` (the ROM byte-for-byte,
not a guess):

```text
$D6B4: JSR $DD82        ; -> reads a byte into B (poll a status/buffer, no bus access observed)
$D6B7: STB ,X           ; X is pinned at $0004 the whole time
$D6B9: CMPB #$0D \ BEQ  ; is it CR?
$D6BD: CMPB #$03 \ BEQ  ; is it Ctrl-C?
$D6C1: CMPB #$80 \ BCC  ; is it >= $80?
$D6C5: CLRA
$D6C6: JSR $D81B        ; second poll, result ignored
$D6C9: BRA $D6B4        ; unconditional - loops forever until B matches one of the above
```

This is a "wait for CR / Ctrl-C / a high-bit key" loop - textbook menu/prompt input wait. B stays
`$00` forever, so it never matches and the branch out of the loop is never taken.

## Ruled out

- **Not the 6800/6809 core migration** (`docs/plans/2026-09-12-6800-6809-migration-checklist.md`).
  Git-worktree A/B test: checked out `054a0428` (parent of `83ff9f2 merge: integrate 6800-6809 core
  migration`, i.e. the last commit before that migration), rebuilt, ran the identical
  3,000,000-instruction script. Byte-for-byte identical final state (`pc-final=$DDA8`, same
  registers). The hang predates the migration; it is not a regression from it, and Cpu6809.Tests
  (91/91) / the PetMachine SuperPET integration tests (15/15) all still pass because none of them
  run anywhere near 60,000 instructions.
- **Not disk-related.** Mounted `roms/pet/test-disks/superpet/Waterloo2-Language-No6702-1.d64`
  before running the same diagnose - identical hang, identical final state. The loop is reached
  before any disk/IEEE-488 activity would occur.
- **Not fixed by a real keypress.** Ran to instruction 59,990, injected `key 3 6 down` (RETURN in
  `Cbm8032KeyboardMap`) for 100 instructions, released, ran 5,000 more - still stuck in the same
  loop. Pressing the emulated PET keyboard does not reach whatever B is supposed to reflect.

## Disassembly: the poll routine, decoded precisely

`$DD82`'s bytes (`waterloo-c000-dfff.970019-12.bin`, offset `$DD82-$C000`):

```text
$DD82: 32 7F        LEAS ,S        (frame teardown from the caller's PSHS, harmless)
$DD84: 6F E4        CLR  ,X        (X = $0004 throughout - clears that one byte)
$DD86: FC 01 2C     LDD  $012C     (loads a 16-bit word from $012C/$012D - a "head" pointer)
$DD89: B3 01 2E     SUBD $012E     (subtracts the word at $012E/$012F - a "tail" pointer)
$DD8C: 27 19        BEQ  $DDA7     (branch if equal - i.e. buffer empty)
$DDA7: 4F           CLRA
$DDA8: E6 E4        LDB  ,X        (reads back the byte CLR just zeroed - always 0)
$DDAA: 32 ...       LEAS ...
$DDAC: 39           RTS
```

This is a classic ring-buffer empty-check: head (`$012C`) and tail (`$012E`) pointers, "equal means
empty", falling through to "return 0" when empty. `watch-range 12c 130` across the full
65,000-instruction boot (`trace 65000` in the same session) confirms **neither word is ever written,
not once, from reset onward** - the buffer starts empty and nothing ever enqueues a character into
it, consistent with B staying `$00` forever and the outer loop at `$D6B4` never seeing CR/Ctrl-C/a
high-bit key.

## Every interrupt source is explicitly disabled by the ROM itself

Extended the check (`via-irq-check`, see below) to sample **all three** IRQ-capable chips
(`PetMachine.Via`, the newly-exposed `PetMachine.Pia1`, `PetMachine.Acia`) after every step for
300,000 steps:

```text
VIA.IRQ true on 0/300000 (last -1); PIA1.IRQ true on 0/300000 (last -1); ACIA.Irq true on 0/300000 (last -1)
```

**None of the three ever interrupts.** This isn't a hidden bug in any one chip - the boot trace's own
device-access log shows the ROM turning them off on purpose, early:

- ACIA (`$EFF2` Command register) is written `$6B` - bit 1 set = **receiver IRQ explicitly disabled**.
- PIA1's control registers (`$E811` CRA, `$E813` CRB) are each written `$00` - clearing the IRQ-enable
  bit in both, **disabling PIA1's IRQ entirely** (this is the chip whose CB1 line normally carries
  the ~60Hz jiffy-clock/keyboard-scan pulse - see `PetMachine.ClockPia1Cb1`'s doc comment - so this
  is the one most likely to matter for a keyboard buffer).
- VIA (`$E840` ORB, `$E842` DDRB, `$E84C` PCR) is only used for plain I/O pin setup - its
  timer/interrupt registers (ACR `$E84B`, IER `$E84E`, T1 `$E844`/`$E845`) are never touched at all.

Also ruled out: real SuperPET hardware does **not** run both CPUs concurrently - the 6502/6809
switch is a hard bus-arbitration selector, one CPU fully halted while the other owns the bus
(confirmed via the 6502.org SuperPET hardware writeups). So "the missing update comes from the other,
never-ticked CPU" is not the explanation either - both CPUs being mutually exclusive is itself
correct, matching this emulator's `SelectProcessor` model.

## Where this leaves it

Every hardware interrupt path an update to `$012C` could plausibly ride in on is confirmed either
absent (VIA timer) or deliberately turned off by the ROM (PIA1, ACIA) within the first ~200
instructions of boot, and stays off for the entire run. The buffer is empty by construction and
nothing in this emulator's current chip set ever fills it. Two explanations remain, and telling them
apart needs something this investigation doesn't have access to:

1. Real hardware also relies on none of these, and Waterloo fills `$012C` some other way entirely
   (a mechanism/chip this emulation doesn't model yet) - a genuine missing-feature gap, not a logic
   bug in existing code.
2. Real hardware re-enables one of these three at some point this investigation didn't reach (the
   3,000,000-instruction ceiling tested here might simply not be enough, or a different profile /
   ROM revision behaves differently).

Resolving either needs the Waterloo 6809 ROM's own symbol table/source (not available here) or a
side-by-side trace against a known-working reference emulator (e.g. VICE) of the exact same boot,
comparing interrupt-enable writes and `$012C` traffic instruction-for-instruction. Recommended next
step if this continues, in that order.

## Tooling used / added

Existing arsenal (`docs/pet/debug-tools.md`) covered everything except one gap:
`PetDebuggerSession`/`MachineDebugger` had no way to inspect a chip's `IRQ` line directly. Added
`via-irq-check <steps>` (mirrors `disk-stall-check`'s shape: run N steps, report how many had each
chip's IRQ line true and the step of the last one) - samples `PetMachine.Via`/`Pia1`/`Acia` every
step. `PetMachine.Pia1` is a new public accessor (mirrors the existing `Via`/`Acia` ones) added
solely so this tool could reach it. Both are permanent, reusable for any future PET interrupt-timing
question, not just this one - see `PetDebuggerSessionTests.ViaIrqCheck_ReportsZeroAssertions_DuringTheSuperPet6809BootHang`
for the regression pin.
