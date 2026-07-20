using LCTWorks.WinUI.Controls;
using LCTWorks.WinUI.Extensions;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class ThemedButtonPage : ObservablePage
{
    public ThemedButtonPage()
    {
        InitializeComponent();
    }

    public string Description { get; } = "ThemedButton_Description".GetTextLocalized();

    public string Header { get; } = "ThemedButton_Title".GetTextLocalized();
}