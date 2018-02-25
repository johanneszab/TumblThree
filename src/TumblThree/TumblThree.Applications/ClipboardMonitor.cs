using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace TumblThree.Applications
{
    public class ClipboardMonitor : IDisposable
    {
        bool disposed = false;

        private readonly HwndSource hwndSource = new HwndSource(0, 0, 0, 0, 0, 0, 0, null, NativeMethods.HWND_MESSAGE);

        public ClipboardMonitor()
        {
            hwndSource.AddHook(WndProc);
            NativeMethods.AddClipboardFormatListener(hwndSource.Handle);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static class NativeMethods
        {
            /// <summary>
            ///     Sent when the contents of the clipboard have changed.
            /// </summary>
            public const int WM_CLIPBOARDUPDATE = 0x031D;

            /// <summary>
            ///     To find message-only windows, specify HWND_MESSAGE in the hwndParent parameter of the FindWindowEx function.
            /// </summary>
            public static readonly IntPtr HWND_MESSAGE = new IntPtr(-3);

            /// <summary>
            ///     Places the given window in the system-maintained clipboard format listener list.
            /// </summary>
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool AddClipboardFormatListener(IntPtr hwnd);

            /// <summary>
            ///     Removes the given window from the system-maintained clipboard format listener list.
            /// </summary>
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool RemoveClipboardFormatListener(IntPtr hwnd);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                NativeMethods.RemoveClipboardFormatListener(hwndSource.Handle);
                hwndSource.RemoveHook(WndProc);
                hwndSource.Dispose();
            }
            // Free any unmanaged objects here.
            //
            disposed = true;
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_CLIPBOARDUPDATE)
            {
                OnClipboardContentChanged?.Invoke(this, EventArgs.Empty);
            }

            return IntPtr.Zero;
        }

        ~ClipboardMonitor()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Occurs when the clipboard content changes.
        /// </summary>
        public event EventHandler OnClipboardContentChanged;
    }
}
