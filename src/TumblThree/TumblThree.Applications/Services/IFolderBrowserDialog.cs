using System;
using System.Windows;

namespace TumblThree.Applications.Services
{
    public interface IFolderBrowserDialog
    {
        string Description { get; set; }
        Environment.SpecialFolder RootFolder { get; set; }
        string SelectedPath { get; set; }
        bool ShowNewFolderButton { get; set; }
        bool? ShowDialog();
        bool? ShowDialog(Window owner);
    }
}
