#!/usr/bin/env python3
"""Wrap VIC-20 BASIC PRG files in standard A000 autostart cartridges."""

from __future__ import annotations

import json
import struct
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "roms" / "vic20" / "test-programs"
OUTPUT = ROOT / "roms" / "vic20" / "cartridges"
MANIFEST = OUTPUT / "test-program-profiles.json"

# VIC-20 BASIC autostart routine from the published "Auto-Start BASIC Programs"
# layout. It copies the BASIC object stored at $A043 into the configured BASIC area,
# initializes BASIC pointers and places the abbreviated RUN + RETURN sequence in
# the keyboard buffer.
AUTOSTART_STUB = bytes([
    0x09, 0xA0, 0x56, 0xFF, 0x41, 0x30, 0xC3, 0xC2, 0xCD,
    0x20, 0x8D, 0xFD, 0xBD, 0x81, 0x02, 0x95, 0x08,
    0xBD, 0x29, 0xA0, 0x9D, 0x81, 0x02, 0xBD, 0x2B,
    0xA0, 0x9D, 0x77, 0x02, 0xE8, 0xE0, 0x02, 0x90,
    0xEA, 0xF0, 0xF3, 0x86, 0xC6, 0x4C,
    0x32, 0xFD, 0x2E, 0xA0, 0x52, 0xD5, 0x0D, 0x00,
    0x43, 0xA0, 0x00, 0x00, 0x97, 0x34, 0x36, 0x2C,
    0xC2, 0x28, 0x39, 0x29, 0x3A, 0x9C, 0x3A, 0x99,
    0x22, 0x93, 0x22, 0x00,
])
PROGRAM_OFFSET = 0x43
MACHINE_CODE_LOADER_OFFSET = 0x1F00
MACHINE_CODE_BOOTSTRAP_ADDRESS = 0xA000 + MACHINE_CODE_LOADER_OFFSET
MACHINE_CODE_COPY_OFFSET = 0x1F40

# The downloaded programs are PRGs with a BASIC SYS stub followed by machine
# code.  The normal 67-byte wrapper is kept as the cartridge header/payload
# layout, but its entry point is redirected to this two-stage loader.  The
# first stage lets BASIC finish its cold start, then BASIC calls the second
# stage, which copies the PRG into RAM after cold start has stopped clearing it.
MACHINE_CODE_BOOTSTRAP = bytes([
    0x20, 0x8D, 0xFD,       # JSR $FD8D (KERNAL/RAM initialization)
    0xA9, 0x53, 0x8D, 0x77, 0x02, # "SYS " in the keyboard buffer
    0xA9, 0x59, 0x8D, 0x78, 0x02,
    0xA9, 0x53, 0x8D, 0x79, 0x02,
    0xA9, 0x20, 0x8D, 0x7A, 0x02,
    0xA9, 0x34, 0x8D, 0x7B, 0x02, # 48960 = $BF40
    0xA9, 0x38, 0x8D, 0x7C, 0x02,
    0xA9, 0x39, 0x8D, 0x7D, 0x02,
    0xA9, 0x36, 0x8D, 0x7E, 0x02,
    0xA9, 0x30, 0x8D, 0x7F, 0x02,
    0xA9, 0x0D, 0x8D, 0x80, 0x02,
    0xA9, 0x0A, 0x85, 0xC6, # ten characters, then normal BASIC cold start
    0x4C, 0x32, 0xFD,       # JMP $FD32
])

MACHINE_CODE_COPY = bytes([
    0xA9, 0x43, 0x85, 0xFB, # source pointer = $A043
    0xA9, 0xA0, 0x85, 0xFC,
    0xA9, 0x01, 0x85, 0xFD, # destination pointer = $1001
    0xA9, 0x10, 0x85, 0xFE,
    0xA2, 0x0E,              # copy 14 full pages (the PRG payload is 0xDFF bytes)
    0xA0, 0x00,
    0xB1, 0xFB, 0x91, 0xFD, # LDA (source),Y / STA (destination),Y
    0xC8, 0xD0, 0xF9,       # INY / BNE copy-page
    0xE6, 0xFC, 0xE6, 0xFE, # advance both pointers by one page
    0xCA, 0xD0, 0xF0,       # DEX / BNE copy-page
    0xA9, 0x01, 0x85, 0x2B, # BASIC text starts at $1001
    0xA9, 0x10, 0x85, 0x2C,
    0x4C, 0x0E, 0x10,       # SYS 4110 target from the original BASIC stub
])

if len(AUTOSTART_STUB) > PROGRAM_OFFSET:
    raise ValueError("VIC-20 autostart stub exceeds its documented program offset")

PROGRAMS = [
    ("Alphoids.prg", "Alphoids-autostart.crt", "Alphoids"),
    ("Alien-Blitz.NTSC.prg", "Alien-Blitz-NTSC-autostart.crt", "Alien Blitz NTSC"),
    ("Alien-Blitz.PAL.prg", "Alien-Blitz-PAL-autostart.crt", "Alien Blitz PAL"),
    ("vic20-sound-test.prg", "vic20-sound-test-autostart.crt", "VIC-20 Sound Test"),
]


def chip(data: bytes) -> bytes:
    header = b"CHIP" + struct.pack(">IHHHH", 0x10 + len(data), 2, 0, 0xA000, len(data))
    return header + data


def crt(name: str, data: bytes) -> bytes:
    header = bytearray(0x40)
    header[:16] = b"C64 CARTRIDGE   "
    struct.pack_into(">IHH", header, 0x10, 0x40, 0x0100, 2)
    encoded_name = name.encode("ascii")[:32]
    header[0x20:0x20 + len(encoded_name)] = encoded_name
    return bytes(header) + chip(data)


def make_cartridge(source: Path, destination: Path, name: str) -> None:
    image = source.read_bytes()
    if len(image) < 3 or image[:2] != b"\x01\x10":
        raise ValueError(f"{source.name}: expected VIC-20 BASIC PRG load address $1001")

    payload = bytearray(AUTOSTART_STUB + bytes(PROGRAM_OFFSET - len(AUTOSTART_STUB)))
    payload[0x09:0x0C] = bytes([0x4C, MACHINE_CODE_BOOTSTRAP_ADDRESS & 0xFF,
                                MACHINE_CODE_BOOTSTRAP_ADDRESS >> 8])
    payload.extend(image[2:])
    payload.extend(bytes([0xFF]) * (MACHINE_CODE_LOADER_OFFSET - len(payload)))
    payload.extend(MACHINE_CODE_BOOTSTRAP)
    payload.extend(bytes([0xFF]) * (MACHINE_CODE_COPY_OFFSET - len(payload)))
    payload.extend(MACHINE_CODE_COPY)
    if len(payload) > 0x2000:
        raise ValueError(f"{source.name}: wrapper does not fit in an 8K cartridge")

    destination.write_bytes(crt(name, bytes(payload) + bytes([0xFF]) * (0x2000 - len(payload))))


def main() -> None:
    OUTPUT.mkdir(parents=True, exist_ok=True)
    profiles = []
    for source_name, destination_name, title in PROGRAMS:
        source = SOURCE / source_name
        destination = OUTPUT / destination_name
        make_cartridge(source, destination, title)
        profile = {
            "id": destination.stem.lower().replace("-", "_"),
            "name": f"{title} (autostart cartridge)",
            "cartridge": destination.name,
            "sourcePrg": f"../test-programs/{source_name}",
            "recommendedExpansionPreset": "Unexpanded",
            "cartridgeRange": "$A000-$BFFF",
            "requiresResetAfterMount": True,
            "notes": "The original PRG is copied to $1001 after BASIC initialization and entered at its SYS target $100E.",
        }
        if source_name.startswith("Alien-Blitz"):
            profile["optionalRamCartridge"] = {
                "pluginId": "vic20-ram-24k",
                "pluginAssembly": "PetEmulator.Vic20.Cartridge.Ram.24K.dll",
                "image": "vic20-ram-24k.bin",
                "resources": "$2000-$7FFF",
            }
        profiles.append(profile)

    MANIFEST.write_text(json.dumps({"profiles": profiles}, indent=2) + "\n", encoding="utf-8")


if __name__ == "__main__":
    main()
