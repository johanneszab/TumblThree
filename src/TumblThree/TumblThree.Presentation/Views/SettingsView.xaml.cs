using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Waf.Applications;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

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
    }
}
