using Microsoft.UI.Xaml;
using System;

namespace LCTWorks.WinUI.Controls;

public class AdaptiveViewItemClickedEventArgs(object clickedItem, UIElement container, int index) : EventArgs
{
    public object ClickedItem { get; } = clickedItem;

    public UIElement Container { get; } = container;

    public int Index { get; } = index;
}