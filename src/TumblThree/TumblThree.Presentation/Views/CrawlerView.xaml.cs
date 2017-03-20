using System;
using System.ComponentModel.Composition;
using System.Waf.Applications;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    /// Interaction logic for CrawlerView.xaml
    /// </summary>
    [Export(typeof(ICrawlerView))]
    public partial class CrawlerView : ICrawlerView
    {
        private readonly Lazy<CrawlerViewModel> viewModel;

        public CrawlerView()
        {
            InitializeComponent();
            this.viewModel = new Lazy<CrawlerViewModel>(() => ViewHelper.GetViewModel<CrawlerViewModel>(this));
        }

        private CrawlerViewModel ViewModel { get { return viewModel.Value; } }
    }
}
