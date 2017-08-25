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
    [Export("TumblrPrivateBlogView", typeof(IDetailsView))]
    public partial class DetailsTumblrPrivateBlogView : IDetailsView
    {
        private readonly Lazy<DetailsAllViewModel> viewModel;

        public DetailsTumblrPrivateBlogView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsAllViewModel>(() => ViewHelper.GetViewModel<DetailsAllViewModel>(this));
        }

        private DetailsAllViewModel ViewModel
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
