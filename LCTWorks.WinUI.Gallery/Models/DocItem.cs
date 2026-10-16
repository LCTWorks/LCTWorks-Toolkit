using LCTWorks.WinUI.Navigation;

namespace LCTWorks.Workshop.Models;

public record class DocItem(string Title, string Description, string IconPath, string NavigationKey) : INavigationItemComparable
{
    public DocItem() : this(string.Empty, string.Empty, string.Empty, string.Empty)
    {
    }

    public bool IsHeader => string.IsNullOrWhiteSpace(NavigationKey);

    /// <summary>
    /// Matches a navigation whose parameter is this item's <see cref="NavigationKey"/>, so
    /// <see cref="NavigationViewHelper"/> can select the matching menu item when navigating
    /// from code (e.g. the Home page) or going back.
    /// </summary>
    public bool IsMatch(object match)
    {
        if (IsHeader)
        {
            return false;
        }
        var parameter = match is FrameNavigationArgs args ? args.Parameter : match;
        return parameter is string key && key == NavigationKey;
    }
}
