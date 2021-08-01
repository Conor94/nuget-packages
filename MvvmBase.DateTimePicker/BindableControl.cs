using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace MvvmBase.DateTimePicker
{
    public abstract class BindableControl : Control, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Assigns the <paramref name="value"/> to the specified <paramref name="property"/>.
        /// </summary>
        /// <typeparam name="T">The properties type.</typeparam>
        /// <param name="property">The property that's being assigned to.</param>
        /// <param name="value">The new value for the property.</param>
        /// <param name="propertyName">The name of the property that's being assigned to.</param>
        /// <returns><see langword="true"/> if the property is changed and <see langword="false"/> if it isn't.</returns>
        public bool SetProperty<T>(ref T property, T value, [CallerMemberName] string propertyName = null)
        {
            if (propertyName == null)
            {
                throw new ArgumentNullException(nameof(propertyName));
            }

            // Check if the property value has changed
            if ((property == null && value == null)
                || property?.Equals(value) == true)
            {
                return false;
            }

            property = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }
    }
}
