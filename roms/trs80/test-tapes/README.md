# Test tapes

## swamp.cas

Real, captured TRS-80 Model I cassette dump, copied from sibling project
`personal-003/disks/swamp.cas`. Format: raw `.CAS` byte stream (no `CASSETTE1A` container/CRC16 -
matches what `Trs80CassettePlayer` currently supports, see `docs/trs80/model1-status.md`).

**License unknown.** `personal-003` deliberately keeps this file out of its own git history
(`disks/` is gitignored there, with an explicit "unknown license" comment on the test that uses
it - `tests/TRS80.Integration.Tests/RealMediaCompatibilityTests.cs`). Copied here anyway, on the
same standing as this repo's other binary ROM/font assets (`roms/trs80/model1-level2-v1.4.bin`,
`character_set_8s.bin`) - see `docs/trs80/model1-status.md`'s "Asset provenance" section: **a
licensing/copyright review is still needed before any of these are redistributed.**

Verified end-to-end against `Trs80CassettePlayer`: ticks through to `AtEndOfTape` without
exception and produces real EAR pulses (motor on, `Tick`, observe port bit `0x80`) - same check as
`personal-003`'s own `RealCasFileTicksThroughWithoutException` test.

## blank.cas

Genuinely empty (0 bytes) - a blank/erased tape, for testing "nothing in the drive" and
end-of-tape-immediately scenarios without needing any real program data.
