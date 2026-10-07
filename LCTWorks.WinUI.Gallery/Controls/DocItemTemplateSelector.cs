using LCTWorks.Workshop.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.Workshop.Controls;

/// <summary>
/// Picks a navigation template for a <see cref="DocItem"/>: a separator when the item has no
/// <see cref="DocItem.NavigationKey"/>, otherwise a regular navigation item.
/// </summary>
public sealed partial class DocItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? ItemTemplate { get; set; }

    public DataTemplate? SeparatorTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item)
        => SelectTemplateCore(item, null!);

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        if (item is DocItem doc && string.IsNullOrWhiteSpace(doc.NavigationKey))
        {
            return SeparatorTemplate ?? ItemTemplate;
        }

        return ItemTemplate;
    }
}
