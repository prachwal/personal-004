# VIC-20 ROM images

Copied from `cpu-vibe-001`'s own `roms/commodore-vic-20/` (same provenance chain that repo
recorded, extracted from VICE 3.10's official source distribution - the same class of source this
repo's own `roms/pet/*` images come from).

| File | Address | Source |
| ------ | --------- | -------- |
| `vic20-chargen.bin` | $8000 | VICE 3.10 `data/VIC20/chargen-901460-03.bin` (default NTSC) |
| `basic.bin` | $C000 | VICE 3.10 `data/VIC20/basic-901486-01.bin` |
| `kernal.bin` | $E000 | VICE 3.10 `data/VIC20/kernal.901486-07.bin` (default NTSC) |

Sizes: `basic.bin` 8,192 B ($2000), `kernal.bin` 8,192 B ($2000), `vic20-chargen.bin` 4,096 B
($1000) - matches `Vic20RomManifest`.
