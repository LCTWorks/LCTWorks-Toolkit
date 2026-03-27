using LCTWorks.WinUI.Controls;
using LCTWorks.Workshop.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.UI;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class AdaptiveViewPage : ObservablePage
{
    private const int BaseItemCount = 64;

    public AdaptiveViewPage()
    {
        InitializeComponent();
    }

    public object? ItemsSource
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public object? ItemTemplate
    {
        get => field;
        set => SetProperty(ref field, value);
    }

    public Layout? Layout
    {
        get => field;
        set => SetProperty(ref field, value);
    }

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
        if (ItemsSource is ObservableCollection<ColorItem> collection)
        {
            int count = collection.Count;
            for (int i = count; i < count + BaseItemCount; i++)
            {
                double hue = 360.0 * i / (count + BaseItemCount);
                Color c = HsvToColor(hue, 1.0, 1.0);
                collection.Add(new ColorItem($"Color {i}", c));
            }
        }
    }

    private void BringFirstIndexTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (ItemsSource is ObservableCollection<ColorItem> items && items.Count > 0)
        {
            ItemsView.BringIntoView(0);
        }
    }

    private void BringLastIndexTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (ItemsSource is ObservableCollection<ColorItem> items && items.Count > 0)
        {
            ItemsView.BringIntoView(items.Count - 1);
        }
    }

    private void BringLastItemTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (ItemsSource is ObservableCollection<ColorItem> items && items.Count > 0)
        {
            var last = items.Last();
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

    private void LoadItemsSourceTapped(object _, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs __)
    {
        var template = Resources["ColorItemTemplate"] as DataTemplate;
        if (template != null)
        {
            ItemTemplate = template;
            var collection = new ObservableCollection<ColorItem>();
            int count = BaseItemCount;
            for (int i = 0; i < count; i++)
            {
                double hue = 360.0 * i / count;
                Color c = HsvToColor(hue, 1.0, 1.0);
                collection.Add(new ColorItem($"Color {i}", c));
            }
            ItemsSource = collection;
        }
    }
}