using System.Collections;

namespace LCTWorks.Core.Collections;

/// <summary>
/// Provides a tolerant wrapper around an arbitrary instance that exposes collection semantics
/// through <see cref="IList{Object}"/>, <see cref="ICollection{Object}"/>, <see cref="IList"/>,
/// and <see cref="ICollection"/> when the underlying object implements any of those interfaces.
/// </summary>
/// <remarks>
/// - Construct with any object. If it does not implement a supported collection interface,
///   <see cref="IsValidCollection"/> is false and all members become no-ops that return default values
///   (e.g., <see cref="Count"/> = 0, indexer returns null, mutators do nothing).
/// - When valid, the wrapper prefers generic interfaces over non-generic, and list semantics over
///   plain collections where available.
/// - Enumeration yields items as <see cref="object"/>; the indexer is only active when the backing type
///   is a list. Out-of-range accesses return default values rather than throwing.
/// - Thread-safety mirrors the underlying <see cref="ICollection.SyncRoot"/> when present; otherwise
///   a private sync root is used.
/// - Mutating operations (<see cref="Add(object)"/>, <see cref="Insert(int, object?)"/>,
///   <see cref="Remove(object)"/>, <see cref="Clear()"/>, <see cref="RemoveAt(int)"/>) are no-ops
///   when the collection is invalid or read-only.
/// </remarks>
/// <example>
/// var wrapper = new CompatibleCollection(possibleCollection);
/// if (wrapper.IsValidCollection)
/// {
///     foreach (var item in wrapper) { /* ... */ }
/// }
/// </example>
public partial class TolerantCollection : ICollection<object>, ICollection, IList<object>, IList
{
    private readonly ICollection? _collection;
    private readonly ICollection<object>? _collectionOfObject;
    private readonly IEnumerable? _enumerable;
    private readonly IEnumerable<object>? _enumerableOfObject;
    private readonly object? _innerObject;
    private readonly IList? _list;
    private readonly IList<object>? _listOfObject;
    private readonly object _syncRoot = new();

    public TolerantCollection(object? obj)
    {
        _innerObject = obj;
        _listOfObject = obj as IList<object>;
        _list = obj as IList;

        _collectionOfObject = obj as ICollection<object>;
        _collection = obj as ICollection;

        _enumerableOfObject = obj as IEnumerable<object>;
        _enumerable = obj as IEnumerable;

        IsValidCollection =
            _listOfObject != null ||
            _list != null ||
            _collectionOfObject != null ||
            _collection != null;
    }

    public int Count
    {
        get
        {
            if (!IsValidCollection)
            {
                return 0;
            }

            if (_collectionOfObject != null)
            {
                return _collectionOfObject.Count;
            }

            if (_collection != null)
            {
                return _collection.Count;
            }

            if (_listOfObject != null)
            {
                return _listOfObject.Count;
            }

            if (_list != null)
            {
                return _list.Count;
            }

            return 0;
        }
    }

    public object? InnerObject => _innerObject;

    public bool IsFixedSize
    {
        get
        {
            if (!IsValidCollection)
            {
                return false;
            }

            if (_list != null)
            {
                return _list.IsFixedSize;
            }

            return false;
        }
    }

    public bool IsGeneric => _listOfObject != null || _collectionOfObject != null || _enumerableOfObject != null;

    public bool IsList => _listOfObject != null || _list != null;

    public bool IsReadOnly
    {
        get
        {
            if (!IsValidCollection)
            {
                return false;
            }

            if (_listOfObject != null)
            {
                return _listOfObject.IsReadOnly;
            }

            if (_list != null)
            {
                return _list.IsReadOnly;
            }

            if (_collectionOfObject != null)
            {
                return _collectionOfObject.IsReadOnly;
            }
            return false;
        }
    }

    public bool IsSynchronized
    {
        get
        {
            if (!IsValidCollection)
            {
                return false;
            }

            return _collection != null && _collection.IsSynchronized;
        }
    }

    public bool IsValidCollection
    {
        get; private set;
    }

    public object SyncRoot
    {
        get
        {
            if (!IsValidCollection)
            {
                return _syncRoot;
            }

            return _collection != null ? _collection.SyncRoot : _syncRoot;
        }
    }

    object IList<object>.this[int index]
    {
        get => this[index] ?? new object();
        set => this[index] = value;
    }

    public object? this[int index]
    {
        get
        {
            if (!IsValidCollection)
            {
                return default;
            }

            if (_listOfObject != null)
            {
                if ((uint)index < (uint)_listOfObject.Count)
                {
                    return _listOfObject[index];
                }
                return default;
            }
            if (_list != null)
            {
                if ((uint)index < (uint)_list.Count)
                {
                    return _list[index];
                }
                return default;
            }

            return default;
        }
        set
        {
            if (!IsValidCollection)
            {
                return;
            }

            if (_listOfObject != null && !IsReadOnly)
            {
                if ((uint)index < (uint)_listOfObject.Count)
                {
                    _listOfObject[index] = value!;
                }
                return;
            }

            if (_list != null && !IsReadOnly)
            {
                if ((uint)index < (uint)_list.Count)
                {
                    _list[index] = value;
                }
            }
        }
    }

    public void Add(object item)
    {
        if (!IsValidCollection || IsReadOnly)
        {
            return;
        }

        if (_listOfObject != null)
        {
            _listOfObject.Add(item);
            return;
        }

        if (_collectionOfObject != null)
        {
            _collectionOfObject.Add(item);
            return;
        }

        if (_list != null)
        {
            _list.Add(item);
            return;
        }
    }

    int IList.Add(object? value)
    {
        if (!IsValidCollection || IsReadOnly)
        {
            return -1;
        }

        if (_list != null)
        {
            return _list.Add(value);
        }

        if (_listOfObject != null)
        {
            _listOfObject.Add(value!);
            return _listOfObject.Count - 1;
        }

        if (_collectionOfObject != null)
        {
            _collectionOfObject.Add(value!);
            return _collectionOfObject.Count - 1;
        }

        return -1;
    }

    public void Clear()
    {
        if (!IsValidCollection || IsReadOnly)
        {
            return;
        }

        if (_listOfObject != null)
        {
            _listOfObject.Clear();
            return;
        }

        if (_list != null)
        {
            _list.Clear();
            return;
        }

        if (_collectionOfObject != null)
        {
            _collectionOfObject.Clear();
            return;
        }
    }

    public bool Contains(object? item)
    {
        if (!IsValidCollection)
        {
            return false;
        }
        if (item == null)
        {
            return false;
        }

        if (_listOfObject != null)
        {
            return _listOfObject.Contains(item);
        }

        if (_collectionOfObject != null)
        {
            return _collectionOfObject.Contains(item);
        }

        if (_list != null)
        {
            return _list.Contains(item);
        }

        return false;
    }

    public void CopyTo(object[] array, int arrayIndex)
    {
        if (!IsValidCollection)
        {
            return;
        }

        if (_collectionOfObject != null)
        {
            var i = arrayIndex;
            foreach (var it in _collectionOfObject)
            {
                if ((uint)i >= (uint)array.Length)
                {
                    break;
                }

                array[i++] = it!;
            }
            return;
        }

        if (_listOfObject != null)
        {
            var i = arrayIndex;
            for (var idx = 0; idx < _listOfObject.Count && i < array.Length; idx++, i++)
            {
                array[i] = _listOfObject[idx]!;
            }
            return;
        }

        if (_list != null)
        {
            var i = arrayIndex;
            for (var idx = 0; idx < _list.Count && i < array.Length; idx++, i++)
            {
                array[i] = _list[idx]!;
            }
            return;
        }

        if (_collection != null)
        {
            _collection.CopyTo(array, arrayIndex);
            return;
        }
    }

    public void CopyTo(Array array, int index)
    {
        if (!IsValidCollection)
        {
            return;
        }

        if (_collection != null)
        {
            _collection.CopyTo(array, index);
            return;
        }

        var i = index;
        foreach (var item in this)
        {
            if ((uint)i >= (uint)array.Length)
            {
                break;
            }

            array.SetValue(item, i++);
        }
    }

    /// <summary>
    /// Returns the first element in the wrapped collection, or default if the collection is invalid or empty.
    /// </summary>
    public object? FirstOrDefault()
    {
        if (!IsValidCollection)
        {
            return default;
        }

        if (_listOfObject != null)
        {
            return _listOfObject.Count > 0 ? _listOfObject[0] : default;
        }

        if (_list != null)
        {
            return _list.Count > 0 ? _list[0] : default;
        }

        if (_enumerableOfObject != null)
        {
            foreach (var item in _enumerableOfObject)
            {
                return item;
            }
            return default;
        }

        if (_enumerable != null)
        {
            foreach (var item in _enumerable)
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    /// Returns the first element that matches the predicate, or default if none found or the collection is invalid.
    /// </summary>
    public object? FirstOrDefault(Func<object, bool> predicate)
    {
        if (!IsValidCollection || predicate == null)
        {
            return default;
        }

        foreach (var item in this)
        {
            if (predicate(item))
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    /// Returns the first element of type T (optionally matching a predicate), or default if none found or invalid.
    /// </summary>
    public T? FirstOrDefault<T>(Func<T, bool>? predicate = null)
    {
        if (!IsValidCollection)
        {
            return default;
        }

        if (predicate == null)
        {
            foreach (var item in GetEnumerable<T>())
            {
                return item;
            }
            return default;
        }

        foreach (var item in GetEnumerable<T>())
        {
            if (predicate(item))
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    /// Enumerates the wrapped collection yielding only items assignable to T.
    /// Yields nothing if the collection is invalid.
    /// </summary>
    public IEnumerable<T> GetEnumerable<T>()
    {
        if (!IsValidCollection)
        {
            yield break;
        }

        if (_enumerableOfObject != null)
        {
            foreach (var item in _enumerableOfObject)
            {
                if (item is T t)
                {
                    yield return t;
                }
            }
            yield break;
        }

        if (_enumerable != null)
        {
            foreach (var item in _enumerable)
            {
                if (item is T t)
                {
                    yield return t;
                }
            }
        }
    }

    public IEnumerator<object> GetEnumerator()
    {
        if (!IsValidCollection)
        {
            return EmptyEnumerator();
        }

        if (_enumerableOfObject != null)
        {
            return _enumerableOfObject.GetEnumerator();
        }

        if (_enumerable != null)
        {
            return EnumerateNonGeneric(_enumerable);
        }

        return EmptyEnumerator();

        static IEnumerator<object> EmptyEnumerator()
        {
            yield break;
        }

        static IEnumerator<object> EnumerateNonGeneric(IEnumerable source)
        {
            foreach (var item in source)
            {
                yield return item!;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public int IndexOf(object? value)
    {
        if (!IsValidCollection)
        {
            return -1;
        }

        if (_listOfObject != null)
        {
            return _listOfObject.IndexOf(value!);
        }

        return _list != null ? _list.IndexOf(value) : -1;
    }

    public void Insert(int index, object? value)
    {
        if (!IsValidCollection || IsReadOnly)
        {
            return;
        }

        if (_listOfObject != null)
        {
            if ((uint)index <= (uint)_listOfObject.Count)
            {
                _listOfObject.Insert(index, value!);
            }
            return;
        }

        if (_list != null)
        {
            if ((uint)index <= (uint)_list.Count)
            {
                _list.Insert(index, value);
            }
        }
    }

    public bool Remove(object item)
    {
        if (!IsValidCollection || IsReadOnly)
        {
            return false;
        }

        if (_listOfObject != null)
        {
            return _listOfObject.Remove(item);
        }

        if (_collectionOfObject != null)
        {
            return _collectionOfObject.Remove(item);
        }

        if (_list != null && _list.Contains(item))
        {
            _list.Remove(item);
            return true;
        }

        return false;
    }

    void IList.Remove(object? value)
    {
        _ = Remove(value!);
    }

    public void RemoveAt(int index)
    {
        if (!IsValidCollection || IsReadOnly)
        {
            return;
        }

        if (_listOfObject != null)
        {
            if ((uint)index < (uint)_listOfObject.Count)
            {
                _listOfObject.RemoveAt(index);
            }
            return;
        }

        if (_list != null)
        {
            if ((uint)index < (uint)_list.Count)
            {
                _list.RemoveAt(index);
            }
        }
    }
}