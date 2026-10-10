using DCSB.Utils;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DCSB.Input
{
    /// <summary>
    /// Global low-level mouse hook. Reports wheel movement and middle/side button presses
    /// even when another application is focused. Events are never swallowed.
    /// Must be created on a thread with a message loop (the WPF UI thread).
    /// </summary>
    public class MouseHook : IDisposable
    {
        public delegate void WheelHandler(int delta);
        public delegate void ButtonHandler(VKey button);

        public event WheelHandler Wheel;
        public event ButtonHandler ButtonDown;
        public event ButtonHandler ButtonUp;

        private const int WH_MOUSE_LL = 14;
        private const int WM_MBUTTONDOWN = 0x0207;
        private const int WM_MBUTTONUP = 0x0208;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_XBUTTONDOWN = 0x020B;
        private const int WM_XBUTTONUP = 0x020C;
        private const int XBUTTON1 = 0x0001;

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public int x;
            public int y;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        // Keep a reference so the delegate isn't garbage collected while the hook is installed
        private readonly LowLevelMouseProc _proc;
        private IntPtr _hook;

        public MouseHook()
        {
            _proc = HookCallback;
            using (ProcessModule module = Process.GetCurrentProcess().MainModule)
            {
                _hook = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(module.ModuleName), 0);
            }
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                try
                {
                    int message = wParam.ToInt32();
                    switch (message)
                    {
                        case WM_MOUSEWHEEL:
                            MSLLHOOKSTRUCT data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                            Wheel?.Invoke((short)((data.mouseData >> 16) & 0xFFFF));
                            break;
                        case WM_MBUTTONDOWN:
                            ButtonDown?.Invoke(VKey.MBUTTON);
                            break;
                        case WM_MBUTTONUP:
                            ButtonUp?.Invoke(VKey.MBUTTON);
                            break;
                        case WM_XBUTTONDOWN:
                            ButtonDown?.Invoke(GetXButton(lParam));
                            break;
                        case WM_XBUTTONUP:
                            ButtonUp?.Invoke(GetXButton(lParam));
                            break;
                    }
                }
                catch (Exception e)
                {
                    // Never let an exception escape a hook callback
                    Debug.WriteLine(e);
                }
            }
            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private static VKey GetXButton(IntPtr lParam)
        {
            MSLLHOOKSTRUCT data = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            return ((data.mouseData >> 16) & 0xFFFF) == XBUTTON1 ? VKey.XBUTTON1 : VKey.XBUTTON2;
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
        }
    }
}
