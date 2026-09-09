# PET profile cursor-blink verification

Asked directly: "zweryfikuj profile pet czy startują do mrygającego kursora" (verify the PET
profiles boot to a blinking cursor). Checked all 4 (`PetProfileCatalog.All`) by booting each to a
real `READY.` and diffing rendered frames over 80 render ticks (matching
`PetMachineViewModel.InstructionsPerTick`'s real cadence) - the same methodology
`docs/vic20-rendering-fixes.md`'s Bug 3 used to prove a blink is real, not a guess.

## Before

```text
pet-2001-8:  booted OK, 0/80 frames changed
pet-2001-32: booted OK, 3/80 frames changed
cbm-4032:    booted OK, 0/80 frames changed
cbm-8032:    booted OK, 0/80 frames changed
```

Only 1 of 4 profiles actually blinked.

## Root cause (cbm-4032 / cbm-8032 - fixed)

`PetRasterDisplay.GetCursorPosition()` branched on whether the profile has a CRTC
(`PetProfile.RequiresCrtc` - BASIC 4, CBM 4032/8032): CRTC-equipped profiles read the cursor
position from the CRTC's own hardware register (`R14`/`R15`), non-CRTC profiles from the KERNAL's
zero-page screen-line pointer (`$C4`/`$C5`/`$C6`). Traced with `PetMachine.BusObserver` against a
real CBM 4032 boot: the KERNAL writes `R14`/`R15` **exactly once**, during CRTC init, to `$0000` -
immediately invalid (`CursorAddress - DisplayStartAddress` is always negative) - and never
touches them again for the rest of a session. Directly reading `$C4`/`$C5`/`$C6` for the same
machine at the same moment found a genuine, live, valid position (`offset=160`, a real in-range
cell). Confirmed the same for CBM 8032 (`offset=320`, also valid).

This KERNAL doesn't drive the CRTC's hardware cursor feature at all - cursor tracking (and the
software blink built on top of it) works identically whether or not a CRTC is present. Fixed:
removed the CRTC branch from `GetCursorPosition` entirely, always use the zero-page pointer. The
now-unused `Crtc6545?` constructor parameter was removed from `PetRasterDisplay` (one production
caller, `PetMachineViewModel`, plus a few tests - all updated). The existing unit test
(`Cursor_WithCrtc_LocatedByCrtcCursorRegister`) had made exactly the same wrong assumption -
it wrote directly to a standalone `Crtc6545`'s register, which the real KERNAL never actually
does - rewritten to write the zero-page pointer instead, proving a CRTC-equipped profile's cursor
works the same way a non-CRTC one does.

## After

```text
pet-2001-8:  booted OK, 0/80 frames changed   <- still open, see below
pet-2001-32: booted OK, 3/80 frames changed
cbm-4032:    booted OK, 3/80 frames changed   <- fixed
cbm-8032:    booted OK, 3/80 frames changed   <- fixed
```

## Still open: pet-2001-8 (BASIC 1)

`$C4`/`$C5`/`$C6` read back `$D0`/`$02`/`$E6` (pointer `$02D0`) right after boot - nowhere near
`VideoRamStart` (`$8000`), and stable there for the entire session (not a timing issue). BASIC 1's
zero-page layout is well known to differ substantially from BASIC 2/4's (a real, documented ROM
revision fact, not this repo's assumption) - the real cursor-tracking address for this specific
ROM hasn't been found yet. Started `docs/pet-disassembly/` (real `rom-1-*.bin` chips
disassembled) to continue the search the same way the VIC-20 keyboard/cursor investigation did,
but the `eor #$80` / `$C4`-adjacent search patterns that worked there didn't turn up an obvious
equivalent here. Not fixed in this pass - `pet-2001-32` (BASIC 2, already this repo's default/most
-used profile throughout its own development) works correctly; `pet-2001-8` is the one remaining
gap.
