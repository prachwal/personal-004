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

## SuperPET Waterloo 6809 ROM disassembly

`waterloo-a000-bfff.asm` / `waterloo-c000-dfff.asm` / `waterloo-e000-ffff.asm` - linear disassembly
of `roms/pet/cbm-8032/waterloo-*.bin` (the three 8K Waterloo 6809 firmware images), generated with
[Capstone](https://www.capstone-engine.org/)'s `CS_ARCH_M680X`/`CS_MODE_M680X_6809` mode - a real
6809 disassembler, not hand-decoded. Kept on disk as ground truth for
`docs/pet/waterloo-investigation.md`'s boot/menu/disk-load trail, most of which started from manual
byte-level decoding before this was generated (that manual work occasionally mislabeled a
register, e.g. read an indexed postbyte's register field as `U` where it's actually `S`; this
Capstone-verified listing is the one to trust when the two disagree).

Regenerate with:

```bash
pip install --break-system-packages capstone   # or a venv; needs CS_MODE_M680X_6809 support
python3 - <<'EOF'
import capstone
def disasm(path, base, out):
    md = capstone.Cs(capstone.CS_ARCH_M680X, capstone.CS_MODE_M680X_6809)
    data = open(path, 'rb').read()
    with open(out, 'w') as f:
        for insn in md.disasm(data, base):
            f.write(f"{insn.address:04X}: {insn.bytes.hex():<10} {insn.mnemonic:<8} {insn.op_str}\n")
disasm('roms/pet/cbm-8032/waterloo-a000-bfff.970018-12.bin', 0xA000, 'docs/pet/disassembly/waterloo-a000-bfff.asm')
disasm('roms/pet/cbm-8032/waterloo-c000-dfff.970019-12.bin', 0xC000, 'docs/pet/disassembly/waterloo-c000-dfff.asm')
disasm('roms/pet/cbm-8032/waterloo-e000-ffff-970034-12.bin', 0xE000, 'docs/pet/disassembly/waterloo-e000-ffff.asm')
EOF
```

Linear disassembly with no symbol table, same data-vs-code caveat as above - string tables and
jump tables (both common in this ROM, e.g. the menu's module-name table and the `$B0AE` system-call
dispatch table) will be misread as instructions in the listing. Cross-reference against
`docs/pet/waterloo-investigation.md`'s known routine addresses before trusting a stretch of this
listing at face value.
