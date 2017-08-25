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
    [Export("TumblrSearchView", typeof(IDetailsView))]
    public partial class DetailsTumblrSearchView : IDetailsView
    {
        private readonly Lazy<DetailsTumblrSearchViewModel> viewModel;

        public DetailsTumblrSearchView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsTumblrSearchViewModel>(() => ViewHelper.GetViewModel<DetailsTumblrSearchViewModel>(this));
        }

        private DetailsTumblrSearchViewModel ViewModel
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
