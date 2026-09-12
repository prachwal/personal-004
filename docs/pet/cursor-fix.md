# PET profile cursor-blink verification

Asked directly: "zweryfikuj profile pet czy startują do mrygającego kursora" (verify the PET
profiles boot to a blinking cursor). Checked all 4 (`PetProfileCatalog.All`) by booting each to a
real `READY.` and diffing rendered frames over 80 render ticks (matching
`PetMachineViewModel.InstructionsPerTick`'s real cadence) - the same methodology
`docs/vic20/rendering-fixes.md`'s Bug 3 used to prove a blink is real, not a guess.

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
now-unused `MT6545?` constructor parameter was removed from `PetRasterDisplay` (one production
caller, `PetMachineViewModel`, plus a few tests - all updated). The existing unit test
(`Cursor_WithCrtc_LocatedByCrtcCursorRegister`) had made exactly the same wrong assumption -
it wrote directly to a standalone `MT6545`'s register, which the real KERNAL never actually
does - rewritten to write the zero-page pointer instead, proving a CRTC-equipped profile's cursor
works the same way a non-CRTC one does.

## pet-2001-8 (BASIC 1) - found and fixed too

Asked directly to finish this one: "wykonaj disaemble 2001-8 odszukaj właściwe adresy". `$C4`/
`$C5`/`$C6` read back `$D0`/`$02`/`$E6` (pointer `$02D0`) right after boot - nowhere near
`VideoRamStart` (`$8000`), stable for the whole session. The `eor #$80` disassembly search that
worked for VIC-20 didn't turn up an obvious equivalent here, so found the real addresses
empirically instead: dumped the full zero page before/after typing a recognizable string, found
three (lo, hi) pairs that resolve to a valid in-range screen offset, then disambiguated with a
tighter test - typing the *same single character* five times in a row and watching which byte
increments by **exactly 1** each time (a column counter's signature; a coincidentally-valid
pointer pair that isn't real tracking won't do this). `$E2` did, every time; `$E0`/`$E1` is its
matching line pointer (confirmed live - it advances by one row's worth after a wrapped line, and
stays put otherwise). BASIC 1 uses `$E0`/`$E1`/`$E2` - same 3-byte shape as BASIC 2/4's `$C4`/
`$C5`/`$C6`, different absolute addresses (a real, distinct ROM revision fact).

### The `PetCursorTracking` per-profile contract

Since the fix for CBM 4032/8032 above already established "cursor tracking is always the
zero-page-pointer method" and now BASIC 1 needs *different addresses* for that same method, the
addresses became genuinely per-profile data - not something the previous three
`private const ushort` fields in `PetRasterDisplay` could express. New:

```csharp
public sealed record PetCursorTracking(ushort LineLowAddress, ushort LineHighAddress, ushort ColumnAddress)
{
    public static PetCursorTracking Basic2Convention { get; } = new(0x00C4, 0x00C5, 0x00C6);
    public static PetCursorTracking Basic1Convention { get; } = new(0x00E0, 0x00E1, 0x00E2);
}
```

`PetProfile` gained a new required `CursorTracking` field (same pattern as its existing
`RomManifest`/`CharacterRomPath` - explicit per profile, no implicit default) - `PetProfileCatalog`
gives `pet-2001-8` `Basic1Convention`, the other three `Basic2Convention`. `PetRasterDisplay
.GetCursorPosition()` reads `_profile.CursorTracking` instead of its own hardcoded constants -
the three old private consts are gone.

## After

```text
pet-2001-8:  booted OK, 3/80 frames changed   <- fixed
pet-2001-32: booted OK, 3/80 frames changed
cbm-4032:    booted OK, 3/80 frames changed
cbm-8032:    booted OK, 3/80 frames changed
```

All 4 profiles blink correctly now.

## Waterloo 6809 menu cursor

Waterloo 6809 does not use the PET BASIC zero-page cursor pointers `$C4-$C6` or
`$E0-$E2`. After the reset sequence reaches the interactive menu, it writes ordinary
ASCII directly to the 80-column screen RAM at `$8000-$87CF` and marks the selected
menu cell with bit 7. The initial menu was verified after 3,000,000 6809 instructions:

```text
Waterloo microSystems

Select :
  setup
  monitor
  apl
  basic
  edit
  fortran
  pascal
  development
```

The selected blank cell contains `$A0`, not `$20`: it is ASCII space with reverse
video. Therefore the renderer must strip bit 7 before selecting the second 2 KiB
ASCII character-ROM bank and invert the resulting glyph. The profile uses the
`ScreenHighBit` cursor strategy; its generic PET BASIC zero-page pointer is not
read and cannot create a second, false blinking cursor.
