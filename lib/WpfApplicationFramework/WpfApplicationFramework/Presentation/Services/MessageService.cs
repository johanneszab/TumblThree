using System.ComponentModel.Composition;
using System.Globalization;
using System.Waf.Applications;
using System.Waf.Applications.Services;
using System.Windows;

namespace System.Waf.Presentation.Services
{
    /// <summary>
    /// This is the default implementation of the <see cref="IMessageService"/> interface. It shows messages via the MessageBox 
    /// to the user.
    /// </summary>
    /// <remarks>
    /// If the default implementation of this service doesn't serve your need then you can provide your own implementation.
    /// </remarks>
    [Export(typeof(IMessageService))]
    public class MessageService : IMessageService
    {
        private static MessageBoxResult MessageBoxResult { get { return MessageBoxResult.None; } }

        private static MessageBoxOptions MessageBoxOptions
        {
            get
            {
                return (CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft) ? MessageBoxOptions.RtlReading : MessageBoxOptions.None;
            }
        }

        /// <summary>
        /// Shows the message.
        /// </summary>
        /// <param name="owner">The window that owns this Message Window.</param>
        /// <param name="message">The message.</param>
        public void ShowMessage(object owner, string message)
        {
            Window ownerWindow = owner as Window;
            if (ownerWindow != null)
            {
                MessageBox.Show(ownerWindow, message, ApplicationInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.None,
                    MessageBoxResult, MessageBoxOptions);
            }
            else
            {
                MessageBox.Show(message, ApplicationInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.None,
                    MessageBoxResult, MessageBoxOptions);
            }
        }

        /// <summary>
        /// Shows the message as warning.
        /// </summary>
        /// <param name="owner">The window that owns this Message Window.</param>
        /// <param name="message">The message.</param>
        public void ShowWarning(object owner, string message)
        {
            Window ownerWindow = owner as Window;
            if (ownerWindow != null)
            {
                MessageBox.Show(ownerWindow, message, ApplicationInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult, MessageBoxOptions);
            }
            else
            {
                MessageBox.Show(message, ApplicationInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.Warning,
                    MessageBoxResult, MessageBoxOptions);
            }
        }

        /// <summary>
        /// Shows the message as error.
        /// </summary>
        /// <param name="owner">The window that owns this Message Window.</param>
        /// <param name="message">The message.</param>
        public void ShowError(object owner, string message)
        {
            Window ownerWindow = owner as Window;
            if (ownerWindow != null)
            {
                MessageBox.Show(ownerWindow, message, ApplicationInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult, MessageBoxOptions);
            }
            else
            {
                MessageBox.Show(message, ApplicationInfo.ProductName, MessageBoxButton.OK, MessageBoxImage.Error,
                    MessageBoxResult, MessageBoxOptions);
            }
        }

        /// <summary>
        /// Shows the specified question.
        /// </summary>
        /// <param name="owner">The window that owns this Message Window.</param>
        /// <param name="message">The question.</param>
        /// <returns><c>true</c> for yes, <c>false</c> for no and <c>null</c> for cancel.</returns>
        public bool? ShowQuestion(object owner, string message)
        {
            Window ownerWindow = owner as Window;
            MessageBoxResult result;
            if (ownerWindow != null)
            {
                result = MessageBox.Show(ownerWindow, message, ApplicationInfo.ProductName, MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question, MessageBoxResult.Cancel, MessageBoxOptions);
            }
            else
            {
                result = MessageBox.Show(message, ApplicationInfo.ProductName, MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question, MessageBoxResult.Cancel, MessageBoxOptions);
            }

            if (result == MessageBoxResult.Yes) { return true; }
            else if (result == MessageBoxResult.No) { return false; }

            return null;
        }

        /// <summary>
        /// Shows the specified yes/no question.
        /// </summary>
        /// <param name="owner">The window that owns this Message Window.</param>
        /// <param name="message">The question.</param>
        /// <returns><c>true</c> for yes and <c>false</c> for no.</returns>
        public bool ShowYesNoQuestion(object owner, string message)
        {
            Window ownerWindow = owner as Window;
            MessageBoxResult result;
            if (ownerWindow != null)
            {
                result = MessageBox.Show(ownerWindow, message, ApplicationInfo.ProductName, MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No, MessageBoxOptions);
            }
            else
            {
                result = MessageBox.Show(message, ApplicationInfo.ProductName, MessageBoxButton.YesNo,
                    MessageBoxImage.Question, MessageBoxResult.No, MessageBoxOptions);
            }

            return result == MessageBoxResult.Yes;
        }
    }
}
