# TRS-80 Model I — status

## Zrealizowane

- Model I memory map, Z80 composition, keyboard matrix, printer, cassette port, timed raster display (including semigraphics) and JV1/DMK-to-FD1793 adapters.
- Level II v1.4 ROM and MCM6670/6673 character-generator dump are available under `roms/trs80/`.
- CLI verb `trs80-debug` and Desktop module `TRS-80 Model I` are registered.
- Existing JV1/DMK/NewDOS/LSDOS parser tests remain green; new Model I contract tests are NUnit + FluentAssertions.

## Ograniczenia

- DMK mounting is read-only and intended for compatible LS-DOS/Model-I images; JV1 remains writable.
- The cassette implementation models the raw `.CAS` byte stream and port timing; the `CASSETTE1A` container and CRC16 are not implemented.
- **Fixed a real bug in `Trs80CassettePlayer.TryGetRecordedCas`** (the SAVE-side decoder, previously
  untested and unused - `Trs80Machine` has no real ROM-driven SAVE capture yet, unlike VIC-20's):
  it counted every 0->1 pulse edge as a separate bit, but a "1" bit's own waveform
  (`Trs80CassetteEncoding.OneBit`) has *two* rising edges per cell, not one, so every "1" bit
  recorded as two bits and any pattern with a "1" bit desynced the whole byte alignment. Fixed by
  grouping edges: a short gap to the next edge means both belong to one "1" bit, a long gap means a
  lone "0" bit. Caught and proven fixed by
  `tests/PetEmulator.Trs80.Tests/Trs80CassetteSaveLoadRoundTripTests.cs` - a full memory-write ->
  bit-banged SAVE -> `Reset()` -> bit-banged LOAD -> memory-read round trip through the real
  cassette port (drives `Trs80Machine.Bus.WritePort/ReadPort/Tick` directly, since real-ROM
  CSAVE/CLOAD isn't verified yet - see below).
- Real-ROM READY/CLOAD requires follow-up hardware-validation work - not yet attempted.
- `FD1791`/`FD1793` (`lib/PetEmulator.Chips/`, shared with Kaypro) now implements STEP/STEP-IN/
  STEP-OUT, side select, READ ADDRESS with a real CRC-16, and READ TRACK/WRITE TRACK (0xE0/0xF0) -
  synthesized FM/MFM track layout since `IFD1791DiskImage` carries no raw track bytes, verified via
  a WRITE TRACK -> READ TRACK -> normal sector read round trip. Still missing: CRC verification on
  ordinary READ SECTOR (the interface has nowhere to source a "real" on-disk CRC to check against
  for JV1; would need an interface extension to matter for DMK).
- The shared debugger now formats the register set exposed by each CPU, including Z80 sessions.
- `Trs80DebuggerSession`'s `disk`/`tape` CLI commands now hot-swap into the running machine
  (`Trs80Machine.InsertDisk`/`LoadTape`) instead of rebuilding it from scratch - matches
  `Trs80MachineViewModel`'s Desktop fix and no longer loses in-progress CPU/RAM state mid-script.

## Asset provenance

- Model I Level II v1.4: copied from sibling `personal-003/roms/model1/v1.4/MDL1REV4.bin`.
- Character generator: copied from sibling `personal-003/roms/chargen/character_set_8s.bin`.
- Test tapes (`roms/trs80/test-tapes/`): `swamp.cas` copied from sibling
  `personal-003/disks/swamp.cas` (real captured tape; `personal-003` itself keeps it out of git,
  "unknown license"); `blank.cas` is a genuinely empty synthetic tape. See that directory's
  `README.md`.
- Binary assets require a licensing/copyright review before redistribution.
