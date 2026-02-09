using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace LCTWorks.WinUI.Converters;

public partial class BooleanToVisibilityConverter : IValueConverter
{
    public bool Invert
    {
        get; set;
    }

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is bool b)
        {
            return b ^ Invert ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (value is Visibility v)
        {
            return v == Visibility.Visible ^ Invert;
        }
        return false;
    }
}