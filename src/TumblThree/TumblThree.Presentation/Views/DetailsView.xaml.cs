using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using System.Windows.Input;

using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    ///     Interaction logic for QueueView.xaml
    /// </summary>
    [Export(typeof(IDetailsView))]
    public partial class DetailsView : IDetailsView
    {
        private readonly Lazy<DetailsViewModel> viewModel;

        public DetailsView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsViewModel>(() => ViewHelper.GetViewModel<DetailsViewModel>(this));
        }

        private DetailsViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        // FIXME: Implement in proper MVVM.
        private void Preview_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            var fullScreenMediaView = new FullScreenMediaView { DataContext = viewModel.Value.BlogFile };
            fullScreenMediaView.ShowDialog();
        }
    }
}
