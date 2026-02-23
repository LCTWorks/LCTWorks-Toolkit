using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Gallery.Models;
using LCTWorks.WinUI.Gallery.ViewModels.Controls;
using LCTWorks.WinUI.Gallery.Views.Controls;
using LCTWorks.WinUI.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LCTWorks.WinUI.Gallery.Services;

public class DocsService
{
    private static readonly Dictionary<string, Type> _itemKeyToTypeMap;
    private readonly List<DocItem> _items = [];

    static DocsService()
    {
        _itemKeyToTypeMap = new Dictionary<string, Type>
        {
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
            _items.Add(new DocItem(title, description, "", item.Key));
        }
    }
}