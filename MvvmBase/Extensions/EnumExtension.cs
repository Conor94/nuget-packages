using System;
using System.ComponentModel;
using System.Reflection;

namespace MvvmBase.Extensions
{
    public static class EnumExtension
    {
        /// <summary>
        /// Gets the description that is stored in a <see cref="DescriptionAttribute"/> for an enum value.
        /// </summary>
        /// <remarks>An <see cref="Exception"/> is thrown if the enum value does not have a <see cref="DescriptionAttribute"/> and if the 
        /// <paramref name="value"/> does not exist in the enum.</remarks>
        /// <typeparam name="T">The type of <see cref="Enum"/>.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns><see cref="DescriptionAttribute.Description"/></returns>
        public static string GetDescription<T>(this T value) where T : Enum
        {
            return value.GetDescription(value.GetType());
        }


        /// <summary>
        /// Gets the description that is stored in a <see cref="DescriptionAttribute"/> for an enum value.
        /// </summary>
        /// <remarks>An <see cref="Exception"/> is thrown if the enum value does not have a <see cref="DescriptionAttribute"/> and if the 
        /// <paramref name="value"/> does not exist in the enum.</remarks>
        /// <typeparam name="T">The type of <see cref="Enum"/>.</typeparam>
        /// <param name="value">The enum value.</param>
        /// <returns><see cref="DescriptionAttribute.Description"/></returns>
        public static string GetDescription(this Enum value, Type enumType)
        {
            // Get the name of enum value and then use it to get value's attributes/metadata
            string fieldName = Enum.GetName(enumType, value);
            if (fieldName == null)
            {
                throw new Exception($"The value '{value}' does not exist in the '{enumType.Name}' enum.");
            }
            FieldInfo field = enumType.GetField(fieldName); // Get the fields attributes/metadata

            // Get the description attribute and return its description
            DescriptionAttribute descAttribute = field.GetCustomAttribute<DescriptionAttribute>();
            if (descAttribute == null)
            {
                throw new Exception($"The property '{fieldName}' from the enum '{enumType}' does not have a {nameof(DescriptionAttribute)}.");
            }
            return descAttribute.Description;
        }
    }
}
