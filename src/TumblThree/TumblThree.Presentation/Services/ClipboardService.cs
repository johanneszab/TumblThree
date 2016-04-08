using System.ComponentModel.Composition;
using System.Windows;
using TumblThree.Applications.Services;

namespace TumblThree.Presentation.Services
{
    [Export(typeof(IClipboardService))]
    internal class ClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            Clipboard.SetText(text);
        }
    }
}
