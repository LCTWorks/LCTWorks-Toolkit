using System.Collections.Generic;

namespace LCTWorks.Workshop.Models;

/// <summary>
/// A section of the Home page: a navigation header and the items under it.
/// Get-only, constructor-initialized so the XAML compiler doesn't need to activate or set it.
/// </summary>
public sealed class DocGroup(string title, IReadOnlyList<DocItem> items)
{
    public IReadOnlyList<DocItem> Items { get; } = items;

    public string Title { get; } = title;
}
