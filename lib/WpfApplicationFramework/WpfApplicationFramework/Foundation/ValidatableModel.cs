using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;

namespace System.Waf.Foundation
{
    /// <summary>
    /// Defines a base class for a model that supports validation.
    /// </summary>
    [Serializable]
    public abstract class ValidatableModel : Model, INotifyDataErrorInfo
    {
        private static readonly ValidationResult[] noErrors = new ValidationResult[0];
        
        [NonSerialized]
        private readonly Dictionary<string, List<ValidationResult>> errors;
        [NonSerialized]
        private bool hasErrors;


        /// <summary>
        /// Initializes a new instance of the <see cref="ValidatableModel"/> class.
        /// </summary>
        protected ValidatableModel()
        {
            this.errors = new Dictionary<string, List<ValidationResult>>();
        }


        /// <summary>
        /// Gets a value that indicates whether the entity has validation errors.
        /// </summary>
        public bool HasErrors 
        { 
            get { return hasErrors; }
            private set { SetProperty(ref hasErrors, value); }
        }


        /// <summary>
        /// Occurs when the validation errors have changed for a property or for the entire entity.
        /// </summary>
        [field:NonSerialized]
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;


        /// <summary>
        /// Gets the validation errors for the entire entity.
        /// </summary>
        /// <returns>The validation errors for the entity.</returns>
        public IEnumerable<ValidationResult> GetErrors()
        {
            return GetErrors(null);
        }

        /// <summary>
        /// Gets the validation errors for a specified property or for the entire entity.
        /// </summary>
        /// <param name="propertyName">The name of the property to retrieve validation errors for; 
        /// or null or String.Empty, to retrieve entity-level errors.</param>
        /// <returns>The validation errors for the property or entity.</returns>
        public IEnumerable<ValidationResult> GetErrors(string propertyName)
        {
            if (!string.IsNullOrEmpty(propertyName))
            {
                List<ValidationResult> result;
                if (errors.TryGetValue(propertyName, out result))
                {
                    return result;
                }
                return noErrors;
            }
            else
            {
                return errors.Values.SelectMany(x => x).Distinct().ToArray();
            }
        }
        
        IEnumerable INotifyDataErrorInfo.GetErrors(string propertyName)
        {
            return GetErrors(propertyName);
        }

        /// <summary>
        /// Validates the object and all its properties. The validation results are stored and can be retrieved by the 
        /// GetErrors method. If the validation results are changing then the ErrorsChanged event will be raised.
        /// </summary>
        /// <returns>True if the object is valid, otherwise false.</returns>
        public bool Validate()
        {
            List<ValidationResult> validationResults = new List<ValidationResult>();
            Validator.TryValidateObject(this, new ValidationContext(this), validationResults, true);
            if (validationResults.Any())
            {
                errors.Clear();
                foreach (var validationResult in validationResults)
                {
                    var propertyNames = validationResult.MemberNames.Any() ? validationResult.MemberNames : new string[] { "" };
                    foreach (string propertyName in propertyNames)
                    {
                        if (!errors.ContainsKey(propertyName))
                        {
                            errors.Add(propertyName, new List<ValidationResult>() { validationResult });
                        }
                        else
                        {
                            errors[propertyName].Add(validationResult);
                        }
                    }
                }
                RaiseErrorsChanged();
                return false;
            }
            else
            {
                if (errors.Any())
                {
                    errors.Clear();
                    RaiseErrorsChanged();
                }
            }
            return true;
        }

        /// <summary>
        /// Set the property with the specified value and validate the property. If the value is not equal with the field then the field is
        /// set, a PropertyChanged event is raised, the property is validated and it returns true.
        /// </summary>
        /// <typeparam name="T">Type of the property.</typeparam>
        /// <param name="field">Reference to the backing field of the property.</param>
        /// <param name="value">The new value for the property.</param>
        /// <param name="propertyName">The property name. This optional parameter can be skipped
        /// because the compiler is able to create it automatically.</param>
        /// <returns>True if the value has changed, false if the old and new value were equal.</returns>
        /// <exception cref="ArgumentException">The argument propertyName must not be null or empty.</exception>
        protected bool SetPropertyAndValidate<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) { throw new ArgumentException("The argument propertyName must not be null or empty."); }
            
            if (SetProperty(ref field, value, propertyName))
            {
                ValidateProperty(value, propertyName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Validates the property with the specified value. The validation results are stored and can be retrieved by the 
        /// GetErrors method. If the validation results are changing then the ErrorsChanged event will be raised.
        /// </summary>
        /// <param name="value">The value of the property.</param>
        /// <param name="propertyName">The property name. This optional parameter can be skipped
        /// because the compiler is able to create it automatically.</param>
        /// <returns>True if the property value is valid, otherwise false.</returns>
        /// <exception cref="ArgumentException">The argument propertyName must not be null or empty.</exception>
        protected bool ValidateProperty(object value, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrEmpty(propertyName)) { throw new ArgumentException("The argument propertyName must not be null or empty."); }
            
            List<ValidationResult> validationResults = new List<ValidationResult>();
            Validator.TryValidateProperty(value, new ValidationContext(this) { MemberName = propertyName }, validationResults);
            if (validationResults.Any())
            {
                errors[propertyName] = validationResults;
                RaiseErrorsChanged(propertyName);
                return false;
            }
            else
            {
                if (errors.Remove(propertyName))
                {
                    RaiseErrorsChanged(propertyName);
                }
            }
            return true;
        }

        /// <summary>
        /// Raises the <see cref="E:ErrorsChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="System.ComponentModel.DataErrorsChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnErrorsChanged(DataErrorsChangedEventArgs e)
        {
            EventHandler<DataErrorsChangedEventArgs> handler = ErrorsChanged;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void RaiseErrorsChanged(string propertyName = "")
        {
            HasErrors = errors.Any();
            OnErrorsChanged(new DataErrorsChangedEventArgs(propertyName));
        }
    }
}
