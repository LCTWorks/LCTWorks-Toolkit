using LCTWorks.WinUI.Controls.ContentDialogs;
using LCTWorks.WinUI.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Controls.Internal;

internal partial class ItemsContentDialogItemDataTemplateSelector : DataTemplateSelector
{
    public ItemsContentDialogItemDataTemplateSelector()
    {
    }

    public DataTemplate? InteractiveTemplate { get; set; }

    public DataTemplate? NonInteractiveTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is ItemsContentDialogItem linkItem)
        {
            return linkItem.Command is not null ? InteractiveTemplate ?? base.SelectTemplateCore(item) : NonInteractiveTemplate ?? base.SelectTemplateCore(item);
        }
        return base.SelectTemplateCore(item);
    }
}