using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Windows.UI.Core;

namespace LCTWorks.WinUI.Controls;

public enum AdaptiveItemsLayout
{
    List,
    Grid,
    GridFit
}

public partial class AdaptiveItemsView : Control
{
    private readonly TemplateWrapperElementFactory _elementFactory;

    private readonly UniformGridLayout _gridLayout = new()
    {
        Orientation = Orientation.Vertical,
        MinItemWidth = 144,
        MinItemHeight = 0,
        MinRowSpacing = 0,
        MinColumnSpacing = 0,
        ItemsStretch = UniformGridLayoutItemsStretch.None
    };

    private readonly StackLayout _listLayout = new() { Orientation = Orientation.Vertical, Spacing = 0 };

    // Internal selection helper (WinUI 3 does not include SelectionModel)
    private readonly SelectionModelLite _selection;

    private INotifyCollectionChanged? _notifyCollection;
    private ItemsRepeater? _repeater;
    private ScrollViewer? _scrollViewer;

    public AdaptiveItemsView()
    {
        DefaultStyleKey = typeof(AdaptiveItemsView);

        _elementFactory = new TemplateWrapperElementFactory(this);
        _selection = new SelectionModelLite(this);

        SizeChanged += (_, __) => ApplyLayoutAndSizing();
        Loaded += (_, __) => ApplyLayoutAndSizing();

        // keep DPs in sync with selection model
        _selection.SelectionChanged += OnSelectionModelChanged;
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        if (_repeater is not null)
        {
            _repeater.ElementPrepared -= OnElementPrepared;
            _repeater.ElementClearing -= OnElementClearing;
            _repeater.ElementIndexChanged -= OnElementIndexChanged;
        }

        _scrollViewer = GetTemplateChild("PART_ScrollViewer") as ScrollViewer;
        _repeater = GetTemplateChild("PART_Repeater") as ItemsRepeater;

        if (_repeater is not null)
        {
            _repeater.ItemTemplate = _elementFactory;
            _repeater.ItemsSource = ItemsSource;
            _repeater.ElementPrepared += OnElementPrepared;
            _repeater.ElementClearing += OnElementClearing;
            _repeater.ElementIndexChanged += OnElementIndexChanged;
        }

        ApplyLayoutAndSizing();
        SyncSelectionToView();
    }

    #region Dependency Properties

    public static readonly DependencyProperty ColumnSpacingProperty =
        DependencyProperty.Register(nameof(ColumnSpacing), typeof(double), typeof(AdaptiveItemsView),
            new PropertyMetadata(0d, OnLayoutSizingChanged));

    public static readonly DependencyProperty FitSingleRowProperty =
        DependencyProperty.Register(nameof(FitSingleRow), typeof(bool), typeof(AdaptiveItemsView),
            new PropertyMetadata(true, OnLayoutSizingChanged));

    public static readonly DependencyProperty GridItemWidthProperty =
        DependencyProperty.Register(nameof(GridItemWidth), typeof(double), typeof(AdaptiveItemsView),
            new PropertyMetadata(144d, OnLayoutSizingChanged));

    public static readonly DependencyProperty IsItemClickEnabledProperty =
        DependencyProperty.Register(nameof(IsItemClickEnabled), typeof(bool), typeof(AdaptiveItemsView),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ItemContainerStyleProperty =
        DependencyProperty.Register(nameof(ItemContainerStyle), typeof(Style), typeof(AdaptiveItemsView),
            new PropertyMetadata(null, OnItemContainerStyleChanged));

    public static readonly DependencyProperty ItemHeightProperty =
        DependencyProperty.Register(nameof(ItemHeight), typeof(double), typeof(AdaptiveItemsView),
            new PropertyMetadata(0d, OnLayoutSizingChanged));

    public static readonly DependencyProperty ItemSpacingProperty =
        DependencyProperty.Register(nameof(ItemSpacing), typeof(double), typeof(AdaptiveItemsView),
            new PropertyMetadata(0d, OnLayoutSizingChanged));

    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(AdaptiveItemsView),
            new PropertyMetadata(null, OnItemsSourceChanged));

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(AdaptiveItemsView),
            new PropertyMetadata(null, OnItemTemplateChanged));

    public static readonly DependencyProperty ItemTemplateSelectorProperty =
        DependencyProperty.Register(nameof(ItemTemplateSelector), typeof(DataTemplateSelector), typeof(AdaptiveItemsView),
            new PropertyMetadata(null, OnItemTemplateChanged));

    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.Register(nameof(Layout), typeof(AdaptiveItemsLayout), typeof(AdaptiveItemsView),
            new PropertyMetadata(AdaptiveItemsLayout.List, OnLayoutChanged));

    public static readonly DependencyProperty MinItemWidthProperty =
        DependencyProperty.Register(nameof(MinItemWidth), typeof(double), typeof(AdaptiveItemsView),
            new PropertyMetadata(144d, OnLayoutSizingChanged));

    public static readonly DependencyProperty RowSpacingProperty =
        DependencyProperty.Register(nameof(RowSpacing), typeof(double), typeof(AdaptiveItemsView),
            new PropertyMetadata(0d, OnLayoutSizingChanged));

    public static readonly DependencyProperty SelectedIndexProperty =
        DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(AdaptiveItemsView),
            new PropertyMetadata(-1, OnSelectedIndexChanged));

    public static readonly DependencyProperty SelectedItemProperty =
        DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(AdaptiveItemsView),
            new PropertyMetadata(null, OnSelectedItemChanged));

    public static readonly DependencyProperty SelectionModeProperty =
        DependencyProperty.Register(nameof(SelectionMode), typeof(ListViewSelectionMode), typeof(AdaptiveItemsView),
            new PropertyMetadata(ListViewSelectionMode.None, OnSelectionModeChanged));

    public event EventHandler<object?>? ItemClick;

    public event SelectionChangedEventHandler? SelectionChanged;

    public double ColumnSpacing
    {
        get => (double)GetValue(ColumnSpacingProperty);
        set => SetValue(ColumnSpacingProperty, value);
    }

    public bool FitSingleRow
    {
        get => (bool)GetValue(FitSingleRowProperty);
        set => SetValue(FitSingleRowProperty, value);
    }

    // Grid/GridFit sizing
    public double GridItemWidth
    {
        get => (double)GetValue(GridItemWidthProperty);
        set => SetValue(GridItemWidthProperty, value);
    }

    public bool IsItemClickEnabled
    {
        get => (bool)GetValue(IsItemClickEnabledProperty);
        set => SetValue(IsItemClickEnabledProperty, value);
    }

    public Style? ItemContainerStyle
    {
        get => (Style?)GetValue(ItemContainerStyleProperty);
        set => SetValue(ItemContainerStyleProperty, value);
    }

    public double ItemHeight
    {
        get => (double)GetValue(ItemHeightProperty);
        set => SetValue(ItemHeightProperty, value);
    }

    // Spacing for list
    public double ItemSpacing
    {
        get => (double)GetValue(ItemSpacingProperty);
        set => SetValue(ItemSpacingProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public DataTemplate? ItemTemplate
    {
        get => (DataTemplate?)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public DataTemplateSelector? ItemTemplateSelector
    {
        get => (DataTemplateSelector?)GetValue(ItemTemplateSelectorProperty);
        set => SetValue(ItemTemplateSelectorProperty, value);
    }

    public AdaptiveItemsLayout Layout
    {
        get => (AdaptiveItemsLayout)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public double MinItemWidth
    {
        get => (double)GetValue(MinItemWidthProperty);
        set => SetValue(MinItemWidthProperty, value);
    }

    public double RowSpacing
    {
        get => (double)GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    public int SelectedIndex
    {
        get => (int)GetValue(SelectedIndexProperty);
        set => SetValue(SelectedIndexProperty, value);
    }

    public IReadOnlyList<int> SelectedIndices => _selection.SelectedIndices;

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public IReadOnlyList<object?> SelectedItems => _selection.SelectedItems;

    public ListViewSelectionMode SelectionMode
    {
        get => (ListViewSelectionMode)GetValue(SelectionModeProperty);
        set => SetValue(SelectionModeProperty, value);
    }

    // 0 => Auto

    #endregion Dependency Properties

    #region DP Callbacks

    private static void OnItemContainerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AdaptiveItemsView)d)._elementFactory.Invalidate();

    private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var v = (AdaptiveItemsView)d;

        if (v._notifyCollection is not null)
        {
            v._notifyCollection.CollectionChanged -= v.OnItemsCollectionChanged;
            v._notifyCollection = null;
        }

        if (e.NewValue is INotifyCollectionChanged ncc)
        {
            v._notifyCollection = ncc;
            ncc.CollectionChanged += v.OnItemsCollectionChanged;
        }

        if (v._repeater is not null)
        {
            v._repeater.ItemsSource = e.NewValue as IEnumerable;
        }

        // Clear selection if items changed to avoid invalid indices
        v._selection.Clear();

        v.ApplyLayoutAndSizing();
        v.SyncSelectionToView();
    }

    private static void OnItemTemplateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AdaptiveItemsView)d)._elementFactory.Invalidate();

    private static void OnLayoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AdaptiveItemsView)d).ApplyLayoutAndSizing();

    private static void OnLayoutSizingChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AdaptiveItemsView)d).ApplyLayoutAndSizing();

    private static void OnSelectedIndexChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AdaptiveItemsView)d).ApplySelectedIndexToModel();

    private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AdaptiveItemsView)d).ApplySelectedItemToModel();

    private static void OnSelectionModeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        => ((AdaptiveItemsView)d).EnsureSelectionConstraints();

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ApplyLayoutAndSizing();
        // Keep selection valid
        if (_selection.SelectedIndices.Count > 0)
        {
            // purge invalid indices
            var count = GetItemsCount(ItemsSource);
            var toKeep = _selection.SelectedIndices.Where(i => i >= 0 && i < count).ToArray();
            if (toKeep.Length != _selection.SelectedIndices.Count)
            {
                _selection.Clear();
                foreach (var idx in toKeep) _selection.Select(idx);
            }
        }
        SyncSelectionToView();
    }

    #endregion DP Callbacks

    #region Selection plumbing

    private void ApplySelectedIndexToModel()
    {
        if (SelectedIndex < 0)
        {
            _selection.Clear();
            return;
        }

        if (SelectionMode == ListViewSelectionMode.None) return;

        if (SelectionMode == ListViewSelectionMode.Single)
        {
            _selection.Clear();
            _selection.Select(SelectedIndex);
        }
        else
        {
            // leave existing; ensure SelectedIndex is included
            if (!_selection.IsSelected(SelectedIndex))
                _selection.Select(SelectedIndex);
        }
    }

    private void ApplySelectedItemToModel()
    {
        if (ItemsSource is null) return;
        if (SelectedItem is null)
        {
            _selection.Clear();
            return;
        }

        int idx = IndexOf(ItemsSource, SelectedItem);
        if (idx >= 0)
        {
            _selection.Clear();
            _selection.Select(idx);
        }
    }

    private void EnsureSelectionConstraints()
    {
        if (SelectionMode == ListViewSelectionMode.None)
        {
            _selection.Clear();
            return;
        }

        if (SelectionMode == ListViewSelectionMode.Single && _selection.SelectedIndices.Count > 1)
        {
            var first = _selection.SelectedIndices[0];
            _selection.Clear();
            _selection.Select(first);
        }
    }

    private void OnSelectionModelChanged(object? sender, SelectionChangedLiteEventArgs args)
    {
        // Update DPs
        var first = _selection.SelectedIndices.Count > 0 ? _selection.SelectedIndices[0] : -1;
        if (SelectedIndex != first) SetValue(SelectedIndexProperty, first);
        var item = first >= 0 ? _selection.SelectedItems.FirstOrDefault() : null;
        if (!ReferenceEquals(SelectedItem, item)) SetValue(SelectedItemProperty, item);

        // Update realized containers selection state
        if (_repeater is not null)
        {
            foreach (var idx in args.AddedIndices)
                UpdateContainerSelection(idx, true);
            foreach (var idx in args.RemovedIndices)
                UpdateContainerSelection(idx, false);
        }

        // Raise SelectionChanged similar to ListViewBase
        SelectionChanged?.Invoke(this, new SelectionChangedEventArgs(args.RemovedItems, args.AddedItems));
    }

    private void SyncSelectionToView()
    {
        if (_repeater is null) return;

        var count = GetItemsCount(ItemsSource);
        for (int i = 0; i < count; i++)
        {
            UpdateContainerSelection(i, _selection.IsSelected(i));
        }
    }

    private void UpdateContainerSelection(int index, bool isSelected)
    {
        if (_repeater is null) return;
        var element = _repeater.TryGetElement(index) as SelectableRepeaterItem;
        if (element is not null)
        {
            element.IsSelected = isSelected;
        }
    }

    #endregion Selection plumbing

    #region ItemsRepeater element events

    private void OnElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        if (args.Element is SelectableRepeaterItem item)
        {
            item.Tapped -= OnItemTapped;
            item.Holding -= OnItemHolding;
            item.DoubleTapped -= OnItemDoubleTapped;
            item.IsSelected = false;
        }
    }

    private void OnElementIndexChanged(ItemsRepeater sender, ItemsRepeaterElementIndexChangedEventArgs args)
    {
        if (args.Element is SelectableRepeaterItem item)
        {
            item.IsSelected = _selection.IsSelected(args.NewIndex);
        }
    }

    private void OnElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (args.Element is SelectableRepeaterItem item)
        {
            item.Style = ItemContainerStyle ?? item.Style; // apply if provided
            item.IsSelected = _selection.IsSelected(args.Index);
            item.Tapped -= OnItemTapped;
            item.Tapped += OnItemTapped;
            item.Holding -= OnItemHolding;
            item.Holding += OnItemHolding;
            item.DoubleTapped -= OnItemDoubleTapped;
            item.DoubleTapped += OnItemDoubleTapped;
        }
    }

    private void OnItemDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        // optional hook
    }

    private void OnItemHolding(object sender, HoldingRoutedEventArgs e)
    {
        // no-op placeholder for touch long-press context if needed
    }

    private void OnItemTapped(object sender, TappedRoutedEventArgs e)
    {
        if (_repeater is null) return;
        if (sender is not FrameworkElement fe) return;

        var index = _repeater.GetElementIndex(fe);
        if (index < 0) return;

        HandleSelectionOnInteraction(index, e.OriginalSource as UIElement, e.PointerDeviceType);
        if (IsItemClickEnabled)
        {
            var data = GetItemAt(ItemsSource, index);
            ItemClick?.Invoke(this, data);
        }
    }

    #endregion ItemsRepeater element events

    #region Layout sizing

    private void ApplyLayoutAndSizing()
    {
        if (_repeater is null) return;

        switch (Layout)
        {
            case AdaptiveItemsLayout.List:
                _listLayout.Spacing = ItemSpacing;
                _repeater.Layout = _listLayout;
                break;

            case AdaptiveItemsLayout.Grid:
                _gridLayout.MinItemWidth = Math.Max(1, GridItemWidth);
                _gridLayout.MinItemHeight = ItemHeight > 0 ? ItemHeight : 0;
                _gridLayout.MinRowSpacing = RowSpacing;
                _gridLayout.MinColumnSpacing = ColumnSpacing;
                _gridLayout.ItemsStretch = UniformGridLayoutItemsStretch.None;
                _gridLayout.ItemsJustification = UniformGridLayoutItemsJustification.Start;
                _gridLayout.MaximumRowsOrColumns = 0; // not constrained
                _repeater.Layout = _gridLayout;
                break;

            case AdaptiveItemsLayout.GridFit:
                _gridLayout.MinItemWidth = Math.Max(1, MinItemWidth);
                _gridLayout.MinItemHeight = ItemHeight > 0 ? ItemHeight : 0;
                _gridLayout.MinRowSpacing = RowSpacing;
                _gridLayout.MinColumnSpacing = ColumnSpacing;
                _gridLayout.ItemsStretch = UniformGridLayoutItemsStretch.Fill;
                _gridLayout.ItemsJustification = UniformGridLayoutItemsJustification.Start;

                // Optionally fit single row by constraining columns to item count
                if (FitSingleRow)
                {
                    int count = GetItemsCount(ItemsSource);
                    var available = ActualWidth > 0 ? ActualWidth : (_repeater.ActualWidth > 0 ? _repeater.ActualWidth : 0);
                    if (available > 0 && count > 0)
                    {
                        int columns = Math.Max(1, (int)Math.Floor((available + ColumnSpacing) / (MinItemWidth + ColumnSpacing)));
                        if (count <= columns)
                        {
                            _gridLayout.MaximumRowsOrColumns = count; // vertical orientation => columns
                        }
                        else
                        {
                            _gridLayout.MaximumRowsOrColumns = 0;
                        }
                    }
                    else
                    {
                        _gridLayout.MaximumRowsOrColumns = 0;
                    }
                }
                else
                {
                    _gridLayout.MaximumRowsOrColumns = 0;
                }

                _repeater.Layout = _gridLayout;
                break;
        }
    }

    #endregion Layout sizing

    #region Helpers

    private static object? GetItemAt(IEnumerable? source, int index)
    {
        if (source is null || index < 0) return null;
        if (source is IList list) return index < list.Count ? list[index] : null;
        int i = 0;
        foreach (var item in source)
        {
            if (i == index) return item;
            i++;
        }
        return null;
    }

    private static int GetItemsCount(IEnumerable? source)
    {
        if (source is null) return 0;
        if (source is ICollection c) return c.Count;
        if (source is IReadOnlyCollection<object> roc) return roc.Count;
        if (source is IList list) return list.Count;
        int count = 0;
        var e = source.GetEnumerator();
        try { while (e.MoveNext()) count++; }
        finally { (e as IDisposable)?.Dispose(); }
        return count;
    }

    private static int IndexOf(IEnumerable? source, object? target)
    {
        if (source is null || target is null) return -1;
        if (source is IList list) return list.IndexOf(target);
        int i = 0;
        foreach (var item in source)
        {
            if (ReferenceEquals(item, target) || Equals(item, target))
                return i;
            i++;
        }
        return -1;
    }

    private void HandleSelectionOnInteraction(int index, UIElement? origin, PointerDeviceType device)
    {
        if (SelectionMode == ListViewSelectionMode.None)
            return;

        var shift = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Shift).HasFlag(CoreVirtualKeyStates.Down);
        var ctrl = InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);

        if (SelectionMode == ListViewSelectionMode.Single)
        {
            _selection.Clear();
            _selection.Select(index);
            return;
        }

        // Multiple | Extended
        if (SelectionMode == ListViewSelectionMode.Multiple)
        {
            if (_selection.IsSelected(index))
                _selection.Deselect(index);
            else
                _selection.Select(index);
            return;
        }

        // Extended behavior: Shift for range, Ctrl for toggle
        if (SelectionMode == ListViewSelectionMode.Extended)
        {
            if (shift && _selection.SelectedIndices.Count > 0)
            {
                var anchor = _selection.AnchorIndex >= 0 ? _selection.AnchorIndex : _selection.SelectedIndices[0];
                _selection.Clear();
                var start = Math.Min(anchor, index);
                var end = Math.Max(anchor, index);
                _selection.SelectRange(start, end);
            }
            else if (ctrl)
            {
                if (_selection.IsSelected(index))
                    _selection.Deselect(index);
                else
                    _selection.Select(index);
                _selection.AnchorIndex = index;
            }
            else
            {
                _selection.Clear();
                _selection.Select(index);
                _selection.AnchorIndex = index;
            }
        }
    }

    #endregion Helpers

    #region ElementFactory

    private sealed class TemplateWrapperElementFactory : IElementFactory
    {
        private readonly AdaptiveItemsView _owner;

        public TemplateWrapperElementFactory(AdaptiveItemsView owner) => _owner = owner;

        public UIElement GetElement(ElementFactoryGetArgs args)
        {
            var container = new SelectableRepeaterItem();
            if (_owner.ItemContainerStyle is not null)
            {
                container.Style = _owner.ItemContainerStyle;
            }

            var item = args.Data;
            var dt = _owner.ItemTemplateSelector?.SelectTemplate(item, _owner) ?? _owner.ItemTemplate;
            container.ContentTemplate = dt;
            container.Content = item;
            return container;
        }

        public void Invalidate()
        {
            // Force repeater to re-realize using new templates
            if (_owner._repeater is not null)
            {
                // Reset item template to this to trigger refresh
                _owner._repeater.ItemTemplate = null;
                _owner._repeater.ItemTemplate = this;
            }
        }

        public void RecycleElement(ElementFactoryRecycleArgs args)
        {
            if (args.Element is SelectableRepeaterItem c)
            {
                c.Content = null;
                c.ContentTemplate = null;
            }
        }
    }

    #endregion ElementFactory

    #region Internal SelectionModelLite

    private sealed class SelectionChangedLiteEventArgs : EventArgs
    {
        public SelectionChangedLiteEventArgs(
            IList<int> addedIndices,
            IList<int> removedIndices,
            IList<object?> addedItems,
            IList<object?> removedItems)
        {
            AddedIndices = addedIndices;
            RemovedIndices = removedIndices;
            AddedItems = addedItems;
            RemovedItems = removedItems;
        }

        public IList<int> AddedIndices { get; }

        public IList<object?> AddedItems { get; }

        public IList<int> RemovedIndices { get; }

        public IList<object?> RemovedItems { get; }
    }

    private sealed class SelectionModelLite(AdaptiveItemsView owner)
    {
        private readonly AdaptiveItemsView _owner = owner;
        private readonly SortedSet<int> _selected = [];

        public event EventHandler<SelectionChangedLiteEventArgs>? SelectionChanged;

        public int AnchorIndex { get; set; } = -1;

        public IReadOnlyList<int> SelectedIndices => _selected.ToArray();

        public IReadOnlyList<object?> SelectedItems =>
            SelectedIndices.Select(i => GetItem(i)).ToArray();

        public void Clear()
        {
            if (_selected.Count == 0) return;
            var removed = _selected.ToArray();
            _selected.Clear();
            RaiseChanged(Array.Empty<int>(), removed);
        }

        public void Deselect(int index)
        {
            if (index < 0) return;
            if (_selected.Remove(index))
            {
                RaiseChanged(Array.Empty<int>(), new[] { index });
            }
        }

        public bool IsSelected(int index) => _selected.Contains(index);

        public void Select(int index)
        {
            if (index < 0) return;
            if (_selected.Add(index))
            {
                RaiseChanged(new[] { index }, Array.Empty<int>());
            }
        }

        public void SelectRange(int start, int end)
        {
            if (end < start) (start, end) = (end, start);
            var toAdd = new List<int>();
            for (int i = start; i <= end; i++)
            {
                if (_selected.Add(i))
                    toAdd.Add(i);
            }
            if (toAdd.Count > 0)
                RaiseChanged(toAdd, Array.Empty<int>());
        }

        private object? GetItem(int index) => GetItemAt(_owner.ItemsSource, index);

        private void RaiseChanged(IList<int> added, IList<int> removed)
        {
            var addedItems = added.Select(GetItem).ToArray();
            var removedItems = removed.Select(GetItem).ToArray();
            SelectionChanged?.Invoke(this, new SelectionChangedLiteEventArgs(added, removed, addedItems, removedItems));
        }
    }

    #endregion Internal SelectionModelLite
}

/// <summary>
/// Simple selectable container used by ItemsRepeater. Template defined in XAML.
/// </summary>
public sealed class SelectableRepeaterItem : ContentControl
{
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(nameof(IsSelected), typeof(bool), typeof(SelectableRepeaterItem),
            new PropertyMetadata(false));

    public SelectableRepeaterItem()
    {
        DefaultStyleKey = typeof(SelectableRepeaterItem);
    }

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }
}