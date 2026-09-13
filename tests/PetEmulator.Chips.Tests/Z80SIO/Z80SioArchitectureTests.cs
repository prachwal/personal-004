using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Chips;

namespace PetEmulator.Chips.Tests;

public sealed class Z80SioArchitectureTests
{
    [Test]
    public void ChipAssemblyDoesNotDependOnMachineOrHostIoLayers()
    {
        var references = typeof(Z80Sio).Assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .ToArray();
        references.Should().NotContain("PetEmulator.Kaypro");
        references.Should().NotContain("PetEmulator.Desktop");

        typeof(Z80Sio).GetMethods()
            .Select(method => method.Name)
            .Should().NotContain(name => name.Contains("Keyboard", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public void ChipSupportsASecondMachinePortMapWithoutKayproRules()
    {
        var sio = new Z80Sio(basePort: 0x40);

        sio.Ports.Should().Equal(0x40, 0x41, 0x42, 0x43);
        sio.ReadPort(0x44).Should().Be(sio.Profile.InvalidReadValue);
    }
}
