# VIC-20 audio/video test programs

Download the selected programs from Zimmers.NET from the repository root:

```bash
python3 tools/download-vic20-test-programs.py
```

- `Alphoids.prg` — unexpanded VIC-20 program; the archive description notes that it plays the Star Wars theme.
- `Alien-Blitz.NTSC.prg` — unexpanded NTSC display test.
- `Alien-Blitz.PAL.prg` — unexpanded PAL display test.

These are normal RAM-loaded programs, not cartridge ROMs. The current
cartridge picker is only for cartridge images, so these files require the
existing tape/disk loading flow or a future direct PRG-to-RAM command.
`IFR-Flight-Simulator.prg` is kept separately under `roms/vic20/cartridges/`
because it is an 8 KB cartridge image.
