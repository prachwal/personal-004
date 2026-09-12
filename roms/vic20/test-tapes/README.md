# Test tapes

## hello-vic.tap

Synthetic (not a real captured tape - this repo has no legally redistributable real VIC-20 .tap
fixture, unlike `roms/pet/test-tapes/tower-and-dragon-town.tap`), built with
`PetTapeCassetteFormat`'s encoding and verified end-to-end against the **real** VIC-20 KERNAL:
a header block (type `NonRelocatableProgram`, filename `HELLOVIC`, load address `$1000`) followed
by 3 sacrificial lead-in bytes and the payload `HELLO VIC` (ASCII).

The 3 lead-in bytes work around a real, observed KERNAL quirk: the tape-read routine's adaptive
pulse-timing calibration (see `docs/vic20/tape.md`) needs a byte or two to settle right after a
fresh sync-to-data-block transition, occasionally corrupting the first 1-2 content bytes of a
block. Real BASIC SAVEs are naturally immune (their own first bytes are link-pointer/line-number
filler, not meaningful content) - a hand-built payload isn't, hence the padding. Confirmed stable
across 3 repeated boot+LOAD attempts before being committed here (see
`Vic20MachineTapeTests.LoadDecodesARealTapeFileByteForByte`).

Verified via `PetTapFile.Parse` round-tripping the pulse-cycle stream exactly (encode → write
VICE-style .tap bytes → parse back → identical pulses).
