using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Core;
using PetEmulator.Pet;
using PetEmulator.Pet.Keyboard;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests.Keyboard;

/// <summary>
/// Proves the punctuation entries in <see cref="Pet2001GraphicsKeyboardMap"/> (Quote/Comma/
/// Period/Slash/Semicolon/Minus - not in personal-001's own table for this keyboard variant, see
/// that class's doc comment) against a real, booted ROM: press the matrix cell alone, and the
/// screen must echo that exact character. Colon shares its cell's screen-code range with the
/// others but isn't independently asserted here since Semicolon/Colon share the row/col-adjacent
/// discovery pass - see the class under test for both.
/// </summary>
public sealed class Pet2001GraphicsKeyboardMapPunctuationTests
{
    private static readonly byte[] ReadyBytes = [0x12, 0x05, 0x01, 0x04, 0x19, 0x2E]; // "READY."

    [TestCase("Quote", '"')]
    [TestCase("Comma", ',')]
    [TestCase("Period", '.')]
    [TestCase("Slash", '/')]
    [TestCase("Semicolon", ';')]
    [TestCase("Minus", '-')]
    [CancelAfter(30_000)]
    public void PunctuationKey_EchoesExpectedCharacter(string hostKey, char expected)
    {
        var profile = PetProfileCatalog.Pet2001_8;
        var machine = CreateMachine(profile);
        var map = new Pet2001GraphicsKeyboardMap();

        machine.RunUntil(mem => ContainsReady(mem, profile), 1_000_000).Should().BeTrue();
        var before = SnapshotScreen(machine, profile);

        foreach (var action in map.Translate(hostKey, HostKeyEventKind.Press))
        {
            if (action.Pressed) machine.Keyboard.Press(action.Row, action.Column);
            else machine.Keyboard.Release(action.Row, action.Column);
        }
        machine.Run(5_500);
        foreach (var action in map.Translate(hostKey, HostKeyEventKind.Release))
        {
            if (action.Pressed) machine.Keyboard.Press(action.Row, action.Column);
            else machine.Keyboard.Release(action.Row, action.Column);
        }
        machine.Run(5_500);

        var after = SnapshotScreen(machine, profile);
        var echoed = FirstChanged(before, after);
        echoed.Should().Be((byte)expected, $"{hostKey} should echo '{expected}' (PET screen codes 0x20-0x3F match ASCII)");
    }

    private static byte? FirstChanged(byte[] before, byte[] after)
    {
        for (var i = 0; i < before.Length; i++)
            if (before[i] != after[i])
                return after[i];
        return null;
    }

    private static bool ContainsReady(IMemoryBus memory, PetProfile profile)
    {
        for (var start = profile.VideoRamStart; start + ReadyBytes.Length <= profile.VideoRamStart + profile.VideoRamLength; start++)
        {
            var match = true;
            for (var j = 0; j < ReadyBytes.Length; j++)
            {
                if (memory.Read((ushort)(start + j)) != ReadyBytes[j]) { match = false; break; }
            }
            if (match) return true;
        }
        return false;
    }

    private static byte[] SnapshotScreen(PetMachine machine, PetProfile profile)
    {
        var bytes = new byte[profile.Columns * profile.Rows];
        for (ushort i = 0; i < bytes.Length; i++)
            bytes[i] = machine.Memory.Read((ushort)(profile.VideoRamStart + i));
        return bytes;
    }

    private static PetMachine CreateMachine(PetProfile profile)
    {
        var profileDirectory = RomLocator.Directory(profile.RomDirectory, profile.RomManifest[0].Path);
        var romsRoot = Directory.GetParent(profileDirectory)!.FullName;
        return new PetMachine(profile, romsRoot);
    }
}
