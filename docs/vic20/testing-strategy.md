# VIC-20 testing strategy

Mirrors `docs/pet/disk-testing-strategy.md`'s 4-layer structure (see `docs/vic20/migration-plan.md`
step 12). All layers pass as of this writing.

## Layer 0 — chip unit tests

Pure logic, no CPU, no bus. `tests/PetEmulator.Chips.Tests/MOS6560Tests.cs`
(register decode: columns/rows/screen-addr A13-inversion/raster tick+wrap/reset),
`MOS2114Tests.cs` (nibble masking, per-offset independence, reset).

## Layer 1 — register-level bus integration

`Vic20MemoryBusTests.cs` - real ROM images loaded at their real addresses (proven via the RESET
vector actually pointing into KERNAL), RAM read/write at both regions, ROM writes silently
dropped, unmapped ($A000-$BFFF cartridge window) reads as open bus, VIC/color-RAM registers
routed through to their chips, `Observer` fires for every real access - all without booting the
CPU through any real code.

## Layer 2 — real KERNAL/BASIC end-to-end

`Vic20BootTests.cs` - `RunUntil` (from `PetEmulator.Core.MachineExtensions` - the exact same
extension method PET tests use) waits for real, non-space characters in VIC screen RAM (BASIC's
own startup banner), then confirms the KERNAL configured real VIC columns/rows during boot.
`Vic20KeyboardBootTests.cs` - types `PRINT5` through the real 8x8 keyboard matrix
(`Vic20TextTyper`/`Vic20HostKeyMap`) once booted, confirms the digit actually appears on screen -
proof the keyboard, not just the CPU, is real and working.

`Vic20MachineTests.cs` additionally proves the debug-tooling promise from
`docs/pet/debug-tools.md`: `MachineDebugger` (`PetEmulator.Debugger`) works against
`Vic20Machine` with **zero VIC-20-specific code** - it was built purely against
`IMachine`/`IProcessor`/`IMemoryBus`, and this is the test that cashes that in.

## Layer 3 — scripted smoke test

`scripts/vic20-boot.dbg` + `scripts/test-vic20-boot.sh` (mirrors
`scripts/pet-tape-loading.dbg`/`test-pet-tape-loading.sh`) - mounts real ROMs through the
`vic20-debug` CLI subcommand (`Vic20DebuggerSession`), runs a bounded trace, greps the log.

## What v1 does NOT cover

Per `docs/vic20/migration-plan.md`'s explicit scope cuts: cartridge loading, tape/disk, audio
(`MOS6560`'s oscillator registers exist as raw bytes but nothing decodes them), PAL timing
(NTSC-only constants), expansion-preset banking (unexpanded memory map only), and Desktop
rendering (no `Vic20ScreenControl` yet - the engine boots and runs correctly, nothing renders it
to a window). None of these block "does the VIC-20 actually boot and run real BASIC" - they're the
next slice, not gaps in what's tested here.
