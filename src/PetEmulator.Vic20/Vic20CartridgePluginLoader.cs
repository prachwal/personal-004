using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;
using System.Runtime.Loader;
using PetEmulator.Vic20.Cartridge.Abstractions;

namespace PetEmulator.Vic20;

/// <summary>Loads cartridge plugin entry points from external DLLs.</summary>
public static class Vic20CartridgePluginLoader
{
    public static IReadOnlyList<IVic20CartridgePlugin> LoadDirectory(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        if (!Directory.Exists(directory))
            return [];

        return Directory.EnumerateFiles(directory, "*.dll", SearchOption.TopDirectoryOnly)
            .SelectMany(LoadAssembly)
            .ToArray();
    }

    public static IVic20CartridgePlugin Load(string path, ILogger? logger = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        var log = logger ?? NullLogger.Instance;
        log.LogInformation("Loading cartridge plugin '{Path}'.", path);
        try
        {
            var plugins = LoadAssembly(Path.GetFullPath(path)).ToArray();
            var plugin = plugins.Length switch
            {
                1 => plugins[0],
                0 => throw new InvalidDataException($"Cartridge plugin '{path}' exports no plugin."),
                _ => throw new InvalidDataException($"Cartridge plugin '{path}' exports more than one plugin."),
            };
            log.LogInformation("Cartridge plugin loaded '{Path}': id='{Id}'.", path, plugin.Descriptor.Id);
            return plugin;
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Loading cartridge plugin '{Path}' failed.", path);
            throw;
        }
    }

    private static IEnumerable<IVic20CartridgePlugin> LoadAssembly(string path)
    {
        Assembly assembly;
        try
        {
            assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
        }
        catch (Exception exception) when (exception is BadImageFormatException or FileLoadException)
        {
            throw new InvalidDataException($"Cannot load cartridge plugin '{path}'.", exception);
        }

        Type[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            var detail = string.Join(" ", exception.LoaderExceptions.OfType<Exception>()
                .Select(error => error.Message));
            throw new InvalidDataException($"Cannot inspect cartridge plugin '{path}': {detail}", exception);
        }

        foreach (var type in types
                     .Where(type => !type.IsAbstract && typeof(IVic20CartridgePlugin).IsAssignableFrom(type)))
        {
            if (Activator.CreateInstance(type) is IVic20CartridgePlugin plugin)
                yield return plugin;
        }
    }
}
