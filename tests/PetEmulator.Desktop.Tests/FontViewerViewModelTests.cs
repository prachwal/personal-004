using FluentAssertions;
using NUnit.Framework;
using PetEmulator.Desktop.ViewModels;
using PetEmulator.Pet.Fonts;

namespace PetEmulator.Desktop.Tests;

public sealed class FontViewerViewModelTests
{
    [Test]
    public void AggregatesFontsFromEveryRegisteredProvider()
    {
        // Proves the view model is provider-agnostic: it doesn't know or care what StubProvider
        // is, it just calls DiscoverFonts on whatever it's given - a new machine module plugs in
        // without ever touching this class.
        var viewModel = new FontViewerViewModel("unused-roms-root", [new StubProvider("A"), new StubProvider("B")]);

        viewModel.Fonts.Select(f => f.Name).Should().BeEquivalentTo("A", "B");
        viewModel.SelectedFont.Should().Be(viewModel.Fonts[0]);
    }

    [Test]
    public void NoProvidersMeansNoFontsInsteadOfThrowing()
    {
        var viewModel = new FontViewerViewModel("unused-roms-root", []);

        viewModel.Fonts.Should().BeEmpty();
        viewModel.SelectedFont.Should().BeNull();
        viewModel.StatusFields.Should().ContainSingle().Which.Value.Should().Be("No character ROMs found");
    }

    [Test]
    public void DefaultConstructorStillFindsTheRealPetAndVic20Fonts()
    {
        var viewModel = new FontViewerViewModel(FindRomsRoot());

        viewModel.Fonts.Should().NotBeEmpty();
        viewModel.Fonts.Should().Contain(f => f.Name == "VIC-20 character ROM");
        viewModel.Fonts.Should().Contain(f => f.Name == "Kaypro II character ROM");
    }

    private sealed class StubProvider(string name) : ICharacterRomProvider
    {
        public IEnumerable<FontSource> DiscoverFonts(string romsRoot)
        {
            yield return new FontSource(name, new BitmapFont(new byte[8], glyphWidth: 8, glyphHeight: 8));
        }
    }

    private static string FindRomsRoot()
    {
        for (var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "roms");
            if (Directory.Exists(candidate))
                return candidate;
        }
        throw new DirectoryNotFoundException("Could not locate the roms/ directory.");
    }
}
