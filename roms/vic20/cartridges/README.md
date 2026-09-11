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
