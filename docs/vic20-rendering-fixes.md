# VIC-20 rendering bug fixes

Three bugs reported after actually running the GUI (colors wrong, aspect ratio wrong, no
blinking cursor) - all root-caused using the real KERNAL disassembly
(`docs/vic20-disassembly/kernal.asm`, `docs/vic20-disassembly/basic.asm` - `da65`, kept on disk
per the workflow this doc records for next time) plus `PetMachine.BusObserver`-style register/
memory tracing (mirrors `docs/pet-debug-tools.md`'s methodology).

## Bug 1: colors wrong (root cause: color-RAM address offset)

`Vic20RasterDisplay` read color RAM at `ColorRamStart + col + row*cols` - offset 0 for cell
(0,0). Real VIC-I hardware doesn't fetch color RAM from a fixed offset: it fetches using the same
low bits of the video matrix address as the screen fetch, so a screen relocated away from offset
0 (which the real KERNAL does - see Bug 3 below) needs a matching color-RAM offset,
`ScreenMatrixBase & 0x3FF` (color RAM only has 10 useful address lines - the matrix's high bits
select a 512-byte page, but wherever within that page it starts also shifts where color RAM
starts). Confirmed empirically: with the real boot-time `ScreenMatrixBase` = $3E00, tracing real
writes showed the KERNAL painting color RAM at $9600-$97F9 (offset $200), exactly
`$3E00 & 0x3FF`. Fixed: `Vic6560.ColorMatrixOffset` (new property) added into the color-RAM
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

## Screenshot tool

This dev environment has no screenshot utility (`import`/`scrot`/`xwd`) and no root to install
one. Built `src/PetEmulator.Screenshot` instead: `Avalonia.Headless` + `Avalonia.Skia`, off-screen
Skia rendering, no OS display needed (works identically under `xvfb-run` or fully headless CI).

```
dotnet run --project src/PetEmulator.Screenshot -- --machine pet|vic20 --ticks N --out path.png
```

Ticks the selected machine directly (`IMachineViewModel.Tick()`, same call `MainWindowViewModel`'s
render-loop timer makes) `N` times, then **disposes the ViewModel before pumping the Avalonia
dispatcher** - a live `DispatcherTimer` re-enqueues itself forever under a headless dispatcher's
time-fast-forwarding, so draining the job queue with one still armed never returns; this is the
one real gotcha the first version of this tool hit (hung consuming 100% CPU until killed).
