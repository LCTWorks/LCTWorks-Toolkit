using LCTWorks.WinUI.Controls;
using LCTWorks.WinUI.Extensions;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class ChipPage : ObservablePage
{
    public ChipPage()
    {
        InitializeComponent();
    }

    public string Description { get; } = "Chip_Description".GetTextLocalized();

    public string Header { get; } = "Chip_Title".GetTextLocalized();
}