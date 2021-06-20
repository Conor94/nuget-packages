namespace MvvmBase.Bindable
{
    /// <summary>
    /// Provides a function that is used by <see cref="DataErrorBindableBase"/> to validate properties.
    /// </summary>
    /// <remarks>
    /// Refer to <see cref="DataErrorValidator{T}"/> for a concrete implementation of this interface.
    /// </remarks>
    public interface IDataErrorValidator
    {
        /// <summary>
        /// Invokes a validator method. The validator method must be defined in the class
        /// that inherits from <see cref="IDataErrorValidator"/>.
        /// </summary>
        /// <param name="value">The value that is being validated.</param>
        /// <param name="errorMessage">The error message returned by the validator. Use an 
        /// empty string ("") if there is no error message.</param>
        /// <returns><see langword="true"/> if the value is valid and <see langword="false"/> if
        /// the value is not valid.</returns>
        bool Validate(object value, out string errorMessage);
    }
}
