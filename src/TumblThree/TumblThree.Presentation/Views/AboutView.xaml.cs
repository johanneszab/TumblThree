using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows;

using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for AboutView.xaml
    /// </summary>
    [Export(typeof(IAboutView)), PartCreationPolicy(CreationPolicy.NonShared)]
    public partial class AboutView : Window, IAboutView
    {
        private readonly Lazy<AboutViewModel> viewModel;

        public AboutView()
        {
            InitializeComponent();
            viewModel = new Lazy<AboutViewModel>(() => ViewHelper.GetViewModel<AboutViewModel>(this));
        }

        private AboutViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        public void ShowDialog(object owner)
        {
            Owner = owner as Window;
            ShowDialog();
        }
    }
}
