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
    [Export("TumblrBlogView", typeof(IDetailsView))]
    public partial class DetailsTumblrBlogView : IDetailsView
    {
        private readonly Lazy<DetailsTumblrBlogViewModel> viewModel;

        public DetailsTumblrBlogView()
        {
            InitializeComponent();
            viewModel = new Lazy<DetailsTumblrBlogViewModel>(() => ViewHelper.GetViewModel<DetailsTumblrBlogViewModel>(this));
        }

        private DetailsTumblrBlogViewModel ViewModel
        {
            get { return viewModel.Value; }
        }

        // FIXME: Implement in proper MVVM.
        private void Preview_OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            FullScreenMediaView fullScreenMediaView = new FullScreenMediaView { DataContext = viewModel.Value.BlogFile };
            fullScreenMediaView.ShowDialog();
        }
    }
}
