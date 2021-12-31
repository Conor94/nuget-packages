namespace PrismMvvmBase.Bindable
{
    /// <summary>Class that allows creating a <see cref="IDataErrorValidator"/> that takes
    /// a generic type for its value. A <see cref="IDataErrorValidator"/> is used by 
    /// <see cref="DataErrorBindableBase"/> to validate properties.</summary>
    /// <remarks>This class provides a reusable object for creating <see cref="IDataErrorValidator"/>s. 
    /// The alternative to using this class is to define a class that inherits <see cref="IDataErrorValidator"/>
    /// for every property you want to validate.</remarks>
    /// <typeparam name="T">The type of property being validated.</typeparam>
    public class DataErrorValidator<T> : IDataErrorValidator
    {
        /// <summary>Validator method.</summary>
        /// <param name="value">The value being validated.</param>
        /// <param name="errorMessage">The error message. Use an empty string if no error occurs.</param>
        /// <returns><see langword="true"/> if the result is valid and <see langword="false"/> if the result is invalid.</returns>
        public delegate bool Validator(T value, out string errorMessage);

        /// <summary>The method that validates the property.</summary>
        public Validator Method { get; set; }

        /// <summary>Calls the <see cref="Validator"/> function assigned to <see cref="Method"/>.</summary>
        public bool Validate(object value, out string errorMessage)
        {
            return Method.Invoke((T)value, out errorMessage);
        }

        /// <summary>Constructor for the <see cref="DataErrorValidator{T}"/>.</summary>
        /// <param name="method">The function that validates the property.</param>
        public DataErrorValidator(Validator method)
        {
            Method = method;
        }
    }
}
