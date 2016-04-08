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
    /// Interaction logic for AboutView.xaml
    /// </summary>
    [Export(typeof(IAboutView)), PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AboutView : Window, IAboutView
    {

        private readonly Lazy<AboutViewModel> viewModel;

        public AboutView()
        {
            InitializeComponent();
            this.viewModel = new Lazy<AboutViewModel>(() => ViewHelper.GetViewModel<AboutViewModel>(this));
        }

        private AboutViewModel ViewModel { get { return viewModel.Value; } }

        public void ShowDialog(object owner)
        {
            Owner = owner as Window;
            ShowDialog();
        }
    }
}
