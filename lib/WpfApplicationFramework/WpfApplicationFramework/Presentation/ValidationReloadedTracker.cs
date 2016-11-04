using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace System.Waf.Presentation
{
    // This class stores the ValidationErrors of an unloaded Control. When the Control is loaded again then
    // it restores the ValidationErrors.
    internal class ValidationReloadedTracker
    {
        private readonly ValidationTracker validationTracker;
        private readonly IEnumerable<ValidationError> errors;


        public ValidationReloadedTracker(ValidationTracker validationTracker, object validationSource,
            IEnumerable<ValidationError> errors)
        {
            this.validationTracker = validationTracker;
            this.errors = errors;

            if (validationSource is FrameworkElement)
            {
                ((FrameworkElement)validationSource).Loaded += ValidationSourceLoaded;
            }
            else
            {
                ((FrameworkContentElement)validationSource).Loaded += ValidationSourceLoaded;
            }
        }


        private void ValidationSourceLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                ((FrameworkElement)sender).Loaded -= ValidationSourceLoaded;
            }
            else
            {
                ((FrameworkContentElement)sender).Loaded -= ValidationSourceLoaded;
            }

            validationTracker.AddErrors(sender, errors);
        }
    }
}
