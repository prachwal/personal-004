using PetEmulator.Core;
using PetEmulator.Vic20.Tape;

namespace PetEmulator.Vic20.Devices;

/// <summary>Live <see cref="IDeviceStatus"/> view of a <see cref="Vic20Datasette"/> - mirrors
/// <c>PetEmulator.Pet.Devices.PetDatasetteStatus</c> exactly.</summary>
public sealed class Vic20DatasetteStatus(Vic20Datasette datasette) : IDeviceStatus
{
    public string Id => "datasette";

    public string Icon => "📼";

    public string DisplayName => "Datasette";

    // TapeName (not HasTape) decides "no tape": a freshly created blank tape (see
    // Vic20Datasette.NewBlankTape) has zero pulses but a real name, and should read as present -
    // ready to record, not "No tape".
    public string StatusText => datasette switch
    {
        { TapeName: null } => "No tape",
        { PlayPressed: false } => $"{datasette.TapeName} - press play",
        { MotorOn: false } => $"{datasette.TapeName} - play pressed, waiting for motor",
        _ => $"{datasette.TapeName} - playing",
    };
}
