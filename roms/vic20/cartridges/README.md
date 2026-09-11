# VIC-20 cartridge fixtures

These small CRT images are deterministic test fixtures, not commercial ROM dumps.
Regenerate them from the repository root with:

```bash
python3 tools/generate-vic20-crt-fixtures.py
```

- `vic20-test-single.crt` — one bank at `$A000`;
- `vic20-test-banked.crt` — banks `0` and `1` at `$A000`;
- `vic20-test-invalid-layout.crt` — deliberately mismatched bank sizes.
- `vic20-ram-3k.bin`, `vic20-ram-8k.bin`, `vic20-ram-16k.bin`, `vic20-ram-24k.bin`,
  `vic20-ram-35k.bin` — zero-initialized images for the RAM expansion plugins.
- `vic20-test-6000.prg` and `vic20-test-a000.prg` — minimal PRG loader fixtures.
- `IFR-Flight-Simulator.prg` — downloadable 8 KB cartridge test image; fetch it
  with `python3 tools/download-vic20-test-programs.py`.
- `Alphoids-autostart.crt`, `Alien-Blitz-NTSC-autostart.crt` and
  `Alien-Blitz-PAL-autostart.crt` — generated 8 KB A000 autostart wrappers for
  the corresponding BASIC PRG files. Generate them with:

  ```bash
  python3 tools/generate-vic20-autostart-basic-cartridges.py
  ```

  Their launch metadata is in `test-program-profiles.json`. Use the
  `Unexpanded` profile, mount one image, then reset the VIC-20. The images
  consume `$A000-$BFFF`, so they conflict with the `+All (35K)` profile's
  block 5 RAM. If an additional RAM cartridge is needed, use the existing
  `vic20-ram-24k.bin` with `PetEmulator.Vic20.Cartridge.Ram.24K.dll`; it
  consumes only `$2000-$7FFF` and can coexist with these wrappers.
- `vic20-sound-test-autostart.crt` — generated 8 KB A000 autostart wrapper for
  `../test-programs/vic20-sound-test.prg`. It plays three original test tones
  through VIC oscillator 1 and requires no RAM expansion. Build the PRG first
  with `python3 tools/build-vic20-sound-test.py`, then regenerate the wrapper
  with `python3 tools/generate-vic20-autostart-basic-cartridges.py`.
- `vic20-mc146818-rtc.bin` — raw 8 KB A000 native autostart image for the
  MC146818 RTC plugin. It contains the VIC-20 KERNAL autostart vector and
  signature, then uses the same two-stage `$BF00`/`SYS $BF40` bootstrap as the
  working VIC-20 program cartridges. The RTC stage returns to normal BASIC
  and keeps the clock updated through IRQ. The plugin maps the RTC index/data registers at
  `$9C00/$9C01` and asserts CPU IRQ on update-ended.
  It can coexist with the `+24K` RAM plugin, but not with `+35K` because the
  image consumes `$A000-$BFFF`.
