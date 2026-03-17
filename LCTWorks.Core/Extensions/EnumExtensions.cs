using System.ComponentModel;

namespace LCTWorks.Core.Extensions;

public static class EnumExtensions
{
    /// <summary>
    /// Gets the description of an enum value, or its name if no description is set.
    /// </summary>
    /// <param name="value">The enum value.</param>
    /// <returns>The description or name of the enum value.</returns>
    public static string GetDescription(this Enum value)
    {
        var type = value.GetType();
        var memberInfo = type.GetMember(value.ToString()).FirstOrDefault();
        if (memberInfo != null)
        {
            if (memberInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .FirstOrDefault() is DescriptionAttribute descriptionAttribute)
            {
                return descriptionAttribute.Description;
            }
        }
        return value.ToString();
    }
}