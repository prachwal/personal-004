using PetEmulator.Core;
using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet.Devices;

/// <summary>Live status view for the independent PET cassette #2 transport.</summary>
public sealed class PetDatasette2Status(PetDatasette2 datasette) : IDeviceStatus
{
    public string Id => "datasette2";

    public string Icon => "📼";

    public string DisplayName => "Datasette #2";

    public string StatusText => datasette switch
    {
        { HasTape: false } => "No tape",
        { PlayPressed: false } => $"{datasette.TapeName ?? "(unnamed tape)"} - press play",
        { MotorOn: false } => $"{datasette.TapeName ?? "(unnamed tape)"} - play pressed, waiting for motor",
        _ => $"{datasette.TapeName ?? "(unnamed tape)"} - playing",
    };
}
