using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace LCTWorks.Common.WinUI.Navigation;

public class FrameNavigationArgs(bool clearNavigation, object? parameter)
{
    public bool ClearNavigation { get; } = clearNavigation;

    public object? Parameter { get; } = parameter;
}