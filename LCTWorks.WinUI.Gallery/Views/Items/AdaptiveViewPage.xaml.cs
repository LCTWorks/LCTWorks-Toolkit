using CommunityToolkit.WinUI.Collections;
using LCTWorks.WinUI.Controls;
using LCTWorks.Workshop.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.VisualBasic;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class AdaptiveViewPage : ObservablePage
{
    private const int BaseItemCount = 64;

    private readonly ObservableCollection<ColorItem> _colors;
    private readonly AdvancedCollectionView _viewSource;

    public AdaptiveViewPage()
    {
        InitializeComponent();
        _colors = [];
        _viewSource = new AdvancedCollectionView(_colors);
    }

    public int FilterCount
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateFilters();
            }
        }
    }

    public object? ItemTemplate
    {
        get;
        set => SetProperty(ref field, value);
    }

    public Layout? Layout
    {
        get;
        set => SetProperty(ref field, value);
    }

    public ICollectionView View => _viewSource;

    private static Color HsvToColor(double hue, double saturation, double value)
    {
        int hi = (int)(hue / 60) % 6;
        double f = hue / 60 - Math.Floor(hue / 60);

        byte v = (byte)(value * 255);
        byte p = (byte)(value * (1 - saturation) * 255);
        byte q = (byte)(value * (1 - f * saturation) * 255);
        byte t = (byte)(value * (1 - (1 - f) * saturation) * 255);

        return hi switch
        {
            0 => Color.FromArgb(255, v, t, p),
            1 => Color.FromArgb(255, q, v, p),
            2 => Color.FromArgb(255, p, v, t),
            3 => Color.FromArgb(255, p, q, v),
            4 => Color.FromArgb(255, t, p, v),
            _ => Color.FromArgb(255, v, p, q),
        };
    }

    private void AddItemsToSourceTapped(object _, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs __)
    {
        int count = _colors.Count;
        for (int i = count; i < count + BaseItemCount; i++)
        {
            double hue = 360.0 * i / (count + BaseItemCount);
            Color c = HsvToColor(hue, 1.0, 1.0);
            _colors.Add(new ColorItem($"Color {i}", c, i + 1));
        }
    }

    private void AddOneColorTapped(object _, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs __)
    {
        double hue = 360.0 * 0 / (1 + BaseItemCount);
        Color c = HsvToColor(hue, 1.0, 1.0);
        _colors.Insert(1, new ColorItem($"Color x", c, _colors.Count + 1));
    }

    private void BringFirstIndexTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        ItemsView.BringIntoView(0);
    }

    private void BringLastIndexTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (_colors.Count > 0)
        {
            ItemsView.BringIntoView(_colors.Count - 1);
        }
    }

    private void BringLastItemTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (_colors.Count > 0)
        {
            var last = _colors.Last();
            ItemsView.BringIntoView(last);
        }
    }

    private void ChangeLayoutTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (Layout is null || Layout is UniformGridLayout)
        {
            Layout = new StackLayout
            {
                Spacing = 4,
            };
        }
        else
        {
            Layout = new UniformGridLayout
            {
                ItemsJustification = UniformGridLayoutItemsJustification.Start,
                ItemsStretch = UniformGridLayoutItemsStretch.Fill,
                MinColumnSpacing = 6,
                MinItemWidth = 200,
                MinRowSpacing = 6,
            };
        }
    }

    private void ChangeTemplateTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        var template = Resources["RoundedColorItemTemplate"] as DataTemplate;
        if (template != null)
        {
            ItemTemplate = template;
        }
    }

    private void ItemsView_ItemClicked(object sender, AdaptiveViewItemClickedEventArgs e)
    {
        var color = e.ClickedItem as ColorItem;
        if (color != null)
        {
            _colors.Remove(color);
        }
    }

    private void LoadItemsSourceTapped(object _, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs __)
    {
        var template = Resources["ColorItemTemplate"] as DataTemplate;
        if (template != null)
        {
            ItemTemplate = template;
            _colors.Clear();
            int count = BaseItemCount;
            for (int i = 0; i < count; i++)
            {
                double hue = 360.0 * i / count;
                Color c = HsvToColor(hue, 1.0, 1.0);
                _colors.Add(new ColorItem($"Color", c, i + 1));
            }
        }
    }

    private void UpdateFilters()
    {
        _viewSource.Filter = item =>
        {
            if (item is ColorItem colorItem)
            {
                return colorItem.Index % (FilterCount + 1) == 0;
            }
            return false;
        };
        // RefreshFilter() already raises a CollectionChanged (Reset) that the ItemsRepeater
        // reacts to. Re-pushing the same view instance via OnPropertyChanged(nameof(View)) is
        // redundant and can interrupt the in-flight rearrange, so it is intentionally omitted.
        _viewSource.RefreshFilter();
    }
}