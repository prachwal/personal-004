#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

out="build/vic20-boot"
mkdir -p "$out"

dotnet run --project src/PetEmulator.Cli --no-restore -- vic20-debug scripts/vic20-boot.dbg \
    | tee "$out/vic20-boot.log"

log="$out/vic20-boot.log"
grep -q '^roms set:' "$log"
grep -q '^profile=VIC-20 NTSC (unexpanded)' "$log"
grep -q 'halted=False' "$log"
! grep -q '^error:' "$log"

echo "VIC-20 boot smoke test passed: $log"
