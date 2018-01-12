using System.Threading.Tasks;
using System.Windows.Input;

namespace System.Waf.Applications
{
    /// <summary>
    /// Provides an <see cref="ICommand"/> implementation which relays the <see cref="Execute"/> and <see cref="CanExecute"/> 
    /// method to the specified delegates.
    /// This implementation disables the command during the async command execution.
    /// </summary>
    public class AsyncDelegateCommand : ICommand
    {
        private static readonly Task<object> completedTask = Task.FromResult((object)null);
        private readonly Func<object, Task> execute;
        private readonly Func<object, bool> canExecute;
        private bool isExecuting;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncDelegateCommand"/> class.
        /// </summary>
        /// <param name="execute">Async Delegate to execute when Execute is called on the command.</param>
        /// <exception cref="ArgumentNullException">The execute argument must not be null.</exception>
        public AsyncDelegateCommand(Func<Task> execute)
            : this(execute, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncDelegateCommand"/> class.
        /// </summary>
        /// <param name="execute">Async Delegate to execute when Execute is called on the command.</param>
        /// <exception cref="ArgumentNullException">The execute argument must not be null.</exception>
        public AsyncDelegateCommand(Func<object, Task> execute)
            : this(execute, null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncDelegateCommand"/> class.
        /// </summary>
        /// <param name="execute">Async Delegate to execute when Execute is called on the command.</param>
        /// <param name="canExecute">Delegate to execute when CanExecute is called on the command.</param>
        /// <exception cref="ArgumentNullException">The execute argument must not be null.</exception>
        public AsyncDelegateCommand(Func<Task> execute, Func<bool> canExecute)
            : this(execute != null ? p => execute() : (Func<object, Task>)null, canExecute != null ? p => canExecute() : (Func<object, bool>)null)
        { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncDelegateCommand"/> class.
        /// </summary>
        /// <param name="execute">Async Delegate to execute when Execute is called on the command.</param>
        /// <param name="canExecute">Delegate to execute when CanExecute is called on the command.</param>
        /// <exception cref="ArgumentNullException">The execute argument must not be null.</exception>
        public AsyncDelegateCommand(Func<object, Task> execute, Func<object, bool> canExecute)
        {
            if (execute == null) { throw new ArgumentNullException(nameof(execute)); }

            this.execute = execute;
            this.canExecute = canExecute;
        }


        /// <summary>
        /// Returns a disabled command.
        /// </summary>
        public static AsyncDelegateCommand DisabledCommand { get; } = new AsyncDelegateCommand(() => completedTask, () => false);

        private bool IsExecuting
        {
            get { return isExecuting; }
            set
            {
                if (isExecuting != value)
                {
                    isExecuting = value;
                    RaiseCanExecuteChanged();
                }
            }
        }


        /// <summary>
        /// Occurs when changes occur that affect whether or not the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;


        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            return !IsExecuting && ((canExecute == null) || canExecute(parameter));
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
        public async void Execute(object parameter)
        {
            if (!CanExecute(parameter))
            {
                return;
            }

            IsExecuting = true;
            try
            {
                await execute(parameter);
            }
            finally
            {
                IsExecuting = false;
            }
        }

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged(EventArgs.Empty);
        }

        /// <summary>
        /// Raises the <see cref="CanExecuteChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected virtual void OnCanExecuteChanged(EventArgs e)
        {
            CanExecuteChanged?.Invoke(this, e);
        }
    }
}
