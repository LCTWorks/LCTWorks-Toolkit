using System.Collections.Generic;
using Windows.ApplicationModel.Resources;

namespace LCTWorks.WinUI.Extensions;

public static class ResourceExtensions
{
    private static readonly Dictionary<string, ResourceLoader> _map;
    private static readonly ResourceLoader _resourceLoader;
    private static readonly ResourceLoader _textPreviewResourceLoader = ResourceLoader.GetForViewIndependentUse("TextPreview");

    static ResourceExtensions()
    {
        _resourceLoader = ResourceLoader.GetForViewIndependentUse();
        _map = new Dictionary<string, ResourceLoader>
        {
            { nameof(_resourceLoader), _textPreviewResourceLoader }
        };
    }

    public static string DefaultValue
    {
        get; set;
    } = string.Empty;

    public static string GetTextLocalized(this string resourceKey) => _resourceLoader.GetString(resourceKey);

    public static string GetTextLocalized(this string resourceKey, string resourceFileName)
    {
        if (!_map.TryGetValue(resourceFileName, out ResourceLoader? value))
        {
            try
            {
                var resource = ResourceLoader.GetForViewIndependentUse(resourceFileName);
                if (resource == null)
                {
                    return DefaultValue;
                }

                value = resource;
                _map[resourceFileName] = value;
            }
            catch (System.Exception)
            {
                return DefaultValue;
            }
        }
        return value.GetString(resourceKey);
    }
}