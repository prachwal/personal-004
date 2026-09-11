#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

out="build/vic20-tape-loading"
mkdir -p "$out"

dotnet run --project src/PetEmulator.Cli --no-restore -- vic20-debug scripts/vic20-tape-loading.dbg \
    | tee "$out/vic20-tape-loading.log"

log="$out/vic20-tape-loading.log"
grep -q '📼 Datasette: No tape' "$log"
grep -q '^tape loaded: hello-vic.tap (22560 pulses)$' "$log"
grep -q '📼 Datasette: hello-vic.tap - press play$' "$log"
grep -q 'halted=False' "$log"
! grep -q '^error:' "$log"

echo "VIC-20 tape-loading smoke test passed: $log"
