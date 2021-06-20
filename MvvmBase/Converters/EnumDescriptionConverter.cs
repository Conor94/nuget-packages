using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using MvvmBase.Extensions;
using System.Windows.Controls;
using System.Collections;
using System.Collections.Generic;

namespace MvvmBase.Converters
{
    /// <summary>
    /// Converts enum values to descriptions and vice versa. This class cannot convert more than one 
    /// description back to an enum value (e.g. an array, <see cref="List{T}"/>, or <see cref="IEnumerable"/> 
    /// of descriptions cannot be converted back to enum values).<para/>
    /// 
    /// The intended purpose of this class is for binding all values in an enum to a read-only <see cref="ComboBox"/>
    /// and also converting the selected value back and forth between its <see cref="DescriptionAttribute"/> and
    /// value.
    /// </summary>
    /// <remarks><b>How to give descriptions to enum values</b><br/>
    /// Descriptions are given to enumeration values through a <see cref="DescriptionAttribute(string)"/>.</remarks>
    public class EnumDescriptionConverter : IValueConverter
    {
        /// <summary>
        /// Converts single enumeration values and arrays of enumeration values to descriptions (i.e. it converts binding source to binding target). 
        /// </summary>
        /// <remarks>
        /// Refer to <see cref="EnumExtension.GetDescription{T}(T)"/> and <see cref="EnumExtension.GetDescription(Enum, Type)"/>
        /// for more information on how this method gets a description from a <see cref="DescriptionAttribute"/>.
        /// </remarks>
        /// <inheritdoc cref="IValueConverter.Convert(object, Type, object, CultureInfo)" path="//*[not(self::returns or self::summary)]"/>
        /// <returns><see cref="DescriptionAttribute.Description"/> for the enumeration value(s) if the enumeration value(s) had
        /// a <see cref="DescriptionAttribute"/>.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Type enumType = value.GetType();
            if (enumType.IsArray)
            {
                string[] filterTypeStrings = enumType.GetElementType().GetDescriptions(DescriptionType.Field);
                return filterTypeStrings;
            }
            else
            {
                return ((Enum)value).GetDescription(enumType);
            }
        }

        /// <summary>
        /// Converts descriptions to enumeration values.
        /// </summary>
        /// <inheritdoc cref="IValueConverter.Convert(object, Type, object, CultureInfo)" path="//*[not(self::returns or self::summary)]"/>
        /// <returns>The value that corresponds to the <see cref="DescriptionAttribute"/> for the <paramref name="targetType"/> enumeration.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string descriptionStr)
            {
                FieldInfo[] fields = targetType.GetFields();

                for (int i = 0; i < fields.Length; i++)
                {
                    DescriptionAttribute descAttribute = fields[i].GetCustomAttribute<DescriptionAttribute>();

                    if (descAttribute != null && descAttribute.Description == descriptionStr)
                    {
                        return Enum.Parse(targetType, fields[i].Name);
                    }
                }

                // Throw an exception because the descriptionStr did not match with any of the DescriptionAttribute descriptions for the enum
                throw new Exception($"{nameof(DescriptionAttribute)} was not found.");
            }
            else
            {
                throw new ArgumentException($"Parameter is a '{value.GetType()}' and it should be a '{typeof(string)}'", nameof(value));
            }
        }
    }

}
