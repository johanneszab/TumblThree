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
    [Export("TumblrTagSearchView", typeof(IDetailsView))]
    public partial class DetailsTumblrTagSearchView : IDetailsView
    {
        private readonly Lazy<DetailsTumblrTagSearchViewModel> viewModel;

        public DetailsTumblrTagSearchView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsTumblrTagSearchViewModel>(() => ViewHelper.GetViewModel<DetailsTumblrTagSearchViewModel>(this));
        }

        private DetailsTumblrTagSearchViewModel ViewModel
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
