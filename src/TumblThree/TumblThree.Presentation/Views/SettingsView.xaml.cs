using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Input;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;
using System.Text.RegularExpressions;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    [Export(typeof(ISettingsView)), PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class SettingsView : Window, ISettingsView
    {

        private readonly Lazy<SettingsViewModel> viewModel;

        public SettingsView()
        {
            InitializeComponent();
            this.viewModel = new Lazy<SettingsViewModel>(() => ViewHelper.GetViewModel<SettingsViewModel>(this));
        }

        private SettingsViewModel ViewModel { get { return viewModel.Value; } }

        public void ShowDialog(object owner)
        {
            Owner = owner as Window;
            ShowDialog();
        }

        private void closeWindow(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}
