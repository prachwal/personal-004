#!/usr/bin/env python3
import json
import sys

path = sys.argv[1] if len(sys.argv) > 1 else "build/cpc464-sieve-benchmark.json"
with open(path, encoding="utf-8") as f:
    data = json.load(f)
sieve = data["sieve"]
emulated_seconds = (sieve["endCycleCount"] - sieve["startCycleCount"]) / 4_000_000
wall = sieve["wallClockSeconds"]
factor = emulated_seconds / wall if wall else 0
prime_pass = sieve.get("completed", False) and sieve.get("primeCount") == 1899
tape_pass = data.get("tapeRoundTrip", {}).get("passed", False)
print(f"real-time factor (emulated seconds / wall seconds): {factor:.3f}x")
print(f"prime-count correctness: {'PASS' if prime_pass else 'FAIL'}")
print(f"tape round-trip: {'PASS' if tape_pass else 'FAIL'}")
print(f"sieve completion: {'PASS' if sieve.get('completed', False) else 'PARTIAL'}")
