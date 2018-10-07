using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;

using IWin32Window = System.Windows.Forms.IWin32Window;

namespace TumblThree.Applications.Services
{
    [Export(typeof(IFolderBrowserDialog))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    internal class WindowsFormsFolderBrowserDialog : IFolderBrowserDialog
    {
        private string description;
        private string selectedPath;

        [ImportingConstructor]
        public WindowsFormsFolderBrowserDialog()
        {
            RootFolder = Environment.SpecialFolder.MyComputer;
            ShowNewFolderButton = false;
        }

        #region IFolderBrowserDialog Members

        public string Description
        {
            get => description ?? string.Empty;
            set => description = value;
        }

        public Environment.SpecialFolder RootFolder { get; set; }

        public string SelectedPath
        {
            get => selectedPath ?? string.Empty;
            set => selectedPath = value;
        }

        public bool ShowNewFolderButton { get; set; }

        public bool? ShowDialog()
        {
            using (FolderBrowserDialog dialog = CreateDialog())
            {
                bool result = dialog.ShowDialog() == DialogResult.OK;
                if (result) SelectedPath = dialog.SelectedPath;
                return result;
            }
        }

        public bool? ShowDialog(Window owner)
        {
            using (FolderBrowserDialog dialog = CreateDialog())
            {
                bool result = dialog.ShowDialog(owner.AsWin32Window()) == DialogResult.OK;
                if (result) SelectedPath = dialog.SelectedPath;
                return result;
            }
        }

        #endregion

        private FolderBrowserDialog CreateDialog()
        {
            var dialog = new FolderBrowserDialog
            {
                Description = Description,
                RootFolder = RootFolder,
                SelectedPath = SelectedPath,
                ShowNewFolderButton = ShowNewFolderButton
            };
            return dialog;
        }
    }

    internal static class WindowExtensions
    {
        public static IWin32Window AsWin32Window(this Window window)
        {
            return new Wpf32Window(window);
        }
    }

    internal class Wpf32Window : IWin32Window
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
