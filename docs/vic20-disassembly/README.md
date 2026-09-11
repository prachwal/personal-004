# VIC-20 ROM disassembly

Plain `da65` (cc65) disassembly of the real ROMs in `roms/vic20/`, kept on disk (not regenerated
on demand) so future debugging can grep it directly instead of re-running `da65` each time - see
`docs/vic20-rendering-fixes.md` for the investigation that first needed this.

Regenerate with:

```bash
da65 --cpu 6502 --start-addr 0xE000 -o docs/vic20-disassembly/kernal.asm roms/vic20/kernal.bin
da65 --cpu 6502 --start-addr 0xC000 -o docs/vic20-disassembly/basic.asm roms/vic20/basic.bin
```

No config/label file was supplied, so `da65`'s linear disassembly occasionally misreads a data
table as code (e.g. the row-start-address table at `LEDFD` disassembles as garbage `brk`
instructions) - real data, not a bug in the disassembly; read raw bytes from the `.bin` directly
(e.g. via a short Python one-liner) when a specific table's contents matter, as done for
`LEDFD` while chasing the color-RAM bug.
