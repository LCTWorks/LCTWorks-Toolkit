using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Gallery.Models;
using LCTWorks.WinUI.Gallery.ViewModels.Items;
using LCTWorks.WinUI.Gallery.Views.Items;
using LCTWorks.WinUI.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LCTWorks.WinUI.Gallery.Services;

public class DocsService
{
    private static readonly Dictionary<string, Type> _itemKeyToTypeMap;
    private readonly List<DocItem> _items = [];

    static DocsService()
    {
        _itemKeyToTypeMap = new Dictionary<string, Type>
        {
            { typeof(HomeViewModel).ToString(), typeof(HomePage) },
            { typeof(SoftImageViewModel).ToString(), typeof(SoftImagePage) },
        };
    }

    public DocsService()
    {
        InitializeItems();
    }

    public List<DocItem> Items => _items;

    private string GetResourceKey(string originalKey)
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
        return sections.Last();
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