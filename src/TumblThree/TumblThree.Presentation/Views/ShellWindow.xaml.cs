using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Input;

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
        }

        private ShellViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public double VirtualScreenWidth
        {
            get { return SystemParameters.VirtualScreenWidth; }
        }

        public double VirtualScreenHeight
        {
            get { return SystemParameters.VirtualScreenHeight; }
        }

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

        public double GridSplitterPosition
        {
            get { return grid.ColumnDefinitions[2].Width.Value; }
            set { grid.ColumnDefinitions[2].Width = new GridLength(value, GridUnitType.Pixel); }
        }

        private static void TryExecute(ICommand command)
        {
            if (command.CanExecute(null))
            {
                command.Execute(null);
            }
        }
    }
}
