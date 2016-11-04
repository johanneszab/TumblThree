using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Text;

namespace System.Waf.Foundation
{
    /// <summary>
    /// This class provides an implementation for the <see cref="IDataErrorInfo"/> interface which uses the
    /// validation classes found in the <see cref="System.ComponentModel.DataAnnotations"/> namespace.
    /// </summary>
    public sealed class DataErrorInfoSupport : IDataErrorInfo
    {
        private readonly object instance;


        /// <summary>
        /// Initializes a new instance of the <see cref="DataErrorInfoSupport"/> class.
        /// </summary>
        /// <param name="instance">The instance.</param>
        /// <exception cref="ArgumentNullException">instance must not be <c>null</c>.</exception>
        public DataErrorInfoSupport(object instance)
        {
            if (instance == null) { throw new ArgumentNullException("instance"); }
            this.instance = instance;
        }


        /// <summary>
        /// Gets an error message indicating what is wrong with this object.
        /// </summary>
        /// <returns>An error message indicating what is wrong with this object. The default is an empty string ("").</returns>
        public string Error { get { return this[""]; } }

        /// <summary>
        /// Gets the error message for the property with the given name.
        /// </summary>
        /// <param name="memberName">The name of the property whose error message to get.</param>
        /// <returns>The error message for the property. The default is an empty string ("").</returns>
        public string this[string memberName]
        {
            get
            {
                List<ValidationResult> validationResults = new List<ValidationResult>();

                if (string.IsNullOrEmpty(memberName))
                {
                    Validator.TryValidateObject(instance, new ValidationContext(instance, null, null), validationResults, true);
                }
                else
                {
                    PropertyDescriptor property = TypeDescriptor.GetProperties(instance)[memberName];
                    if (property == null)
                    {
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture,
                            "The specified member {0} was not found on the instance {1}", memberName, instance.GetType()));
                    }
                    Validator.TryValidateProperty(property.GetValue(instance),
                        new ValidationContext(instance, null, null) { MemberName = memberName }, validationResults);
                }

                return string.Join(Environment.NewLine, validationResults.Select(x => x.ErrorMessage));
            }
        }
    }
}
