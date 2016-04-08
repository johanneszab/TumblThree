using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using TumblThree.Applications.Services;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    [Export(typeof(IShellView))]
    public partial class ShellWindow : Window, IShellView
    {

        private readonly Lazy<ShellViewModel> viewModel;

        public ShellWindow()
        {
            InitializeComponent();
            viewModel = new Lazy<ShellViewModel>(() => ViewHelper.GetViewModel<ShellViewModel>(this));
            //Loaded += LoadedHandler;
        }


        public double VirtualScreenWidth { get { return SystemParameters.VirtualScreenWidth; } }

        public double VirtualScreenHeight { get { return SystemParameters.VirtualScreenHeight; } }

        public bool IsMaximized
        {
            get { return WindowState == WindowState.Maximized; }
            set
            {
                if (value)
                {
                    WindowState = WindowState.Maximized;
                }
                else if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;
                }
            }
        }

        private ShellViewModel ViewModel { get { return viewModel.Value; } }

        //private void LoadedHandler(object sender, RoutedEventArgs e)
        //{
        //    ViewModel.ShellService.PropertyChanged += ShellServicePropertyChanged;
        //}

        //private void ShellServicePropertyChanged(object sender, PropertyChangedEventArgs e)
        //{
        //    if (e.PropertyName == nameof(IShellService.IsApplicationBusy))
        //    {
        //        if (ViewModel.ShellService.IsApplicationBusy)
        //        {
        //            Mouse.OverrideCursor = Cursors.Wait;
        //        }
        //        else
        //        {
        //            // Delay removing the wait cursor so that the UI has finished its work as well.
        //            Dispatcher.InvokeAsync(() => Mouse.OverrideCursor = null, DispatcherPriority.ApplicationIdle);
        //        }
        //    }
        //}

        private static void TryExecute(ICommand command)
        {
            if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }
}
