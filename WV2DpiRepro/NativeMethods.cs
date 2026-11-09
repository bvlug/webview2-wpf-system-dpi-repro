using System;
using System.Runtime.InteropServices;

namespace WV2DpiRepro
{
    static class NativeMethods
    {
        public const uint MONITOR_DEFAULTTONEAREST = 2;

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        // MDT_EFFECTIVE_DPI = 0, MDT_ANGULAR_DPI = 1, MDT_RAW_DPI = 2
        [DllImport("shcore.dll")]
        public static extern int GetDpiForMonitor(
            IntPtr hmonitor, uint dpiType,
            out uint dpiX, out uint dpiY);
    }
}
