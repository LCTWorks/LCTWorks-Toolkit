using LCTWorks.Core.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using System;
using System.Numerics;
using System.Threading.Tasks;

namespace LCTWorks.WinUI.Controls;

[TemplatePart(Name = ItemsRepeaterPartName, Type = typeof(ItemsRepeater))]
[TemplatePart(Name = ItemsScrollHostPartName, Type = typeof(ItemsRepeaterScrollHost))]
[TemplatePart(Name = ItemsScrollPartName, Type = typeof(ScrollViewer))]
public partial class AdaptiveView : Control
{
    public static readonly DependencyProperty IsItemClickEnabledProperty =
        DependencyProperty.Register(nameof(IsItemClickEnabled), typeof(bool), typeof(AdaptiveView),
            new PropertyMetadata(false));

    public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(object), typeof(AdaptiveView),
            new PropertyMetadata(default));

    public static readonly DependencyProperty ItemTemplateProperty =
        DependencyProperty.Register(nameof(ItemTemplate), typeof(object), typeof(AdaptiveView),
            new PropertyMetadata(default));

    public static readonly DependencyProperty LayoutProperty =
            DependencyProperty.Register(nameof(Layout), typeof(Layout), typeof(AdaptiveView),
            new PropertyMetadata(default));

    public static readonly DependencyProperty UseSoftAnimationsProperty =
        DependencyProperty.Register(nameof(UseSoftAnimations), typeof(bool), typeof(AdaptiveView),
            new PropertyMetadata(false));

    private const string ItemsRepeaterPartName = "ItemsRepeater";
    private const string ItemsScrollHostPartName = "ItemsScrollHost";
    private const string ItemsScrollPartName = "ItemsScroll";

    private static readonly DependencyProperty SoftAnimationDataProperty =
        DependencyProperty.RegisterAttached("SoftAnimationData", typeof(SoftAnimationData), typeof(AdaptiveView),
            new PropertyMetadata(null));

    private ItemsRepeater? _itemsRepeater;
    private ScrollViewer? _itemsScroll;
    private ItemsRepeaterScrollHost? _itemsScrollHost;

    public event EventHandler<AdaptiveViewItemClickedEventArgs>? ItemClicked;

    public ItemsRepeater? InternalItemsRepeater => _itemsRepeater;

    public ItemsRepeaterScrollHost? InternalScrollHost => _itemsScrollHost;

    public ScrollViewer? InternalScrollViewer => _itemsScroll;

    public bool IsItemClickEnabled
    {
        get => (bool)GetValue(IsItemClickEnabledProperty);
        set => SetValue(IsItemClickEnabledProperty, value);
    }

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

    public bool UseSoftAnimations
    {
        get => (bool)GetValue(UseSoftAnimationsProperty);
        set => SetValue(UseSoftAnimationsProperty, value);
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

        if (_itemsRepeater is not null)
        {
            _itemsRepeater.Tapped -= OnItemsRepeaterTapped;
            _itemsRepeater.ElementPrepared -= OnElementPrepared;
            _itemsRepeater.ElementClearing -= OnElementClearing;
        }

        _itemsRepeater = GetTemplateChild(ItemsRepeaterPartName) as ItemsRepeater;
        _itemsScroll = GetTemplateChild(ItemsScrollPartName) as ScrollViewer;
        _itemsScrollHost = GetTemplateChild(ItemsScrollHostPartName) as ItemsRepeaterScrollHost;

        if (_itemsRepeater is not null)
        {
            _itemsRepeater.Tapped += OnItemsRepeaterTapped;
            _itemsRepeater.ElementPrepared += OnElementPrepared;
            _itemsRepeater.ElementClearing += OnElementClearing;
        }
    }

    protected virtual void OnItemClicked(AdaptiveViewItemClickedEventArgs args)
    {
        ItemClicked?.Invoke(this, args);
    }

    private static void AttachSoftAnimations(UIElement element)
    {
        ElementCompositionPreview.SetIsTranslationEnabled(element, true);

        var visual = ElementCompositionPreview.GetElementVisual(element);
        var compositor = visual.Compositor;

        var offsetUp = compositor.CreateVector3KeyFrameAnimation();
        offsetUp.InsertKeyFrame(1.0f, new Vector3(0, -4, 0));

        var offsetDown = compositor.CreateVector3KeyFrameAnimation();
        offsetDown.InsertKeyFrame(1.0f, new Vector3(0, 0, 0));

        var scale = compositor.CreateVector3KeyFrameAnimation();
        scale.InsertKeyFrame(0f, new Vector3(0.96f));
        scale.InsertKeyFrame(1f, new Vector3(1f));
        scale.Duration = TimeSpan.FromMilliseconds(800);

        element.SetValue(SoftAnimationDataProperty, new SoftAnimationData(visual, offsetUp, offsetDown, scale));

        element.PointerEntered += OnItemPointerEntered;
        element.PointerExited += OnItemPointerExited;
        element.Tapped += OnItemTapped;
    }

    private static void DetachSoftAnimations(UIElement element)
    {
        element.SetValue(SoftAnimationDataProperty, null);
        element.PointerEntered -= OnItemPointerEntered;
        element.PointerExited -= OnItemPointerExited;
        element.Tapped -= OnItemTapped;
    }

    private static SoftAnimationData? GetAnimationData(UIElement element) =>
        element.GetValue(SoftAnimationDataProperty) as SoftAnimationData;

    private static void OnItemPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (GetAnimationData((UIElement)sender) is { } data)
        {
            data.Visual.StartAnimation("Translation", data.OffsetUp);
        }
    }

    private static void OnItemPointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (GetAnimationData((UIElement)sender) is { } data)
        {
            data.Visual.StartAnimation("Translation", data.OffsetDown);
        }
    }

    private static void OnItemTapped(object sender, TappedRoutedEventArgs e)
    {
        if (GetAnimationData((UIElement)sender) is { } data)
        {
            data.Visual.CenterPoint = new Vector3(data.Visual.Size / 2, 0);
            data.Visual.StartAnimation("Scale", data.Scale);
        }
    }

    private UIElement? FindRepeaterChildFromSource(DependencyObject source)
    {
        var current = source;
        while (current is not null)
        {
            if (current is UIElement uiElement)
            {
                var parent = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(uiElement);
                if (parent == _itemsRepeater)
                {
                    return uiElement;
                }
            }
            current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
        }
        return null;
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

    private void OnElementClearing(ItemsRepeater sender, ItemsRepeaterElementClearingEventArgs args)
    {
        DetachSoftAnimations(args.Element);
    }

    private void OnElementPrepared(ItemsRepeater sender, ItemsRepeaterElementPreparedEventArgs args)
    {
        if (!UseSoftAnimations)
        {
            return;
        }

        AttachSoftAnimations(args.Element);
    }

    private sealed record SoftAnimationData(
        Microsoft.UI.Composition.Visual Visual,
        Microsoft.UI.Composition.Vector3KeyFrameAnimation OffsetUp,
        Microsoft.UI.Composition.Vector3KeyFrameAnimation OffsetDown,
        Microsoft.UI.Composition.Vector3KeyFrameAnimation Scale);

    private void OnItemsRepeaterTapped(object sender, TappedRoutedEventArgs e)
    {
        if (!IsItemClickEnabled || _itemsRepeater is null)
        {
            return;
        }

        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var element = FindRepeaterChildFromSource(source);
        if (element is null)
        {
            return;
        }

        var index = _itemsRepeater.GetElementIndex(element);
        if (index < 0)
        {
            return;
        }

        var items = new TolerantCollection(ItemsSource);
        if (index >= items.Count)
        {
            return;
        }

        var clickedItem = items[index];
        if (clickedItem is not null)
        {
            OnItemClicked(new AdaptiveViewItemClickedEventArgs(clickedItem, element, index));
        }
    }
}