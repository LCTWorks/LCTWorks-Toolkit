using LCTWorks.Core.Helpers;

namespace LCTWorks.Core.Extensions;

public static class EnumerableExtensions
{
    public const string EmptyValueReplacementDefault = "-";

    /// <summary>
    /// Projects a sequence of (Key, Value) tuples into a sequence of KeyValuePair{TKey,TValue} with minimal allocations.
    /// </summary>
    /// <typeparam name="TKey">Key type.</typeparam>
    /// <typeparam name="TValue">Value type.</typeparam>
    /// <param name="items">The input sequence of tuples.</param>
    /// <returns>An enumerable of key/value pairs.</returns>
    public static IEnumerable<KeyValuePair<TKey, TValue>> ToKeyValuePairs<TKey, TValue>(this IEnumerable<(TKey Key, TValue Value)> items)
    {
        ThrowCheck.Null(items, nameof(items));
        if (items is null)
        {
            yield break;
        }

        if (items is List<(TKey Key, TValue Value)> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var (key, value) = list[i];
                yield return new KeyValuePair<TKey, TValue>(key, value);
            }
            yield break;
        }

        if (items is (TKey Key, TValue Value)[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                var (key, value) = array[i];
                yield return new KeyValuePair<TKey, TValue>(key, value);
            }
            yield break;
        }

        foreach (var (key, value) in items)
        {
            yield return new KeyValuePair<TKey, TValue>(key, value);
        }
    }

    /// <summary>
    /// Validates and projects (Key, Value) tuples into KeyValuePair&lt;string,string&gt;:
    /// - Replaces null/empty keys with the provided replacement or a new GUID per item.
    /// - Replaces null/empty values with the provided replacement or <see cref="EnumerableExtensions.EmptyValueReplacementDefault"/>.
    /// </summary>
    /// <param name="source">Source tuples.</param>
    /// <param name="emptyKeyReplacement">Replacement for null/empty keys. If null, a new GUID string is generated per empty key.</param>
    /// <param name="emptyValueReplacement">Replacement for null/empty values. Defaults to <see cref="EnumerableExtensions.EmptyValueReplacementDefault"/>.</param>
    /// <returns>Lazily-yielded validated key/value pairs.</returns>
    public static IEnumerable<KeyValuePair<string, string>> ValidateStringKeyValuePair(
        this IEnumerable<(string Key, string Value)> source,
        string? emptyKeyReplacement = null,
        string? emptyValueReplacement = null)
    {
        ThrowCheck.Null(source, nameof(source));

        var valueReplacement = emptyValueReplacement ?? EmptyValueReplacementDefault;

        if (source is List<(string Key, string Value)> list)
        {
            return EnumerateAndReplaceList(list, emptyKeyReplacement, valueReplacement);
        }

        if (source is (string Key, string Value)[] array)
        {
            return EnumerateAndReplaceArray(array, emptyKeyReplacement, valueReplacement);
        }

        return EnumerateAndReplace(source, emptyKeyReplacement, valueReplacement);
    }

    #region Internal

    private static IEnumerable<KeyValuePair<string, string>> EnumerateAndReplace(
        IEnumerable<(string Key, string Value)> src,
        string? keyReplacement,
        string valueReplacementLocal)
    {
        foreach (var (key, value) in src)
        {
            var validKey = string.IsNullOrEmpty(key) ? (keyReplacement ?? Guid.NewGuid().ToString()) : key;
            var validVal = string.IsNullOrEmpty(value) ? valueReplacementLocal : value;
            yield return new KeyValuePair<string, string>(validKey, validVal);
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> EnumerateAndReplaceArray(
        (string Key, string Value)[] src,
        string? keyReplacement,
        string valueReplacementLocal)
    {
        for (int i = 0; i < src.Length; i++)
        {
            var (key, value) = src[i];
            var validKey = string.IsNullOrEmpty(key) ? (keyReplacement ?? Guid.NewGuid().ToString()) : key;
            var validVal = string.IsNullOrEmpty(value) ? valueReplacementLocal : value;
            yield return new KeyValuePair<string, string>(validKey, validVal);
        }
    }

    private static IEnumerable<KeyValuePair<string, string>> EnumerateAndReplaceList(
        List<(string Key, string Value)> src,
        string? keyReplacement,
        string valueReplacementLocal)
    {
        for (int i = 0; i < src.Count; i++)
        {
            var (key, value) = src[i];
            var validKey = string.IsNullOrEmpty(key) ? (keyReplacement ?? Guid.NewGuid().ToString()) : key;
            var validVal = string.IsNullOrEmpty(value) ? valueReplacementLocal : value;
            yield return new KeyValuePair<string, string>(validKey, validVal);
        }
    }

    #endregion Internal
}