using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace MvvmBase.Bindable
{
    public abstract class DataErrorBindableBase : BindableBase, IDataErrorInfo
    {
        #region IDataErrorInfo
        public string this[string propertyName]
        {
            get
            {
                // Check if there is a validator function for the property
                if (ValidatorFunctions.ContainsKey(propertyName) && ValidatorFunctions[propertyName] != null)
                {
                    // Invoke the validator function for the property and return the error message if there is one
                    if (!ValidatorFunctions[propertyName].Invoke(mInheritedType.GetProperty(propertyName).GetValue(this), out string errorMessage))
                    {
                        return errorMessage;
                    }
                }
                return "";
            }
        }
        public string Error
        {
            get => mError;
            private set => mError = value;
        }
        #endregion

        #region Fields
        private string mError;
        private Dictionary<string, IDataErrorValidator> mValidatorFunctions;
        private Type mInheritedType;
        #endregion

        #region Properties
        /******************** Data ********************/
        private Dictionary<string, IDataErrorValidator> ValidatorFunctions
        {
            get => mValidatorFunctions ?? (mValidatorFunctions = new Dictionary<string, IDataErrorValidator>());
            set => mValidatorFunctions = value;
        }
        #endregion

        #region Constructor
        public DataErrorBindableBase()
        {
            // Get the inherited class type
            mInheritedType = GetType();

            // Initialize properties
            Error = "";
            ValidatorFunctions = null;
        }
        #endregion

        #region Methods
        public void AddValidator(string propertyName, IDataErrorValidator validatorFunction)
        {
            if (mInheritedType.GetProperty(propertyName) != null)
            {
                ValidatorFunctions.Add(propertyName, validatorFunction);
            }
            else
            {
                throw new ArgumentException($"The type '{mInheritedType.Name}' does not have a property named '{propertyName}'.", propertyName);
            }
        }
        #endregion
    }
}
