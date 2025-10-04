namespace LCTWorks.WinUI.Navigation;

public class FrameNavigationArgs(bool clearNavigation, object? parameter)
{
    public bool ClearNavigation { get; } = clearNavigation;

    public object? Parameter { get; } = parameter;
}