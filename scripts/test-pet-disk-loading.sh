#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

out="build/pet-disk-loading"
mkdir -p "$out"

dotnet run --project src/PetEmulator.Cli --no-restore -- debug scripts/pet-disk-loading.dbg \
    | tee "$out/pet-disk-loading.log"

log="$out/pet-disk-loading.log"
grep -q '^disk mounted: games-1.d64 on device 8$' "$log"
grep -q '💾 Drive 8: games-1.d64$' "$log"
grep -q 'halted=False' "$log"
! grep -q '^error:' "$log"

echo "PET disk-loading smoke test passed: $log"
