#!/usr/bin/env python3
"""Download selected VIC-20 visual/audio test programs from Zimmers.NET."""

from pathlib import Path
from urllib.request import urlopen


ROOT = Path(__file__).resolve().parents[1]
BASE = "https://www.zimmers.net/anonftp/pub/cbm/vic20/"
FILES = {
    "cartridges/IFR-Flight-Simulator.prg": BASE + "carts/8k/IFR%20(Flight%20Simulator).prg",
    "test-programs/Alphoids.prg": BASE + "games/unexpanded/Alphoids.prg",
    "test-programs/Alien-Blitz.NTSC.prg": BASE + "games/unexpanded/Alien%20Blitz.NTSC.prg",
    "test-programs/Alien-Blitz.PAL.prg": BASE + "games/unexpanded/Alien%20Blitz.PAL.prg",
}


def main() -> None:
    for relative_path, url in FILES.items():
        destination = ROOT / "roms" / "vic20" / relative_path
        destination.parent.mkdir(parents=True, exist_ok=True)
        print(f"Downloading {url} -> {destination}")
        with urlopen(url) as response:
            destination.write_bytes(response.read())


if __name__ == "__main__":
    main()
