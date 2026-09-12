# VIC-20 rendering bug fixes

Three bugs reported after actually running the GUI (colors wrong, aspect ratio wrong, no
blinking cursor) - all root-caused using the real KERNAL disassembly
(`docs/vic20/disassembly/kernal.asm`, `docs/vic20/disassembly/basic.asm` - `da65`, kept on disk
per the workflow this doc records for next time) plus `PetMachine.BusObserver`-style register/
memory tracing (mirrors `docs/pet/debug-tools.md`'s methodology).

## Bug 1: colors wrong (root cause: color-RAM address offset)

`Vic20RasterDisplay` read color RAM at `ColorRamStart + col + row*cols` - offset 0 for cell
(0,0). Real VIC-I hardware doesn't fetch color RAM from a fixed offset: it fetches using the same
low bits of the video matrix address as the screen fetch, so a screen relocated away from offset
0 (which the real KERNAL does - see Bug 3 below) needs a matching color-RAM offset,
`ScreenMatrixBase & 0x3FF` (color RAM only has 10 useful address lines - the matrix's high bits
select a 512-byte page, but wherever within that page it starts also shifts where color RAM
starts). Confirmed empirically: with the real boot-time `ScreenMatrixBase` = $3E00, tracing real
writes showed the KERNAL painting color RAM at $9600-$97F9 (offset $200), exactly
`$3E00 & 0x3FF`. Fixed: `MOS6560.ColorMatrixOffset` (new property) added into the color-RAM
address in `Vic20RasterDisplay.Render`.

## Bug 2: aspect ratio wrong (root cause: hardcoded 1:1, no config contract)

`Vic20MachineViewModel.PixelAspect` was hardcoded to `(1, 1)`, explicitly flagged in its own doc
comment as an unverified simplification. Fixed two things at once:

- **Contract**: new `Vic20DisplayConfig` record, passed into `Vic20Machine`'s constructor (not
  hardcoded downstream in a GUI ViewModel) - mirrors `PetProfile.PixelAspect`'s role for PET.
  `Vic20Machine.DisplayConfig` exposes it; `Vic20MachineViewModel.PixelAspect` now reads through
  it instead of a literal.
- **Value**: derived the same way `PetProfile.PixelAspect`'s own doc comment documents its
  values - real hardware paints a physical 4:3 CRT regardless of native pixel count, so
  `PixelAspect = (4:3 target) / (native resolution)`. With `Vic20RasterDisplay`'s native 176x184
  canvas: `(4*184):(3*176) = 736:528`, reduced by their gcd (16) to **46:33**.

## Bug 3: no blinking cursor (root cause: test false positive masked a real, working blink)

This one took the most digging, and the actual renderer/machine code turned out to have **no
bug** - the blink genuinely works. What was wrong: the boot-detection heuristic in
`Vic20BootTests`/`Vic20KeyboardBootTests` (`HasScreenText`, `!= space` counting) is a false
positive on zeroed RAM (`0x00 != 0x20` too), which a machine briefly shows at a *provisional*
screen address before the KERNAL performs its final relocation. Tracing every `$9000`-`$900F`
write across the whole boot (not stopping at the first "looks booted" check) found the KERNAL
writes $9002/$9005 **twice**: once early ($9002=$16, $9005=$C0 → CPU address $1000, a transient
value), then ~35 instructions later with the real final configuration ($9002=$96, $9005=$F0 →
$1E00, matching the KERNAL's own `$0288` HIBASE variable). The early, provisional $1000 region
happened to contain zeroed RAM, which the flawed heuristic misread as "already booted" -
`Vic.ScreenAddr` at that exact moment was real but not yet final.

Confirmed the actual blink mechanism is correct three ways:
1. Disassembly: `kernal.asm` line ~1650 (`LEAEA: eor #$80` inside the periodic jiffy-IRQ handler)
   is a real cursor-blink routine, gated by a `$CD` countdown (reloaded to `$14`=20, i.e. blinks
   every 20 IRQs) and a `$CC` suppress flag.
2. IRQ trace: 410 IRQ-vector fetches ($FFFE/$FFFF reads) over 1.2M instructions post-boot -
   VIA2's Timer1 (armed free-run, `$912B`=$40 ACR, `$912E`=$C0 IER) genuinely fires periodically;
   sparse polling of `VIA2.IRQ`/`IFR` between fetches missed the transient true state (the KERNAL
   itself acknowledges/clears it every IRQ) - another point sampling artifact, not a bug.
3. Full-screen diff at the *correct* ($1E00) address: exactly 1 byte toggles roughly every 6
   render ticks - a clean, periodic blink. Confirmed visually too: two `PetEmulator.Screenshot`
   captures 6 ticks apart show the cursor block present in one, absent in the other.

Fixed the test-quality issue anyway (`HasScreenText` now requires a real letter, screen-code
1-26, not just "non-space") so this false positive can't recur, matching the honest state of what
the check was ever supposed to prove.

## Bug 4: keyboard didn't work at all (root cause: wrong VIA, wrong ports, single-row model)

Reported separately, after the first three fixes shipped: "nie mogę wpisać nic, klawiatura nie
działa". By far the deepest issue - three compounding bugs, only findable by tracing a real
keypress all the way from a routed GUI key event down to what the KERNAL's own scan code actually
reads.

1. **Wrong VIA, wrong ports.** `Vic20Machine` wired the keyboard to VIA1 port A (row-select
   output) / port B (column input) - ported from a reference project's convention without
   verifying it against this real KERNAL. The disassembly (`kernal.asm` ~line 1685: `sta $9120`
   writes row-select, `lda $9121`/`lda $9121` debounce-reads columns) and the real boot-time DDR
   writes ($9122=DDRB=$FF all-output, $9123=DDRA=$00 all-input) both confirm the real wiring is
   **VIA2** port B out / port A in - the exact opposite chip and the opposite two ports. Wrong in
   this specific way doesn't crash or corrupt anything visible: it just means every real keypress
   is written to a VIA nobody's KERNAL scan routine ever reads, so it silently vanishes. Fixed in
   `Vic20Machine`'s constructor (`_via2.PortBWritten`/`_via2.PortAInput` instead of `_via1`'s).

2. **Single-row keyboard matrix model.** Even after wiring the right VIA/ports, keypresses still
   didn't register. Tracing every `$9120`/`$9121` access with a key held the whole time
   (`PetMachine.BusObserver`-style) revealed the KERNAL runs a cheap "is anything pressed at all"
   check on every jiffy IRQ *before* ever running its real, expensive per-row scan: it asserts
   **all eight rows simultaneously** (`$9120=$00`) and reads `$9121` once - real open-collector
   hardware wire-ORs every asserted row's column state together, so a pressed key in *any*
   currently-asserted row pulls its column low. `Vic20KeyboardMatrix.SetRowSelect` only ever
   tracked one selected row (the first bit found clear in the mask), so this all-rows check always
   read row 0's state - genuinely nothing pressed there - and the KERNAL never proceeded to the
   real scan that would have found the actual key. Fixed: `SetRowSelect` now just latches the raw
   mask; `ReadColumns` ORs `~_pressedColumns[row]` across every row whose bit is 0, which
   degrades to the original single-row behavior when only one bit is clear (the normal per-row
   scan) and now also handles the all-rows case correctly.

3. **Wrong (row, col) table.** With the wiring and matrix model both fixed, keys registered but
   typed the *wrong* characters (typing "PRINT5" echoed "DW*J@"). `Vic20HostKeyMap.LetterMap`
   (also ported from the same reference project) didn't match this real KERNAL's actual scan
   order at all. Rather than guess again, generated the real table empirically: booted to BASIC,
   pressed each of the 64 matrix cells alone, read back the real PETSCII screen code the KERNAL
   echoed for it (filtering out cursor-blink noise - a raw before/after screen diff is dominated
   by the blink toggling `$20`/`$A0` at the cursor cell, unrelated to any real keypress). All 26
   letters and 10 digits came back with a full, distinct hit. Enter and Space needed a second,
   separate check (both showed as "nothing recognizable" in the raw scan): Enter confirmed by the
   KERNAL's screen line-pointer (`$D1`/`$D2`) advancing by exactly one row (22, the profile's
   column count) after that cell alone; Space confirmed by "A" + candidate + "B" echoing as
   `A <space> B`, not `AB`. Rewrote `Vic20HostKeyMap.LetterMap` in full with the verified table
   (`'@'` and a few less-common punctuation keys are a known, documented gap - not covered by
   this scan, not needed by anything this repo currently types).

Verified end-to-end through the *real* GUI pipeline, not a direct `machine.Keyboard.Press` call:
extended `PetEmulator.Screenshot` with `--type <text>`, using Avalonia.Headless's
`window.KeyPress`/`KeyRelease` (the same routed-event path a real OS keypress takes - focus,
bubble, `MainWindow`'s `KeyDown`/`KeyUp` handlers, `MainWindowViewModel.HandleKey`) instead of
calling into the machine directly. Typing `"PRINT2+2\n"` now genuinely echoes `PRINT22` (`+` isn't
in this tool's own tiny char-to-`Key` table yet) to the real rendered screen.

Also fixed while chasing this (real, but not root-cause): `MainWindow`/`PetMachineView`/
`Vic20MachineView` were calling `.Focus()` on the outer `ContentControl` (`MachineHost`) instead
of the actual leaf `Screen` control inside whichever machine view is current - each machine
View's code-behind now focuses its own `Screen` on `Loaded` and on every `DataContext` change
(covers reusing the same View instance across a same-type machine/profile switch). Didn't turn
out to be why typing failed here (the headless test harness routes key events by focus
independently of the OS-level quirk this was guarding against), but it's a real gap the same
investigation surfaced and a legitimate fix regardless.

## Bug 5: Enter didn't execute typed lines, just moved the cursor down

Reported as "po wciśnięciu enter przeskakuje mnie o wiersz do dołu a nie wysyła enter". The
`Vic20HostKeyMap.LetterMap` fix (Bug 4) had already shipped a wrong Enter cell, and the test that
should have caught it (`Vic20KeyboardBootTests`) was **itself a false positive** of exactly the
same shape as the boot-detection bug in Bug 3: it typed `"PRINT5\n"` and checked for a literal
`'5'` screen code - but `'5'` is *typed input*, echoed to screen the moment it's pressed,
regardless of whether Enter ever ran anything. The test stayed green through the entire time
Enter was broken.

The original Enter verification (Bug 4) picked `(3,7)` because pressing it alone advanced the
KERNAL's screen line-pointer (`$D1`/`$D2`) by exactly one row - the same signature a genuine
newline produces. But `(3,7)` is **CRSR-DOWN**, a different real key that moves the cursor with
the identical pointer arithmetic and never touches the input line at all. Line-pointer movement
alone can't distinguish "the KERNAL's line editor really processed and ran this line" from "the
cursor just moved" - re-verified properly by typing a full command
(`"PRINT2+2"` + candidate) and checking whether it actually *executes* (does `'4'` - a value that
cannot appear from echoing the typed digits - show up on screen). `(3,7)` never executes anything
however long it's given to run; `(1,7)` does. Fixed `Vic20HostKeyMap`'s `ReturnRow`/`ReturnCol` to
`(1, 7)`, and rewrote both the false-positive test (now types `"PRINT2+2"`, checks for `'4'`) and
`Vic20KeyboardMatrixTests`'s Enter assertion to match, plus added
`Vic20MachineTests.Enter_ActuallyExecutesTheTypedLine_NotJustCrsrDown` as an explicit regression
test for this exact CRSR-DOWN-vs-Enter confusion.

Also checked while investigating: whether the char ROM was being read with inverted bits. It
isn't - dumped the raw glyph for screen code 1 ('A') and it's a clean, recognizable 'A' shape.
Wrongly concluded from there that the white-text-on-blue look was genuine real VIC-20 default
behavior (see Bug 4 below for the correction - it wasn't the font, and it wasn't the default).

## Bug 4: colors reversed (root cause: `MOS6560.ReverseMode` polarity inverted)

The "not a bug" conclusion above was wrong on the actual color question, just right that the font
bits themselves weren't inverted. Re-checked against a fresh WebSearch (Lemon64/AtariAge/GitHub
6561.txt) after the user pushed back with a real screenshot (white text on blue) against what
real VIC-20 hardware actually shows (blue text on a white background, cyan/blue border): bit 3 of
`$900F` defaults to **1 = normal** (ink/paper in their respective places), **0 = reversed** - the
opposite of what its name suggests. `MOS6560.ReverseMode` read it as `!= 0` (bit set = reverse),
backwards from real silicon. Confirmed empirically: `BusObserver`-traced the real KERNAL boot
write to `$900F` - it writes `$1B` (bit 3 set) exactly once. Under the old (wrong) polarity that's
"reverse", swapping `ink`/`paper` in `Vic20RasterDisplay.RenderChar` for the entire default boot
screen (white on blue). Under the corrected polarity (bit 3 set = normal, no swap), the same `$1B`
produces blue text (`colorIndex`=6) on a white background (`screenColor`=1), border cyan
(`borderColor`=3) - matching real hardware exactly. Fixed: `ReverseMode => (reg & 0x08) == 0`.
One existing round-trip test (`MOS6560Tests.ReadWrite_RoundTripsThroughBaseAddressOffset`) had
baked in the old polarity's assumption (bit set = reverse) and had to flip its expectation; added
`ReverseMode_BitClear_IsReversed` to cover the case that let the polarity bug through unnoticed in
the first place. One `Vic20RasterDisplayTests` case had picked a register value (`0x11`, bit 3
clear) that happened to mean "normal" only under the old polarity - bumped to `0x19` (bit 3 set)
to keep meaning "normal" under the corrected one.

Verified end-to-end through the real GUI pipeline again (`PetEmulator.Screenshot --type`): typing
`"PRINT2+2\n"` now genuinely echoes `PRINT22` (`+` still isn't in the tool's own tiny
char-to-`Key` table), executes on Enter, prints `22`, and a fresh `READY.` prompt appears - the
complete, correct real BASIC direct-mode round trip.

## Screenshot tool

This dev environment has no screenshot utility (`import`/`scrot`/`xwd`) and no root to install
one. Built `lib/PetEmulator.Screenshot` instead: `Avalonia.Headless` + `Avalonia.Skia`, off-screen
Skia rendering, no OS display needed (works identically under `xvfb-run` or fully headless CI).

```
dotnet run --project lib/PetEmulator.Screenshot -- --machine pet|vic20 --ticks N --out path.png
  [--type "text\n"]
```

`--type` (added for Bug 4 above) simulates real routed key events via Avalonia.Headless's
`window.KeyPress`/`KeyRelease` - the actual GUI input pipeline, not a shortcut into the machine -
so it can catch focus/routing bugs a direct `machine.Keyboard.Press` test can't see.

Ticks the selected machine directly (`IMachineViewModel.Tick()`, same call `MainWindowViewModel`'s
render-loop timer makes) `N` times, then **disposes the ViewModel before pumping the Avalonia
dispatcher** - a live `DispatcherTimer` re-enqueues itself forever under a headless dispatcher's
time-fast-forwarding, so draining the job queue with one still armed never returns; this is the
one real gotcha the first version of this tool hit (hung consuming 100% CPU until killed).
