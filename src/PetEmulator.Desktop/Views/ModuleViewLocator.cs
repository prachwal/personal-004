using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Microsoft.Extensions.Logging;
using PetEmulator.Core.Logging;
using PetEmulator.Desktop.ViewModels;

namespace PetEmulator.Desktop.Views;

/// <summary>Single view-resolution contract for every shell module: <c>FooViewModel</c> in
/// <c>ViewModels</c> maps to <c>FooView</c> in this namespace, created fresh per switch. Replaces
/// the per-machine <c>DataTemplate DataType</c> list that used to live in
/// <c>MainWindow.axaml</c> (one entry per machine, growing with every new module) - adding a
/// machine is now "add View + ViewModel following the naming convention", touching nothing here.
/// A mismatch logs and renders a visible fallback instead of a blank window.</summary>
public sealed class ModuleViewLocator : IDataTemplate
{
    private static readonly ILogger Log = EmulatorLogging.CreateLogger("View");

    public bool Match(object? data) => data is IShellModule;

    public Control? Build(object? data)
    {
        if (data is null)
            return null;

        var viewModelName = data.GetType().Name;
        const string suffix = "ViewModel";
        if (!viewModelName.EndsWith(suffix, StringComparison.Ordinal))
        {
            Log.LogError("Module {Module} does not follow the *ViewModel naming convention; cannot locate its view.", viewModelName);
            return Fallback($"No view for '{viewModelName}' (naming convention: *ViewModel -> *View).");
        }

        var viewName = viewModelName[..^suffix.Length] + "View";
        var viewType = GetType().Assembly.GetType($"{GetType().Namespace}.{viewName}");
        if (viewType is null)
        {
            Log.LogError("View '{View}' for module {Module} not found in {Namespace}.", viewName, viewModelName, GetType().Namespace);
            return Fallback($"View '{viewName}' not found.");
        }

        try
        {
            var view = (Control)Activator.CreateInstance(viewType)!;
            Log.LogDebug("Resolved {Module} -> {View}.", viewModelName, viewName);
            return view;
        }
        catch (Exception ex)
        {
            Log.LogError(ex, "Failed to create view '{View}' for module {Module}.", viewName, viewModelName);
            return Fallback($"Failed to create '{viewName}': {ex.Message}");
        }
    }

    private static Control Fallback(string message) => new TextBlock
    {
        Text = message,
        Foreground = Avalonia.Media.Brushes.Red,
        Margin = new Avalonia.Thickness(16),
    };
}
