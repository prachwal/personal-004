using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Kaypro;
using PetEmulator.Pet.Fonts;
using PetEmulator.Vic20;
using PetEmulator.Trs80;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class FontViewerViewModel : ObservableObject, IShellModule
{
    [ObservableProperty]
    private FontSource? _selectedFont;

    [ObservableProperty]
    private bool _reverse;

    public FontViewerViewModel(string romsRoot, IEnumerable<ICharacterRomProvider>? providers = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(romsRoot);
        var log = EmulatorLogging.CreateLogger("FontViewer");
        log.LogInformation("Discovering character ROMs in '{RomsRoot}'.", romsRoot);
        var sources = (providers ?? DefaultProviders(log))
            .SelectMany(provider => provider.DiscoverFonts(romsRoot))
            .ToArray();
        log.LogInformation("Discovered {Count} fonts: {Fonts}.", sources.Length, string.Join(", ", sources.Select(font => font.Name)));
        Fonts = new ObservableCollection<FontSource>(sources);
        SelectedFont = Fonts.FirstOrDefault();
    }

    /// <summary>Every machine module that ships a character ROM registers its provider here -
    /// the only place PetEmulator.Desktop needs to touch when a new one is added.</summary>
    private static IEnumerable<ICharacterRomProvider> DefaultProviders(ILogger log)
    {
        yield return new PetCharacterRomProvider(log);
        yield return new Vic20CharacterRomProvider(log);
        yield return new KayproCharacterRomProvider(log);
        yield return new Trs80CharacterRomProvider(log);
    }

    public string WindowTitle => "Font / Glyph Viewer";
    public IReadOnlyList<StatusField> StatusFields => [new("Font", SelectedFont?.Name ?? "No character ROMs found")];
    public ObservableCollection<FontSource> Fonts { get; }
    public ObservableCollection<GlyphCell> Glyphs { get; } = [];

    partial void OnSelectedFontChanged(FontSource? value)
    {
        RebuildGlyphs();
        OnPropertyChanged(nameof(StatusFields));
    }

    partial void OnReverseChanged(bool value) => RebuildGlyphs();

    [RelayCommand]
    private void ToggleReverse() => Reverse = !Reverse;

    private void RebuildGlyphs()
    {
        Glyphs.Clear();
        if (SelectedFont is null)
            return;

        for (var index = 0; index < 256; index++)
        {
            var rows = new string[SelectedFont.Font.GlyphHeight];
            for (var row = 0; row < rows.Length; row++)
            {
                var bits = SelectedFont.Font.GetGlyphRow((byte)index, row);
                if (Reverse) bits = (byte)~bits;
                rows[row] = string.Concat(Enumerable.Range(0, 8)
                    .Select(bit => (bits & (0x80 >> bit)) != 0 ? "##" : "  "));
            }
            Glyphs.Add(new GlyphCell(index, string.Join(Environment.NewLine, rows)));
        }
    }

    public void Dispose() { }
}

public sealed record GlyphCell(int Code, string Pattern);
