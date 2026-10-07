using LCTWorks.WinUI.Extensions;
using LCTWorks.Workshop.Models;
using LCTWorks.Workshop.Items;
using LCTWorks.WinUI.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using LCTWorks.Workshop.Views.Items;

namespace LCTWorks.Workshop.Services;

public class DocsService
{
    private const string IconResKeySuffix = "Page";
    private static readonly Dictionary<string, Type?> _itemKeyToTypeMap;
    private readonly List<DocItem> _items = [];

    static DocsService()
    {
        _itemKeyToTypeMap = new Dictionary<string, Type?>
        {
            { typeof(HomePage).ToString(), typeof(HomePage) },
            { $"ControlsHeader", null },
            { typeof(AdaptiveImagePage).ToString(), typeof(AdaptiveImagePage) },
            { typeof(AdaptiveViewPage).ToString(), typeof(AdaptiveViewPage) },
            { typeof(ChipPage).ToString(), typeof(ChipPage) },
            { typeof(SampleCodePresenterPage).ToString(), typeof(SampleCodePresenterPage) },
            { typeof(SentryPage).ToString(), typeof(SentryPage) },
            { typeof(ThemedButtonPage).ToString(), typeof(ThemedButtonPage) },
            { $"ResourcesHeader", null },
            { typeof(TextResourcesPage).ToString(), typeof(TextResourcesPage) },
        };
    }

    public DocsService()
    {
        InitializeItems();
    }

    public List<DocItem> Items => _items;

    private static string GetResourceKey(string originalKey)
    {
        if (string.IsNullOrEmpty(originalKey))
        {
            return string.Empty;
        }
        var sections = originalKey.Split(".");
        if (sections.Length == 0)
        {
            return string.Empty;
        }
        var lastSection = sections.Last();
        var index = lastSection.LastIndexOf(IconResKeySuffix);
        if (index == -1)
        {
            return lastSection;
        }
        return lastSection[..index];
    }

    private void InitializeItems()
    {
        foreach (var item in _itemKeyToTypeMap)
        {
            var resKey = GetResourceKey(item.Key);
            var title = $"{resKey}_Title".GetTextLocalized();
            string description = string.Empty;
            string icon = string.Empty;
            string navigationKey = string.Empty;
            if (item.Value != null)
            {
                NavigationPageMap.Configure(item.Key, item.Value);
                description = $"{resKey}_Description".GetTextLocalized();
                icon = $"ms-appx:///Assets/Icons/{resKey}.svg";
                navigationKey = item.Key;
            }
            _items.Add(new DocItem(title, description, icon, navigationKey));
        }
    }
}