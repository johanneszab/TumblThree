using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace System.Waf.Presentation
{
    // This class listens to the Validation.Error event of the owner (Control). When the
    // Error event is raised then it synchronizes the errors with its internal errors list and
    // updates the ValidationHelper.
    internal sealed class ValidationTracker
    {
        private readonly List<Tuple<object, ValidationError>> errors;
        private readonly DependencyObject owner;


        public ValidationTracker(DependencyObject owner)
        {
            this.owner = owner;
            this.errors = new List<Tuple<object, ValidationError>>();

            Validation.AddErrorHandler(owner, ErrorChangedHandler);
        }


        internal void AddErrors(object validationSource, IEnumerable<ValidationError> errors)
        {
            foreach (ValidationError error in errors)
            {
                AddError(validationSource, error);
            }

            ValidationHelper.InternalSetIsValid(owner, !errors.Any());
        }

        private void AddError(object validationSource, ValidationError error)
        {
            errors.Add(new Tuple<object, ValidationError>(validationSource, error));

            if (validationSource is FrameworkElement)
            {
                ((FrameworkElement)validationSource).Unloaded += ValidationSourceUnloaded;
            }
            else if (validationSource is FrameworkContentElement)
            {
                ((FrameworkContentElement)validationSource).Unloaded += ValidationSourceUnloaded;
            }
        }

        private void ErrorChangedHandler(object sender, ValidationErrorEventArgs e)
        {
            if (e.Action == ValidationErrorEventAction.Added)
            {
                AddError(e.OriginalSource, e.Error);
            }
            else
            {
                Tuple<object, ValidationError> error = errors.FirstOrDefault(err => err.Item1 == e.OriginalSource && err.Item2 == e.Error);
                if (error != null) { errors.Remove(error); }
            }

            ValidationHelper.InternalSetIsValid(owner, !errors.Any());
        }

        private void ValidationSourceUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement)
            {
                ((FrameworkElement)sender).Unloaded -= ValidationSourceUnloaded;
            }
            else
            {
                ((FrameworkContentElement)sender).Unloaded -= ValidationSourceUnloaded;
            }

            // An unloaded control might be loaded again. Then we need to restore the validation errors.
            Tuple<object, ValidationError>[] errorsToRemove = errors.Where(err => err.Item1 == sender).ToArray();
            if (errorsToRemove.Any())
            {
                // It keeps alive because it listens to the Loaded event.
                new ValidationReloadedTracker(this, errorsToRemove.First().Item1, errorsToRemove.Select(x => x.Item2));

                foreach (Tuple<object, ValidationError> error in errorsToRemove)
                {
                    errors.Remove(error);
                }
            }

            ValidationHelper.InternalSetIsValid(owner, !errors.Any());
        }
    }
}
