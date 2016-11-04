namespace System.Waf.Applications.Services
{
    /// <summary>
    /// Provides method overloads for the <see cref="IMessageService"/> to simplify its usage.
    /// </summary>
    public static class MessageServiceExtensions
    {
        /// <summary>
        /// Shows the message.
        /// </summary>
        /// <param name="service">The message service.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="ArgumentNullException">The argument service must not be null.</exception>
        public static void ShowMessage(this IMessageService service, string message)
        {
            if (service == null) { throw new ArgumentNullException("service"); }
            service.ShowMessage(null, message);
        }

        /// <summary>
        /// Shows the message as warning.
        /// </summary>
        /// <param name="service">The message service.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="ArgumentNullException">The argument service must not be null.</exception>
        public static void ShowWarning(this IMessageService service, string message)
        {
            if (service == null) { throw new ArgumentNullException("service"); }
            service.ShowWarning(null, message);
        }

        /// <summary>
        /// Shows the message as error.
        /// </summary>
        /// <param name="service">The message service.</param>
        /// <param name="message">The message.</param>
        /// <exception cref="ArgumentNullException">The argument service must not be null.</exception>
        public static void ShowError(this IMessageService service, string message)
        {
            if (service == null) { throw new ArgumentNullException("service"); }
            service.ShowError(null, message);
        }

        /// <summary>
        /// Shows the specified question.
        /// </summary>
        /// <param name="service">The message service.</param>
        /// <param name="message">The question.</param>
        /// <returns><c>true</c> for yes, <c>false</c> for no and <c>null</c> for cancel.</returns>
        /// <exception cref="ArgumentNullException">The argument service must not be null.</exception>
        public static bool? ShowQuestion(this IMessageService service, string message)
        {
            if (service == null) { throw new ArgumentNullException("service"); }
            return service.ShowQuestion(null, message);
        }

        /// <summary>
        /// Shows the specified yes/no question.
        /// </summary>
        /// <param name="service">The message service.</param>
        /// <param name="message">The question.</param>
        /// <returns><c>true</c> for yes and <c>false</c> for no.</returns>
        /// <exception cref="ArgumentNullException">The argument service must not be null.</exception>
        public static bool ShowYesNoQuestion(this IMessageService service, string message)
        {
            if (service == null) { throw new ArgumentNullException("service"); }
            return service.ShowYesNoQuestion(null, message);
        }
    }
}
