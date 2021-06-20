using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using MvvmBase.Extensions;

namespace MvvmBase.Converters
{
    public class EnumConverter : IValueConverter
    {
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
