using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MOGWAI_CLI
{
    internal class Win32
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOZORDER = 0x0004;

        private static Size GetScreenSize() => new Size(GetSystemMetrics(0), GetSystemMetrics(1));

        private struct Size
        {
            public int Width { get; set; }
            public int Height { get; set; }

            public Size(int width, int height)
            {
                Width = width;
                Height = height;
            }
        }

        [DllImport("User32.dll", ExactSpelling = true, CharSet = CharSet.Auto)]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetWindowRect(HandleRef hWnd, out Rect lpRect);

        [DllImport("User32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool MoveWindow(HandleRef hWnd, int x, int y, int width, int height, bool redraw);

        [StructLayout(LayoutKind.Sequential)]
        
        public struct Rect
        {
            public int Left;        // x position of upper-left corner
            public int Top;         // y position of upper-left corner
            public int Right;       // x position of lower-right corner
            public int Bottom;      // y position of lower-right corner
        }

        public static Rect? GetWindowRect()
        {
            IntPtr window = Process.GetCurrentProcess().MainWindowHandle;

            if (GetWindowRect(new HandleRef(null, window), out Rect rect))
            {
                return rect;
            }

            return null;
        }

        public static bool MoveWindow(int X, int Y, int W, int H)
        {
            IntPtr window = Process.GetCurrentProcess().MainWindowHandle;
            return MoveWindow(new HandleRef(null, window), X, Y, W, H, true);
        }

        private static Size GetWindowSize(IntPtr window)
        {
            if (!GetWindowRect(new HandleRef(null, window), out Rect rect))
                throw new Exception("Unable to get window rect!");

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;

            return new Size(width, height);
        }

        public static void MoveWindowToCenter()
        {
            IntPtr window = Process.GetCurrentProcess().MainWindowHandle;

            if (window == IntPtr.Zero)
            {
                Size screenSize = GetScreenSize();
                Size windowSize = GetWindowSize(window);

                int x = (screenSize.Width - windowSize.Width) / 2;
                int y = (screenSize.Height - windowSize.Height) / 2;

                SetWindowPos(window, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
            }
        }

        public static void MoveWindow(int x, int y)
        {
            IntPtr window = Process.GetCurrentProcess().MainWindowHandle;

            if (window != IntPtr.Zero)
            {
                SetWindowPos(window, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
            }
        }
    }
}
