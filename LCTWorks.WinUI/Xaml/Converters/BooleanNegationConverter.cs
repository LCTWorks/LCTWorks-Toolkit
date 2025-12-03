using Microsoft.UI.Xaml.Data;
using System;

namespace LCTWorks.WinUI.Xaml.Converters;

public partial class BooleanNegationConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return !(value is bool v && v);
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        return !(value is bool v && v);
    }
}