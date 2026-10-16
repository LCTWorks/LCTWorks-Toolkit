using System.Collections.Generic;
using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Navigation;
using LCTWorks.Workshop.Models;
using LCTWorks.Workshop.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class HomePage : Page
{
    public HomePage()
    {
        Groups = BuildGroups(App.GetService<DocsService>()?.Items ?? []);
        InitializeComponent();
    }

    public string AppDisplayName { get; } = "AppDisplayName".GetTextLocalized();

    public string Description { get; } = "Home_Description".GetTextLocalized();

    /// <summary>
    /// Navigation items grouped by their header (Controls, Tools, Resources...), excluding Home itself.
    /// </summary>
    public IReadOnlyList<DocGroup> Groups { get; }

    private static List<DocGroup> BuildGroups(IEnumerable<DocItem> items)
    {
        var groups = new List<DocGroup>();
        string? title = null;
        var current = new List<DocItem>();

        void Flush()
        {
            if (title != null && current.Count > 0)
            {
                groups.Add(new DocGroup(title, current));
            }
            current = [];
        }

        foreach (var item in items)
        {
            if (item.IsHeader)
            {
                Flush();
                title = item.Title;
            }
            else if (title != null)
            {
                // Items before the first header (Home itself) aren't shown.
                current.Add(item);
            }
        }
        Flush();
        return groups;
    }

    private void DocItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string key } && !string.IsNullOrEmpty(key))
        {
            // Passing the key as the parameter lets NavigationViewHelper select the matching menu item.
            App.GetService<FrameNavigationService>()?.NavigateTo(key, key);
        }
    }
}
