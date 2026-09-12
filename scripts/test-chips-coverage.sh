#!/usr/bin/env bash
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
project="$repo_root/tests/PetEmulator.Chips.Tests/PetEmulator.Chips.Tests.csproj"
settings="$repo_root/tests/coverage.runsettings"

dotnet test "$project" --no-restore --settings "$settings" \
  --collect:"XPlat Code Coverage" --verbosity minimal

report="$(find "$repo_root/tests/PetEmulator.Chips.Tests/TestResults" \
  -name coverage.cobertura.xml -type f -printf '%T@ %p\n' 2>/dev/null \
  | sort -nr | awk 'NR == 1 { print $2 }')"

if [[ -z "$report" ]]; then
  echo "Coverage gate: nie znaleziono coverage.cobertura.xml" >&2
  exit 1
fi

chips=(MC146818 MOS2114 MOS6522 MOS6560 MOS6702 MT6520 MT6545)
for chip in "${chips[@]}"; do
  class="$(awk -v chip="$chip" '
    $0 ~ "<class name=\"PetEmulator.Chips." chip "\"" { inside=1 }
    inside { print }
    inside && /<\/class>/ { exit }
  ' "$report")"

  if [[ -z "$class" ]]; then
    echo "Coverage gate: brak klasy PetEmulator.Chips.$chip" >&2
    exit 1
  fi

  class_header="${class%%$'\n'*}"
  if [[ "$class_header" != *'line-rate="1"'* || "$class_header" != *'branch-rate="1"'* ]]; then
    echo "Coverage gate: PetEmulator.Chips.$chip nie ma 100% line/branch coverage" >&2
    exit 1
  fi

  method_gaps="$(printf '%s\n' "$class" | awk '/<method / && $0 !~ /line-rate="1"/ { print }')"
  if [[ -n "$method_gaps" ]]; then
    echo "Coverage gate: PetEmulator.Chips.$chip nie ma 100% method coverage" >&2
    exit 1
  fi
done

echo "Coverage gate: 100% line, branch i method dla: ${chips[*]}"
