using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Pet.Tape;
using PetEmulator.Pet.Tests.Roms;

namespace PetEmulator.Pet.Tests.Tape;

/// <summary>
/// Proves <see cref="PetTapFile"/>/<see cref="PetTapePulseDecoder"/> against a real, downloaded
/// Commodore Datasette tape image (see <c>roms/pet/test-tapes/README.md</c> for provenance) -
/// not a synthetic byte array built in code. The header's platform byte is mislabeled C64 rather
/// than PET, but the pulse-cycle stream itself is proven below to be genuine PET/Commodore
/// cassette protocol: a long Short-pulse leader, then real <c>Long,Medium</c> byte-start markers
/// and Short/Medium bit pairs, decoded through the actual <see cref="PetTapePulseDecoder"/> -
/// not just a header signature check.
/// </summary>
public sealed class PetTapFileRealFixtureTests
{
    private static PetTapFile LoadFixture() =>
        PetTapFile.Parse(File.ReadAllBytes(Path.Combine(
            RomLocator.Directory("test-tapes", "tower-and-dragon-town.tap"), "tower-and-dragon-town.tap")));

    [Test]
    public void Parses_a_real_version1_tap_container()
    {
        var tap = LoadFixture();

        tap.Version.Should().Be(1);
        tap.PulseCycles.Should().HaveCount(549_507);
    }

    [Test]
    public void Starts_with_a_long_shortPulse_leader_before_any_real_data()
    {
        var tap = LoadFixture();
        var kinds = tap.PulseCycles.Select(PetTapeCassetteFormat.Classify).ToList();

        kinds[0].Should().Be(PetTapePulseKind.Short);
        var firstNonShort = kinds.FindIndex(k => k != PetTapePulseKind.Short);
        firstNonShort.Should().Be(27_136, "the real tape's leader is this many short sync pulses long");
        kinds[firstNonShort].Should().Be(PetTapePulseKind.Long, "a real byte starts Long,Medium");
        kinds[firstNonShort + 1].Should().Be(PetTapePulseKind.Medium);
    }

    [Test]
    public void DecodesRealBytesImmediatelyAfterTheLeader()
    {
        var tap = LoadFixture();
        var kinds = tap.PulseCycles.Select(PetTapeCassetteFormat.Classify).ToList();
        var leaderEnd = kinds.FindIndex(k => k != PetTapePulseKind.Short);

        // 20 pulses/byte (2 start-marker + 16 bit-pair + 2 parity); this bounded slice sits
        // entirely inside the first duplicated copy's data, before the next sync-countdown
        // block boundary PetTapePulseDecoder.DecodeBytes doesn't model on its own.
        var firstBlock = tap.PulseCycles.Skip(leaderEnd).Take(4_040).ToList();

        var decoded = PetTapePulseDecoder.DecodeBytes(firstBlock);

        decoded.Should().HaveCount(202, "real, protocol-correct Commodore tape data decodes cleanly through the real decoder");
    }
}
