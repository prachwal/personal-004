#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

out="build/pet-tape-loading"
mkdir -p "$out"

dotnet run --project src/PetEmulator.Cli --no-restore -- debug scripts/pet-tape-loading.dbg \
    | tee "$out/pet-tape-loading.log"

log="$out/pet-tape-loading.log"
grep -q '📼 Datasette: No tape' "$log"
grep -q '^tape loaded: tower-and-dragon-town.tap (549507 pulses)$' "$log"
grep -q '📼 Datasette: tower-and-dragon-town.tap - press play$' "$log"
grep -q 'halted=False' "$log"
! grep -q '^error:' "$log"

echo "PET tape-loading smoke test passed: $log"
