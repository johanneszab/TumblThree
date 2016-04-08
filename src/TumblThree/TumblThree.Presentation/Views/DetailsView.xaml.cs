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
using System.Windows.Navigation;
using System.Windows.Shapes;
using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.Views
{
    /// <summary>
    /// Interaction logic for QueueView.xaml
    /// </summary>
    [Export(typeof(IDetailsView))]
    public partial class DetailsView : IDetailsView
    {
        private readonly Lazy<DetailsViewModel> viewModel;

        public DetailsView()
        {
            InitializeComponent();
            this.viewModel = new Lazy<DetailsViewModel>(() => ViewHelper.GetViewModel<DetailsViewModel>(this));

        }

        private DetailsViewModel ViewModel { get { return viewModel.Value; } }
    }
}
