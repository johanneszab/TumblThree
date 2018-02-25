using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

namespace TumblThree.Applications.Services
{

    [Export(typeof(IFolderBrowserDialog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class WindowsFormsFolderBrowserDialog : IFolderBrowserDialog
    {
        private string _description;
        private string _selectedPath;

        [ImportingConstructor]
        public WindowsFormsFolderBrowserDialog()
        {
            RootFolder = System.Environment.SpecialFolder.MyComputer;
            ShowNewFolderButton = false;
        }

        #region IFolderBrowserDialog Members

        public string Description
        {
            get { return _description ?? string.Empty; }
            set { _description = value; }
        }

        public System.Environment.SpecialFolder RootFolder { get; set; }

        public string SelectedPath
        {
            get { return _selectedPath ?? string.Empty; }
            set { _selectedPath = value; }
        }

        public bool ShowNewFolderButton { get; set; }

        public bool? ShowDialog()
        {
            using (var dialog = CreateDialog())
            {
                var result = dialog.ShowDialog() == DialogResult.OK;
                if (result) SelectedPath = dialog.SelectedPath;
                return result;
            }
        }

        public bool? ShowDialog(Window owner)
        {
            using (var dialog = CreateDialog())
            {
                var result = dialog.ShowDialog(owner.AsWin32Window()) == DialogResult.OK;
                if (result) SelectedPath = dialog.SelectedPath;
                return result;
            }
        }
        #endregion

        private FolderBrowserDialog CreateDialog()
        {
            var dialog = new FolderBrowserDialog();
            dialog.Description = Description;
            dialog.RootFolder = RootFolder;
            dialog.SelectedPath = SelectedPath;
            dialog.ShowNewFolderButton = ShowNewFolderButton;
            return dialog;
        }
    }

    internal static class WindowExtensions
    {
        public static System.Windows.Forms.IWin32Window AsWin32Window(this Window window)
        {
            return new Wpf32Window(window);
        }
    }

    internal class Wpf32Window : System.Windows.Forms.IWin32Window
    {
        public Wpf32Window(Window window)
        {
            Handle = new WindowInteropHelper(window).Handle;
        }

        #region IWin32Window Members

        public IntPtr Handle { get; private set; }

        #endregion
    }
}
