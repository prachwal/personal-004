using PetEmulator.Pet.Tape;

namespace PetEmulator.Pet.Devices;

/// <summary>Live <see cref="IPetDeviceStatus"/> view of a <see cref="PetDatasette"/> - every
/// property re-reads the datasette on each access, so a GUI polling <see cref="PetMachine.Devices"/>
/// each frame always sees the current tape name/motor state with no separate update wiring.</summary>
public sealed class PetDatasetteStatus(PetDatasette datasette) : IPetDeviceStatus
{
    public string Id => "datasette";

    public string Icon => "📼";

    public string DisplayName => "Datasette";

    public string StatusText => datasette.HasTape
        ? $"{datasette.TapeName ?? "(unnamed tape)"}{(datasette.MotorOn ? " - playing" : "")}"
        : "No tape";
}
