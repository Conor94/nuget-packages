using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace PrismMvvmBase.Extensions
{
    public enum DescriptionType
    {
        Field = 1 << 0,
        Property = 1 << 1,
        FieldAndProperty = 1 << 2
    }

    public static class TypeExtension
    {
        /// <summary>
        /// Gets descriptions for all fields and/or properties for a <see cref="Type"/>. Add a 
        /// <see cref="DescriptionAttribute(string)"/> to a property or field to give it a description.
        /// </summary>
        /// <param name="type">The type of object that has the <see cref="DescriptionAttribute"/> assigned to
        /// its fields and properties.</param>
        /// <param name="descriptionType">Used to specify whether you want this method to get descriptions from 
        /// fields, properties, or fields and properties.</param>
        /// <returns>The descriptions that were retrieved for the <see cref="Type"/>.</returns>
        public static string[] GetDescriptions(this Type type, DescriptionType descriptionType)
        {
            List<string> descriptions = new List<string>();
            FieldInfo[] fields = null;
            PropertyInfo[] properties = null;

            // Get descriptions for all fields
            if (descriptionType == DescriptionType.Field || descriptionType == DescriptionType.FieldAndProperty)
            {
                fields = type.GetFields();

                for (int i = 0; i < fields.Length; i++)
                {
                    DescriptionAttribute descAttribute = fields[i].GetCustomAttribute<DescriptionAttribute>();
                    if (descAttribute != null)
                    {
                        descriptions.Add(descAttribute.Description);
                    }
                }
            }

            // Get descriptions for all properties
            if (descriptionType == DescriptionType.Property || descriptionType == DescriptionType.FieldAndProperty)
            {
                properties = type.GetProperties();
                for (int i = 0; i < properties.Length; i++)
                {
                    DescriptionAttribute descAttribute = properties[i].GetCustomAttribute<DescriptionAttribute>();
                    if (descAttribute != null)
                    {
                        descriptions.Add(descAttribute.Description);
                    }
                }
            }

            return descriptions.ToArray();
        }

    }
}
