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
`Dispose()` and `AudioOutput`. Every desktop machine exposes an audio sink; machines without a
modeled physical audio path use `NullAudioOutput`.

## Media capabilities

`MainWindowViewModel` routes media operations through capability interfaces, never through concrete
machine ViewModel types:

- `ITapeViewModel` - accepts a tape image. TRS-80 implements this minimal capability because its
  ordinary cassette player has no shared PLAY/STOP/EJECT transport UI.
- `IDatasetteViewModel` - `ITapeViewModel` plus PLAY/STOP/EJECT state and commands. PET, VIC-20,
  CPC464 and CPC6128 implement this full datasette UI; CPC machines expose the same controls over
  their cassette pulse player.
- `IDiskDriveViewModel` - shared load/status surface only. PET/VIC-20, Kaypro and TRS-80 retain
  their machine-specific disk image formats and FDC operations.
- `INewDiskViewModel` and `INewTapeViewModel` - opt-in creation commands for machines that support
  creating those media types.

## Implementations

- `PetMachineViewModel` - one instance per PET profile.
- `Vic20MachineViewModel` - wraps `Vic20Machine` + the new `Vic20RasterDisplay`
  (`PetEmulator.Vic20.Display`, ported from cpu-vibe-001's `Vic20Video.RenderChar` - fixed
  176x184 canvas since VIC-20 resolution is chip-register-driven and unknown until the KERNAL
  configures it during boot, unlike PET's profile-fixed geometry).
- `Cpc464MachineViewModel` and `Cpc6128MachineViewModel` - expose the CPC screen, cassette and
  audio capabilities; CPC6128 additionally exposes its I8272 disk drive capability.

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

## Contract coverage

`tests/PetEmulator.Core.Tests/MachineContractTests.cs` exercises `IMachine` against all five real
machine compositions (PET, VIC-20, Kaypro II, TRS-80 Model I and CPC464), while retaining the
small `TestMachine` fixture for CPU-agnostic lifecycle semantics.
