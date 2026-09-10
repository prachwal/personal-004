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

    public string StatusText => datasette switch
    {
        { HasTape: false } => "No tape",
        { PlayPressed: false } => $"{datasette.TapeName ?? "(unnamed tape)"} - press play",
        { MotorOn: false } => $"{datasette.TapeName ?? "(unnamed tape)"} - play pressed, waiting for motor",
        _ => $"{datasette.TapeName ?? "(unnamed tape)"} - playing",
    };
}
