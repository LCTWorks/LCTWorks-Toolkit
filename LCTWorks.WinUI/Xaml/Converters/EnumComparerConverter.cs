using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace LCTWorks.WinUI.Converters;

public partial class EnumComparerConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString)
        {
            if (!Enum.IsDefined(typeof(ElementTheme), value))
            {
                throw new ArgumentException("value must be an enum", nameof(value));
            }

            var enumValue = Enum.Parse<ElementTheme>(enumString);

            return enumValue.Equals(value);
        }

        throw new ArgumentException(null, nameof(parameter));
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        if (parameter is string enumString)
        {
            return Enum.Parse<ElementTheme>(enumString);
        }

        throw new ArgumentException(null, nameof(parameter));
    }
}