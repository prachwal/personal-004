# PET BASIC 1 ROM disassembly

Plain `da65` (cc65) disassembly of `roms/pet/pet-2001-8/*.bin` (the original PET 2001, BASIC 1 -
6 separate 2K chips), kept on disk for the same reason `docs/vic20/disassembly/` is: grep it
directly instead of re-running `da65` each time. Started while chasing the BASIC 1 cursor-blink
gap noted in `docs/pet/cursor-fix.md` - not yet found (BASIC 1's zero-page layout differs from
BASIC 2/4's, and the obvious `eor #$80`/`$C4`-`$C6` search patterns that worked for BASIC 2 and
VIC-20 didn't turn up an equivalent here).

Regenerate with:

```bash
da65 --cpu 6502 --start-addr 0xC000 -o docs/pet/disassembly/rom-c000.asm roms/pet/pet-2001-8/rom-1-c000.901439-01.bin
da65 --cpu 6502 --start-addr 0xC800 -o docs/pet/disassembly/rom-c800.asm roms/pet/pet-2001-8/rom-1-c800.901439-05.bin
da65 --cpu 6502 --start-addr 0xD000 -o docs/pet/disassembly/rom-d000.asm roms/pet/pet-2001-8/rom-1-d000.901439-02.bin
da65 --cpu 6502 --start-addr 0xD800 -o docs/pet/disassembly/rom-d800.asm roms/pet/pet-2001-8/rom-1-d800.901439-06.bin
da65 --cpu 6502 --start-addr 0xE000 -o docs/pet/disassembly/rom-e000.asm roms/pet/pet-2001-8/rom-1-e000.901439-03.bin
da65 --cpu 6502 --start-addr 0xF000 -o docs/pet/disassembly/rom-f000.asm roms/pet/pet-2001-8/rom-1-f000.901439-04.bin
da65 --cpu 6502 --start-addr 0xF800 -o docs/pet/disassembly/rom-f800.asm roms/pet/pet-2001-8/rom-1-f800.901439-07.bin
```

No config/label file was supplied, so linear disassembly occasionally misreads a data table as
code (same caveat as `docs/vic20/disassembly/README.md`).
