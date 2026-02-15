using HtmlAgilityPack;
using LCTWorks.Core.Extensions;

namespace LCTWorks.Web.Extensions;

public static class HtmlAgilityPackExtensions
{
    public static string GetAttributeValue(this HtmlNodeCollection collection, string attributeKeyName, string attributeValueName, params string[] propertyNames)
    {
        if (collection != null)
        {
            var singlePropertyName = propertyNames.Length == 1;
            foreach (var node in collection)
            {
                var property = node.GetAttributeValue(attributeKeyName, "").ToLowerInvariant();
                if (singlePropertyName && propertyNames[0] == property)
                {
                    return node.GetAttributeValue(attributeValueName, "");
                }
                else if (propertyNames.Any(x => x == property))
                {
                    return node.GetAttributeValue(attributeValueName, "");
                }
            }
        }
        return string.Empty;
    }

    public static string GetAttributeValue(this HtmlNodeCollection collection, IDictionary<string, string> conditionProperties, string returnPropertyName)
    {
        if (collection == null)
        {
            return string.Empty;
        }
        foreach (var node in collection)
        {
            if (conditionProperties.All(x => node.Attributes.Any(a => a.Name == x.Key && a.Value == x.Value)))
            {
                return node.GetAttributeValue(returnPropertyName, "");
            }
        }
        return string.Empty;
    }

    public static string? GetLinkHref(this HtmlNodeCollection? linkNodes, params string[] relValues)
    {
        if (linkNodes == null)
        {
            return null;
        }

        foreach (var rel in relValues)
        {
            var node = linkNodes.FirstOrDefault(n =>
                string.Equals(n.GetAttributeValue("rel", string.Empty), rel, StringComparison.OrdinalIgnoreCase));
            if (node != null)
            {
                return node.GetAttributeValue("href", string.Empty);
            }
        }
        return null;
    }

    /// <summary>
    /// Finds the meta attribute value by checking both "property" and "name" attributes.
    /// </summary>
    public static string GetMetaAttributeValue(this HtmlNodeCollection? collection, params string[] propertyNames)
    {
        if (collection == null)
        {
            return string.Empty;
        }
        var propertyValue = collection.GetAttributeValue("property", "content", propertyNames);
        if (string.IsNullOrWhiteSpace(propertyValue))
        {
            propertyValue = collection.GetAttributeValue("name", "content", propertyNames);
        }
        return propertyValue;
    }

    public static string? SelectSingleNodeAttribute(this HtmlDocument? doc, string xpath, string attributeName)
    {
        if (doc == null)
        {
            return null;
        }
        return doc.DocumentNode
            .SelectSingleNode(xpath)?
            .GetAttributeValue(attributeName, string.Empty)
            .NullIfEmpty();
    }
}