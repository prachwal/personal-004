namespace PetEmulator.Pet.Devices;

/// <summary>One dynamically-discoverable peripheral a GUI can show a status icon for. A running
/// <see cref="PetMachine"/> builds its <see cref="PetMachine.Devices"/> list from whatever is
/// actually attached (a datasette with/without a tape, each mounted IEEE-488 disk drive) rather
/// than a fixed enum - a GUI iterates the list instead of hard-coding one icon per device kind, so
/// a new device (a printer, a second disk drive, an RS-232 modem) shows up automatically the
/// moment something in <c>PetEmulator.Pet</c> starts reporting it here, with no GUI change
/// needed.</summary>
public interface IPetDeviceStatus
{
    /// <summary>Stable id for a GUI to key elements on (e.g. "datasette", "ieee488:8").</summary>
    string Id { get; }

    /// <summary>Single glyph/emoji a GUI shows as the device's icon.</summary>
    string Icon { get; }

    string DisplayName { get; }

    /// <summary>Current one-line status, e.g. "No tape" / "starwars.tap" / "game.d64".</summary>
    string StatusText { get; }
}
