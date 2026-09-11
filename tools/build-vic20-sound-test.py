#!/usr/bin/env python3
"""Assemble the small VIC-20 sound test PRG with ca65/ld65."""

from __future__ import annotations

import subprocess
import tempfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "roms" / "vic20" / "test-programs" / "vic20-sound-test.asm"
OUTPUT = ROOT / "roms" / "vic20" / "test-programs" / "vic20-sound-test.prg"

LINKER_CONFIG = """\
MEMORY {
    RAM: start = $1001, size = $1FFF, file = %O;
}
SEGMENTS {
    CODE: load = RAM, type = ro;
}
"""


def main() -> None:
    with tempfile.TemporaryDirectory(prefix="vic20-sound-") as directory:
        work = Path(directory)
        obj = work / "sound-test.o"
        binary = work / "sound-test.bin"
        config = work / "sound-test.cfg"
        config.write_text(LINKER_CONFIG, encoding="ascii")
        subprocess.run(["ca65", str(SOURCE), "-o", str(obj)], check=True)
        subprocess.run(["ld65", str(obj), "-C", str(config), "-o", str(binary)], check=True)
        payload = binary.read_bytes()
        if not payload or len(payload) > 0x1FFF:
            raise RuntimeError(f"unexpected sound test size: {len(payload)} bytes")
        OUTPUT.write_bytes(b"\x01\x10" + payload)
        print(f"wrote {OUTPUT} ({len(payload) + 2} bytes)")


if __name__ == "__main__":
    main()
