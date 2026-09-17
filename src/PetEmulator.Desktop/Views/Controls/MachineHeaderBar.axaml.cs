using Avalonia.Controls;

namespace PetEmulator.Desktop.Views.Controls;

/// <summary>Shared top metric bar for every machine view ("NAME | WxH | CPU | extra" -
/// see <see cref="ViewModels.IMachineViewModel.MachineSummary"/>).</summary>
public partial class MachineHeaderBar : UserControl
{
    public MachineHeaderBar() => InitializeComponent();
}
