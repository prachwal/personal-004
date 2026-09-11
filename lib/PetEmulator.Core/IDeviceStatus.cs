namespace PetEmulator.Core;

/// <summary>One dynamically-discoverable peripheral a GUI can show a status icon for. A running
/// <see cref="IMachine"/> builds its device list from whatever is actually attached rather than a
/// fixed enum - a GUI iterates the list instead of hard-coding one icon per device kind, so a new
/// device shows up automatically the moment something reports it here, with no GUI change needed.
///
/// Moved here from PetEmulator.Pet.Devices.IPetDeviceStatus once a second machine (VIC-20)
/// needed a shared per-machine device-status contract for the same Desktop status-bar view - see
/// docs/vic20-desktop-plan.md.</summary>
public interface IDeviceStatus
{
    /// <summary>Stable id for a GUI to key elements on (e.g. "datasette", "ieee488:8").</summary>
    string Id { get; }

    /// <summary>Single glyph/emoji a GUI shows as the device's icon.</summary>
    string Icon { get; }

    string DisplayName { get; }

    /// <summary>Current one-line status, e.g. "No tape" / "starwars.tap" / "game.d64".</summary>
    string StatusText { get; }
}
