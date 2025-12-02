using LCTWorks.Core.Helpers;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace LCTWorks.Core.Extensions;

/// <summary>
/// High-performance, allocation-aware extensions for common collection scenarios.
/// </summary>
public static class CollectionExtensions
{
    /// <summary>
    /// Adds <paramref name="item"/> to <paramref name="collection"/> only if an equivalent item is not already present.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="collection">The target collection to add the item to.</param>
    /// <param name="item">The item to add if it is not already present.</param>
    /// <param name="equalityComparer">
    /// Optional comparer used to test equality when the target collection is not a set.
    /// If not provided, the collection's native semantics are used (e.g., <see cref="EqualityComparer{T}.Default"/> for lists, the set's comparer for sets).
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="collection"/> is read-only.</exception>
    public static void AddIgnoringDuplicates<T>(this ICollection<T> collection, T item, IEqualityComparer<T>? equalityComparer = null)
    {
        ThrowCheck.Null(collection, nameof(collection));
        ThrowCheck.ReadOnlyCollection(collection);

        if (equalityComparer is null)
        {
            if (collection is ISet<T> set)
            {
                set.Add(item);
                return;
            }

            if (collection is List<T> list)
            {
                var span = CollectionsMarshal.AsSpan(list);
                for (int i = 0; i < span.Length; i++)
                {
                    if (EqualityComparer<T>.Default.Equals(span[i], item))
                        return;
                }
                list.Add(item);
                return;
            }

            if (!collection.Contains(item))
            {
                collection.Add(item);
            }
            return;
        }

        if (collection is List<T> listWithComparer)
        {
            var span = CollectionsMarshal.AsSpan(listWithComparer);
            for (int i = 0; i < span.Length; i++)
            {
                if (equalityComparer.Equals(span[i], item))
                    return;
            }
            listWithComparer.Add(item);
            return;
        }

        if (collection is HashSet<T> hs && ReferenceEquals(equalityComparer, hs.Comparer))
        {
            hs.Add(item);
            return;
        }

        foreach (var existing in collection)
        {
            if (equalityComparer.Equals(existing, item))
                return;
        }
        collection.Add(item);
    }

    /// <summary>
    /// Adds all items from <paramref name="items"/> to <paramref name="collection"/>.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="collection">The target collection to add items to.</param>
    /// <param name="items">The items to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> or <paramref name="items"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="collection"/> is read-only.</exception>
    public static void AddRange<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        ThrowCheck.Null(collection, nameof(collection));
        ThrowCheck.Null(items, nameof(items));
        ThrowCheck.ReadOnlyCollection(collection);

        if (!items.Any())
        {
            return;
        }

        switch (collection)
        {
            case List<T> list:
                {
                    list.AddRange(items);
                    return;
                }
            case HashSet<T> hashSet:
                {
                    var count = items is ICollection<T> c ? c.Count
                               : items is IReadOnlyCollection<T> rc ? rc.Count
                               : (int?)null;
                    if (count.HasValue)
                    {
                        hashSet.EnsureCapacity(hashSet.Count + count.Value);
                    }
                    hashSet.UnionWith(items);
                    return;
                }
            case ISet<T> set:
                set.UnionWith(items);
                return;
        }

        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>
    /// Adds all items in <paramref name="items"/> to <paramref name="collection"/>.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="collection">The target collection to add items to.</param>
    /// <param name="items">Items to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> or <paramref name="items"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="collection"/> is read-only.</exception>
    public static void AddRange<T>(this ICollection<T> collection, params T[] items)
    {
        ThrowCheck.Null(collection, nameof(collection));
        ThrowCheck.Null(items, nameof(items));
        ThrowCheck.ReadOnlyCollection(collection);
        if (items.Length == 0) return;

        switch (collection)
        {
            case List<T> list:
                list.AddRange(items);
                return;

            case HashSet<T> hashSet:
                hashSet.EnsureCapacity(hashSet.Count + items.Length);
                hashSet.UnionWith(items);
                return;

            case ISet<T> set:
                set.UnionWith(items);
                return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            collection.Add(items[i]);
        }
    }

    /// <summary>
    /// Adds all items in <paramref name="items"/> to <paramref name="collection"/> without extra allocations.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="collection">The target collection to add items to.</param>
    /// <param name="items">Items to add, provided as a <see cref="ReadOnlySpan{T}"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="collection"/> is read-only.</exception>
    public static void AddRange<T>(this ICollection<T> collection, ReadOnlySpan<T> items)
    {
        ThrowCheck.Null(collection, nameof(collection));
        ThrowCheck.ReadOnlyCollection(collection);
        if (items.Length == 0)
        {
            return;
        }

        switch (collection)
        {
            case List<T> list:
                list.EnsureCapacity(list.Count + items.Length);
                for (int i = 0; i < items.Length; i++)
                {
                    list.Add(items[i]);
                }
                return;

            case HashSet<T> hashSet:
                hashSet.EnsureCapacity(hashSet.Count + items.Length);
                for (int i = 0; i < items.Length; i++)
                {
                    hashSet.Add(items[i]);
                }
                return;

            case ISet<T> set:
                for (int i = 0; i < items.Length; i++)
                {
                    set.Add(items[i]);
                }
                return;
        }

        for (int i = 0; i < items.Length; i++)
        {
            collection.Add(items[i]);
        }
    }

    /// <summary>
    /// Returns a read-only view over <paramref name="collection"/> without copying data.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="collection">The source collection.</param>
    /// <returns>
    /// An <see cref="IReadOnlyCollection{T}"/> wrapper. If <paramref name="collection"/> already implements
    /// <see cref="IReadOnlyCollection{T}"/>, it is returned as-is; if it implements <see cref="IList{T}"/>,
    /// a <see cref="ReadOnlyCollection{T}"/> is used; otherwise, a lightweight adapter is returned.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> is <c>null</c>.</exception>
    public static IReadOnlyCollection<T> AsReadOnly<T>(this ICollection<T> collection)
    {
        ThrowCheck.Null(collection, nameof(collection));

        if (collection is IReadOnlyCollection<T> ro)
        {
            return ro;
        }

        if (collection is IList<T> list)
        {
            return new ReadOnlyCollection<T>(list);
        }

        return new ReadOnlyCollectionAdapter<T>(collection);
    }

    /// <summary>
    /// Invokes <paramref name="action"/> for each item in <paramref name="collection"/>.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="collection">The source collection.</param>
    /// <param name="action">The action to execute per item.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> or <paramref name="action"/> is <c>null</c>.</exception>
    public static void ForEach<T>(this ICollection<T> collection, Action<T> action)
    {
        ThrowCheck.Null(collection, nameof(collection));
        ThrowCheck.Null(action, nameof(action));

        switch (collection)
        {
            case List<T> list:
                {
                    var span = CollectionsMarshal.AsSpan(list);
                    for (int i = 0; i < span.Length; i++)
                    {
                        action(span[i]);
                    }
                    return;
                }
            case T[] array:
                {
                    var span = array.AsSpan();
                    for (int i = 0; i < span.Length; i++)
                    {
                        action(span[i]);
                    }
                    return;
                }
            case IReadOnlyList<T> roList:
                {
                    for (int i = 0; i < roList.Count; i++)
                    {
                        action(roList[i]);
                    }
                    return;
                }
        }

        foreach (var item in collection)
        {
            action(item);
        }
    }

    /// <summary>
    /// Invokes <paramref name="action"/> for each item in <paramref name="collection"/>, passing through caller-supplied state.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <typeparam name="TState">State type.</typeparam>
    /// <param name="collection">The source collection.</param>
    /// <param name="state">A caller-supplied state value passed to each invocation.</param>
    /// <param name="action">The action to execute per item that accepts the state.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="collection"/> or <paramref name="action"/> is <c>null</c>.
    /// </exception>
    public static void ForEach<T, TState>(this ICollection<T> collection, TState state, Action<T, TState> action)
    {
        ThrowCheck.Null(collection, nameof(collection));
        ThrowCheck.Null(action, nameof(action));

        switch (collection)
        {
            case List<T> list:
                {
                    var span = CollectionsMarshal.AsSpan(list);
                    for (int i = 0; i < span.Length; i++)
                    {
                        action(span[i], state);
                    }
                    return;
                }
            case T[] array:
                {
                    var span = array.AsSpan();
                    for (int i = 0; i < span.Length; i++)
                    {
                        action(span[i], state);
                    }
                    return;
                }
            case IReadOnlyList<T> roList:
                {
                    for (int i = 0; i < roList.Count; i++)
                    {
                        action(roList[i], state);
                    }
                    return;
                }
        }

        foreach (var item in collection)
        {
            action(item, state);
        }
    }

    /// <summary>
    /// Replaces the first occurrence of <paramref name="oldItem"/> in <paramref name="list"/> with <paramref name="newItem"/>.
    /// Uses the provided <paramref name="equalityComparer"/> when supplied; otherwise, if <paramref name="oldItem"/> implements
    /// <see cref="IComparable{T}"/>, equality is determined by CompareTo(...) == 0; falling back to <see cref="EqualityComparer{T}.Default"/>.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="list">Target list to mutate.</param>
    /// <param name="oldItem">Item to search for.</param>
    /// <param name="newItem">Replacement item.</param>
    /// <param name="equalityComparer">Optional equality comparer.</param>
    /// <returns>
    /// True if <paramref name="oldItem"/> was found and replaced; otherwise, False.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="list"/> is null.</exception>
    public static bool Replace<T>(this List<T> list, T oldItem, T newItem, IEqualityComparer<T>? equalityComparer = null)
    {
        ThrowCheck.Null(list, nameof(list));

        if (list.Count == 0)
        {
            return false;
        }

        var span = CollectionsMarshal.AsSpan(list);

        if (equalityComparer is not null)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (equalityComparer.Equals(span[i], oldItem))
                {
                    list[i] = newItem;
                    return true;
                }
            }
            return false;
        }

        if (oldItem is IComparable<T> comparable)
        {
            for (int i = 0; i < span.Length; i++)
            {
                if (comparable.CompareTo(span[i]) == 0)
                {
                    list[i] = newItem;
                    return true;
                }
            }
            return false;
        }

        var def = EqualityComparer<T>.Default;
        for (int i = 0; i < span.Length; i++)
        {
            if (def.Equals(span[i], oldItem))
            {
                list[i] = newItem;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Replaces the entire content of <paramref name="collection"/> with <paramref name="items"/>.
    /// </summary>
    /// <typeparam name="T">Item type.</typeparam>
    /// <param name="collection">The target collection to mutate.</param>
    /// <param name="items">The new items.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="collection"/> or <paramref name="items"/> is <c>null</c>.</exception>
    /// <exception cref="NotSupportedException">Thrown when <paramref name="collection"/> is read-only.</exception>
    public static void ReplaceAll<T>(this ICollection<T> collection, IEnumerable<T> items)
    {
        ThrowCheck.Null(collection, nameof(collection));
        ThrowCheck.Null(items, nameof(items));
        ThrowCheck.ReadOnlyCollection(collection);

        // Self-assignment guard
        if (ReferenceEquals(collection, items))
        {
            return;
        }

        switch (collection)
        {
            case List<T> list:
                {
                    int? newCount = items is ICollection<T> c ? c.Count
                                  : items is IReadOnlyCollection<T> rc ? rc.Count
                                  : null;
                    if (newCount.HasValue)
                    {
                        list.EnsureCapacity(newCount.Value);
                    }
                    int i = 0;
                    foreach (var item in items)
                    {
                        if (i < list.Count)
                        {
                            list[i] = item;
                        }
                        else
                        {
                            list.Add(item);
                        }
                        i++;
                    }

                    if (i < list.Count)
                    {
                        list.RemoveRange(i, list.Count - i);
                    }
                    return;
                }

            case HashSet<T> hashSet:
                {
                    hashSet.Clear();
                    if (items is ICollection<T> c)
                    {
                        hashSet.EnsureCapacity(c.Count);
                    }
                    hashSet.UnionWith(items);
                    return;
                }

            case ISet<T> set:
                {
                    set.Clear();
                    set.UnionWith(items);
                    return;
                }
        }

        // Fallback for arbitrary ICollection<T>
        collection.Clear();
        foreach (var item in items)
        {
            collection.Add(item);
        }
    }

    /// <summary>
    /// Builds a dictionary from <paramref name="source"/>, keeping the first item for each key and ignoring later duplicates.
    /// </summary>
    /// <typeparam name="TSource">The type of the items in the source sequence.</typeparam>
    /// <typeparam name="TKey">The dictionary key type.</typeparam>
    /// <param name="source">The input sequence.</param>
    /// <param name="keySelector">Function that selects the key for each item.</param>
    /// <returns>
    /// A <see cref="Dictionary{TKey, TValue}"/> mapping keys to the first encountered item producing that key.
    /// </returns>
    /// <remarks>
    /// - Unlike <see cref="Enumerable.ToDictionary{TKey, TValue}(IEnumerable{KeyValuePair{TKey, TValue}})"/>, duplicate keys are ignored instead of throwing.
    /// - <paramref name="keySelector"/> must not produce null keys.
    /// </remarks>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="source"/> or <paramref name="keySelector"/> is <c>null</c>.
    /// </exception>
    public static Dictionary<TKey, TSource> ToDictionaryIgnoreDuplicateKeys<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector) where TKey : notnull
    {
        ThrowCheck.Null(source, nameof(source));
        ThrowCheck.Null(keySelector, nameof(keySelector));

        var dict = new Dictionary<TKey, TSource>();
        foreach (var item in source)
        {
            var key = keySelector(item);
            dict.TryAdd(key, item);
        }
        return dict;
    }

    private sealed class ReadOnlyCollectionAdapter<TItem>(ICollection<TItem> inner) : IReadOnlyCollection<TItem>
    {
        private readonly ICollection<TItem> _inner = inner ?? throw new ArgumentNullException(nameof(inner));

        public int Count => _inner.Count;

        public IEnumerator<TItem> GetEnumerator() => _inner.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() =>
            ((System.Collections.IEnumerable)_inner).GetEnumerator();
    }
}