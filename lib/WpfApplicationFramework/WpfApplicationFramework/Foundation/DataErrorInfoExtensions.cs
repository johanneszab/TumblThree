using System.ComponentModel;

namespace System.Waf.Foundation
{
    /// <summary>
    /// Extends the <see cref="IDataErrorInfo"/> interface with new Validation methods.
    /// </summary>
    public static class DataErrorInfoExtensions
    {
        /// <summary>
        /// Validates the specified object.
        /// </summary>
        /// <param name="instance">The object to validate.</param>
        /// <returns>An error message indicating what is wrong with this object. The default is an empty string ("").</returns>
        /// <exception cref="ArgumentNullException">The argument instance must not be null.</exception>
        public static string Validate(this IDataErrorInfo instance)
        {
            if (instance == null) { throw new ArgumentNullException("instance"); }

            return instance.Error ?? "";
        }

        /// <summary>
        /// Validates the specified member of the object.
        /// </summary>
        /// <param name="instance">The object to validate.</param>
        /// <param name="memberName">The name of the member to validate.</param>
        /// <returns>The error message for the member. The default is an empty string ("").</returns>
        /// <exception cref="ArgumentNullException">The argument instance must not be null.</exception>
        public static string Validate(this IDataErrorInfo instance, string memberName)
        {
            if (instance == null) { throw new ArgumentNullException("instance"); }

            return instance[memberName] ?? "";
        }
    }
}
