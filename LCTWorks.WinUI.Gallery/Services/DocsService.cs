using LCTWorks.WinUI.Extensions;
using LCTWorks.Workshop.Models;
using LCTWorks.Workshop.ViewModels.Items;
using LCTWorks.Workshop.Items;
using LCTWorks.WinUI.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using LCTWorks.Workshop.Views.Items;

namespace LCTWorks.Workshop.Services;

public class DocsService
{
    private const string IconResKeySuffix = "ViewModel";
    private static readonly Dictionary<string, Type> _itemKeyToTypeMap;
    private readonly List<DocItem> _items = [];

    static DocsService()
    {
        _itemKeyToTypeMap = new Dictionary<string, Type>
        {
            { typeof(HomeViewModel).ToString(), typeof(HomePage) },
            { typeof(AdaptiveImageViewModel).ToString(), typeof(AdaptiveImagePage) },
            //{ typeof(SampleCodePresenterViewModel).ToString(), typeof(SampleCodePresenterPage) },
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
            NavigationPageMap.Configure(item.Key, item.Value);
            var resKey = GetResourceKey(item.Key);
            var title = $"{resKey}_Title".GetTextLocalized();
            var description = $"{resKey}_Description".GetTextLocalized();
            var icon = $"ms-appx:///Assets/Icons/{resKey}.svg";
            _items.Add(new DocItem(title, description, icon, item.Key));
        }
    }
}