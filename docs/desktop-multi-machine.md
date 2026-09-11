# Desktop: multi-machine shell

The "_Machine" menu switches between machine *kinds* (PET profiles and VIC-20), not just PET
profiles - each entry constructs a fresh `IMachineViewModel` and the View swaps wholesale via
Avalonia `DataTemplate` matching on the new instance's concrete type ("podmiana komponentu MVVM",
not an `if`/`else` in code-behind).

## Contract: `IMachineViewModel`

`src/PetEmulator.Desktop/IMachineViewModel.cs`. Three regions per the design brief:

1. **Screen** - `FrameBuffer`/`PixelWidth`/`PixelHeight`/`PixelAspect`, rendered by
   `PetScreenControl` (the name predates VIC-20; it's just an ARGB8888 blitter, already
   machine-agnostic - not renamed to keep the diff focused).
2. **Device icon bar** - `Devices` (`IReadOnlyList<PetEmulator.Core.IDeviceStatus>`, moved from
   `PetEmulator.Pet.Devices.IPetDeviceStatus` once VIC-20 needed the same shared shape - always
   empty for VIC-20 v1, no devices modeled yet).
3. **`Extra`** - reserved, deliberately unused extension point (`object?`, always `null` today).
   The user picked "empty slot for the future" over a concrete use during design - contract has
   room, nothing renders it yet.

Plus `WindowTitle`/`StatusText`/`Tick()`/`HandleKey()`/`Reset()`/`FrameReady`/`GeometryChanged`/
`Dispose()`.

## Implementations

- `PetMachineViewModel` - one instance per PET profile (was the old, sole `MainWindowViewModel`).
- `Vic20MachineViewModel` - new; wraps `Vic20Machine` + the new `Vic20RasterDisplay`
  (`PetEmulator.Vic20.Display`, ported from cpu-vibe-001's `Vic20Video.RenderChar` - fixed
  176x184 canvas since VIC-20 resolution is chip-register-driven and unknown until the KERNAL
  configures it during boot, unlike PET's profile-fixed geometry).

Switching machine (`MainWindowViewModel.SwitchMachineCommand`) disposes the old instance and
replaces `CurrentMachine` wholesale - never mutates an existing instance in place.

## Views

`PetMachineView.axaml`/`Vic20MachineView.axaml` - each a composite screen+device-bar+status
UserControl, matched by `DataTemplate DataType` in `MainWindow.axaml`. Both re-wire
`FrameReady`/`GeometryChanged` on `DataContextChanged`, not just once in the constructor - Avalonia
can reuse the same View instance across a same-type `CurrentMachine` swap (e.g. one PET profile to
another), so imperative event subscriptions must track the *current* DataContext, not whatever it
was at construction.

`MainWindow.axaml` is now a thin shell: menu + one `ContentControl` bound to `CurrentMachine`.

## What's out of scope

Tape/disk menu actions are PET-only (`MainWindowViewModel.LoadTape`/`LoadDisk` no-op when
`CurrentMachine` isn't a `PetMachineViewModel`) - VIC-20 has no tape/disk device in v1. No
automated Desktop/UI test project exists (none did before this change either) - verified via a
clean build and a crash-free headless launch (`xvfb-run`), plus the pre-existing thorough
`PetEmulator.Vic20.Tests`/`PetEmulator.Pet.Tests` coverage of everything each ViewModel wraps.
