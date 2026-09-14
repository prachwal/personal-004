using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PetEmulator.Kaypro;
using PetEmulator.Pet.Fonts;
using PetEmulator.Vic20;

namespace PetEmulator.Desktop.ViewModels;

public sealed partial class FontViewerViewModel : ObservableObject, IShellModule
{
    [ObservableProperty]
    private FontSource? _selectedFont;

    [ObservableProperty]
    private bool _reverse;

    public FontViewerViewModel(string romsRoot, IEnumerable<ICharacterRomProvider>? providers = null)
    {
        var sources = (providers ?? DefaultProviders())
            .SelectMany(provider => provider.DiscoverFonts(romsRoot))
            .ToArray();
        Fonts = new ObservableCollection<FontSource>(sources);
        SelectedFont = Fonts.FirstOrDefault();
    }

    /// <summary>Every machine module that ships a character ROM registers its provider here -
    /// the only place PetEmulator.Desktop needs to touch when a new one is added.</summary>
    private static IEnumerable<ICharacterRomProvider> DefaultProviders()
    {
        yield return new PetCharacterRomProvider();
        yield return new Vic20CharacterRomProvider();
        yield return new KayproCharacterRomProvider();
    }

    public string WindowTitle => "Font / Glyph Viewer";
    public string StatusText => SelectedFont?.Name ?? "No character ROMs found";
    public ObservableCollection<FontSource> Fonts { get; }
    public ObservableCollection<GlyphCell> Glyphs { get; } = [];

    partial void OnSelectedFontChanged(FontSource? value)
    {
        RebuildGlyphs();
        OnPropertyChanged(nameof(StatusText));
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
