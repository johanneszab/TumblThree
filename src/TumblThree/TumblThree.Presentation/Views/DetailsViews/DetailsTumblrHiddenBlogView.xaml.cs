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
    [Export("TumblrHiddenBlogView", typeof(IDetailsView))]
    public partial class DetailsTumblrHiddenBlogView : IDetailsView
    {
        private readonly Lazy<DetailsTumblrHiddenBlogViewModel> viewModel;

        public DetailsTumblrHiddenBlogView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsTumblrHiddenBlogViewModel>(() => ViewHelper.GetViewModel<DetailsTumblrHiddenBlogViewModel>(this));
        }

        private DetailsTumblrHiddenBlogViewModel ViewModel
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
