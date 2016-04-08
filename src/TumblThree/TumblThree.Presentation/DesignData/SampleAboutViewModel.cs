using TumblThree.Applications.ViewModels;
using TumblThree.Applications.Views;

namespace TumblThree.Presentation.DesignData
{
    public class SampleAboutViewModel : AboutViewModel
    {
        public SampleAboutViewModel() : base(new MockAboutView())
        {
        }


        private class MockAboutView : MockView, IAboutView
        {
            public void ShowDialog(object owner)
            {
            }
        }
    }
}
