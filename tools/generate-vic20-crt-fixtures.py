#!/usr/bin/env python3
"""Generate small deterministic CRT fixtures used by VIC-20 tests."""

from pathlib import Path
import struct


ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / "roms" / "vic20" / "cartridges"


def chip(data: bytes, bank: int, load_address: int = 0xA000) -> bytes:
    header = b"CHIP" + struct.pack(">IHHHH", 0x10 + len(data), 2, bank, load_address, len(data))
    return header + data


def crt(name: str, chips: list[bytes]) -> bytes:
    header = bytearray(0x40)
    header[0:16] = b"C64 CARTRIDGE   "
    struct.pack_into(">IHH", header, 0x10, 0x40, 0x0100, 2)
    header[0x20:0x20 + len(name.encode("ascii"))] = name.encode("ascii")
    return bytes(header) + b"".join(chips)


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    (OUTPUT / "vic20-test-single.crt").write_bytes(crt("VIC-20 test single", [chip(b"S", 0)]))
    (OUTPUT / "vic20-test-banked.crt").write_bytes(
        crt("VIC-20 test banked", [chip(b"A", 0), chip(b"B", 1)])
    )
    (OUTPUT / "vic20-test-invalid-layout.crt").write_bytes(
        crt("VIC-20 invalid layout", [chip(b"A", 0), chip(b"BC", 1)])
    )
    for name, size in [("3k", 0x0C00), ("8k", 0x2000), ("16k", 0x4000),
                       ("24k", 0x6000), ("35k", 0x8C00)]:
        (OUTPUT / f"vic20-ram-{name}.bin").write_bytes(bytes(size))
    (OUTPUT / "vic20-test-6000.prg").write_bytes(struct.pack("<H", 0x6000) + b"P")
    (OUTPUT / "vic20-test-a000.prg").write_bytes(struct.pack("<H", 0xA000) + b"Q")


if __name__ == "__main__":
    main()
