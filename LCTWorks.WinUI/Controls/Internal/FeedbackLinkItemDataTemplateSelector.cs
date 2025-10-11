using LCTWorks.WinUI.Controls.ContentDialogs;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Controls.Internal;

internal partial class FeedbackLinkItemDataTemplateSelector : DataTemplateSelector
{
    public FeedbackLinkItemDataTemplateSelector()
    {
    }

    public DataTemplate? InteractiveTemplate { get; set; }

    public DataTemplate? NonInteractiveTemplate { get; set; }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is FeedbackLinkItem linkItem)
        {
            return linkItem.Command is not null ? InteractiveTemplate ?? base.SelectTemplateCore(item) : NonInteractiveTemplate ?? base.SelectTemplateCore(item);
        }
        return base.SelectTemplateCore(item);
    }
}