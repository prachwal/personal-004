namespace PetEmulator.Desktop.Services.DiskImages;

public sealed class DiskImageProviderRegistry : IDiskImageProviderRegistry
{
    private readonly IReadOnlyList<IDiskImageProvider> _providers;

    public DiskImageProviderRegistry(IEnumerable<IDiskImageProvider>? providers = null)
    {
        _providers = (providers ?? DefaultProviders()).ToArray();
    }

    /// <summary>Tries each provider's actual Open() in turn (not just CanOpen) and falls through on
    /// failure. Format is recognized by parsing content, not by file extension - CanOpen is a cheap
    /// pre-filter, but the real arbiter is whether Open() succeeds, since two formats can share a
    /// file size with no magic byte to fully disambiguate (e.g. a raw Kaypro CP/M image and a JV1
    /// TRS-80 image are both unheadered 204800-byte sector dumps).</summary>
    public DiskImageDocument Open(string path)
    {
        foreach (var provider in _providers)
        {
            if (!provider.CanOpen(path))
                continue;
            try
            {
                return provider.Open(path);
            }
            catch
            {
                // CanOpen's cheap check passed but the real parse didn't - try the next provider.
            }
        }
        throw new NotSupportedException($"No disk image provider recognized '{Path.GetFileName(path)}'.");
    }

    private static IEnumerable<IDiskImageProvider> DefaultProviders()
    {
        // Most-specific content signature first: Trs80/D64 fully validate before claiming a file;
        // Kaypro's raw .dsk has no magic byte at all (just a fixed size), so it goes last as the
        // fallback for whatever nothing more specific recognized.
        yield return new Trs80DiskImageProvider();
        yield return new D64DiskImageProvider();
        yield return new KayproDiskImageProvider();
    }
}
