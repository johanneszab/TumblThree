using System.Diagnostics.CodeAnalysis;
using System.Windows.Threading;
using System.Threading;

namespace System.Waf.Applications
{
    /// <summary>
    /// Provides helper methods that perform common tasks involving a view.
    /// </summary>
    public static class ViewHelper
    {
        /// <summary>
        /// Gets the ViewModel which is associated with the specified view.
        /// </summary>
        /// <param name="view">The view.</param>
        /// <returns>The associated ViewModel, or <c>null</c> when no ViewModel was found.</returns>
        /// <exception cref="ArgumentNullException">view must not be <c>null</c>.</exception>
        public static ViewModel GetViewModel(this IView view)
        {
            if (view == null) { throw new ArgumentNullException("view"); }

            object dataContext = view.DataContext;
            // When the DataContext is null then it might be that the ViewModel hasn't set it yet.
            // Enforce it by executing the event queue of the Dispatcher.
            if (dataContext == null && SynchronizationContext.Current is DispatcherSynchronizationContext)
            {
                DispatcherHelper.DoEvents();
                dataContext = view.DataContext;
            }
            return dataContext as ViewModel;
        }
        
        /// <summary>
        /// Gets the ViewModel which is associated with the specified view.
        /// </summary>
        /// <typeparam name="T">The type of the ViewModel</typeparam>
        /// <param name="view">The view.</param>
        /// <returns>The associated ViewModel, or <c>null</c> when no ViewModel was found.</returns>
        /// <exception cref="ArgumentNullException">view must not be <c>null</c>.</exception>
        public static T GetViewModel<T>(this IView view) where T : ViewModel
        {
            return GetViewModel(view) as T;
        }
    }
}
