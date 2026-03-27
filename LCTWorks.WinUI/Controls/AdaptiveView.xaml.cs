using LCTWorks.Core.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace LCTWorks.WinUI.Controls;

[TemplatePart(Name = ItemsRepeaterPartName, Type = typeof(ItemsRepeater))]
[TemplatePart(Name = ItemsScrollHostPartName, Type = typeof(ItemsRepeaterScrollHost))]
[TemplatePart(Name = ItemsScrollPartName, Type = typeof(ScrollViewer))]
public partial class AdaptiveView : Control
{
    public static readonly DependencyProperty ItemsSourceProperty =
        DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(AdaptiveView),
            new PropertyMetadata(default));

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(nameof(ItemTemplate), typeof(object), typeof(AdaptiveView),
            new PropertyMetadata(default));

    public static readonly DependencyProperty LayoutProperty =
            DependencyProperty.Register(nameof(Layout), typeof(Layout), typeof(AdaptiveView),
            new PropertyMetadata(default));

    private const string ItemsRepeaterPartName = "ItemsRepeater";
    private const string ItemsScrollHostPartName = "ItemsScrollHost";
    private const string ItemsScrollPartName = "ItemsScroll";

    private ItemsRepeater? _itemsRepeater;
    private ScrollViewer? _itemsScroll;
    private ItemsRepeaterScrollHost? _itemsScrollHost;

    public ItemsRepeater? InternalItemsRepeater => _itemsRepeater;

    public ItemsRepeaterScrollHost? InternalScrollHost => _itemsScrollHost;

    public ScrollViewer? InternalScrollViewer => _itemsScroll;

    public object ItemsSource
    {
        get => (object)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object ItemTemplate
    {
        get => (object)GetValue(ItemTemplateProperty);
        set => SetValue(ItemTemplateProperty, value);
    }

    public Layout Layout
    {
        get => (Layout)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    public async void BringIntoView(object item)
    {
        if (_itemsRepeater is null || _itemsScroll is null)
        {
            return;
        }
        var index = GetItemIndex(item);
        BringIntoView(index);
    }

    public async void BringIntoView(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0, nameof(index));

        if (_itemsRepeater is null || _itemsScroll is null)
        {
            return;
        }
        if (ItemsSource == null)
        {
            return;
        }
        var anchor = _itemsRepeater.TryGetElement(index);
        if (anchor == null)
        {
            anchor = _itemsRepeater.GetOrCreateElement(index);
            await Task.Delay(100);
        }
        anchor.StartBringIntoView(new BringIntoViewOptions
        {
            VerticalAlignmentRatio = 0.5,
            AnimationDesired = true
        });
    }

    public UIElement? GetContainer(object item)
    {
        if (_itemsRepeater is null || _itemsScroll is null)
        {
            return default;
        }
        var index = GetItemIndex(item);
        if (index < 0)
        {
            return default;
        }
        return _itemsRepeater.GetOrCreateElement(index);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _itemsRepeater = GetTemplateChild(ItemsRepeaterPartName) as ItemsRepeater;
        _itemsScroll = GetTemplateChild(ItemsScrollPartName) as ScrollViewer;
        _itemsScrollHost = GetTemplateChild(ItemsScrollHostPartName) as ItemsRepeaterScrollHost;
    }

    private int GetItemIndex(object item)
    {
        ArgumentNullException.ThrowIfNull(item, nameof(item));
        if (ItemsSource == null)
        {
            return -1;
        }
        var items = new TolerantCollection(ItemsSource);
        return items.IndexOf(item);
    }
}