using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace TOTVSHub
{
    public static class NativeWin32
    {
        public const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;
        public const uint TOKEN_QUERY = 0x0008;
        public const int WM_GETMINMAXINFO = 0x0024;
        public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MINMAXINFO
        {
            public POINT ptReserved;
            public POINT ptMaxSize;
            public POINT ptMaxPosition;
            public POINT ptMinTrackSize;
            public POINT ptMaxTrackSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public static string GetProcessImagePathSafe(int pid)
        {
            IntPtr hProc = IntPtr.Zero;
            try
            {
                hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                if (hProc != IntPtr.Zero)
                {
                    StringBuilder sb = new StringBuilder(1024);
                    int size = sb.Capacity;
                    if (QueryFullProcessImageName(hProc, 0, sb, ref size))
                    {
                        return sb.ToString();
                    }
                }
            }
            catch { }
            finally
            {
                if (hProc != IntPtr.Zero)
                    CloseHandle(hProc);
            }
            return null;
        }

        public static string GetProcessOwnerUser(int pid)
        {
            if (pid <= 0) return null;

            IntPtr hProc = IntPtr.Zero;
            IntPtr hToken = IntPtr.Zero;
            try
            {
                hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                if (hProc == IntPtr.Zero)
                {
                    hProc = OpenProcess(0x0400, false, pid);
                }

                if (hProc != IntPtr.Zero && OpenProcessToken(hProc, TOKEN_QUERY | 0x0002, out hToken))
                {
                    using (WindowsIdentity wi = new WindowsIdentity(hToken))
                    {
                        string full = wi.Name;
                        if (!string.IsNullOrEmpty(full))
                        {
                            int slash = full.IndexOf('\\');
                            return slash >= 0 ? full.Substring(slash + 1) : full;
                        }
                    }
                }
            }
            catch { }
            finally
            {
                if (hToken != IntPtr.Zero) CloseHandle(hToken);
                if (hProc != IntPtr.Zero) CloseHandle(hProc);
            }

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "tasklist.exe";
                psi.Arguments = string.Format("/v /fi \"PID eq {0}\" /fo csv /nh", pid);
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    if (p != null)
                    {
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(2000);
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            string[] cols = output.Trim().Split(new string[] { "\",\"" }, StringSplitOptions.None);
                            if (cols.Length >= 7)
                            {
                                string userCol = cols[6].Trim('"').Trim();
                                if (!string.IsNullOrEmpty(userCol) && !string.Equals(userCol, "N/A", StringComparison.OrdinalIgnoreCase))
                                {
                                    int slash = userCol.IndexOf('\\');
                                    return slash >= 0 ? userCol.Substring(slash + 1) : userCol;
                                }
                            }
                        }
                    }
                }
            }
            catch { }

            try
            {
                return Environment.UserName;
            }
            catch { }

            return null;
        }

        public static IntPtr MultiMonitorWndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_GETMINMAXINFO)
            {
                MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));
                IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
                if (monitor != IntPtr.Zero)
                {
                    MONITORINFO monitorInfo = new MONITORINFO();
                    monitorInfo.cbSize = Marshal.SizeOf(typeof(MONITORINFO));
                    if (GetMonitorInfo(monitor, ref monitorInfo))
                    {
                        RECT rcWork = monitorInfo.rcWork;
                        RECT rcMonitor = monitorInfo.rcMonitor;
                        mmi.ptMaxPosition.X = Math.Abs(rcWork.Left - rcMonitor.Left);
                        mmi.ptMaxPosition.Y = Math.Abs(rcWork.Top - rcMonitor.Top);
                        mmi.ptMaxSize.X = Math.Abs(rcWork.Right - rcWork.Left);
                        mmi.ptMaxSize.Y = Math.Abs(rcWork.Bottom - rcWork.Top);
                    }
                }
                Marshal.StructureToPtr(mmi, lParam, true);
                handled = true;
            }
            return IntPtr.Zero;
        }
    }
}
