using System;
using System.Diagnostics;
using System.Management;
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

        [StructLayout(LayoutKind.Sequential, Size = 72)]
        public struct PROCESS_MEMORY_COUNTERS_EX
        {
            public uint cb;
            public uint PageFaultCount;
            public UIntPtr PeakWorkingSetSize;
            public UIntPtr WorkingSetSize;
            public UIntPtr QuotaPeakPagedPoolUsage;
            public UIntPtr QuotaPagedPoolUsage;
            public UIntPtr QuotaPeakNonPagedPoolUsage;
            public UIntPtr QuotaNonPagedPoolUsage;
            public UIntPtr PagefileUsage;
            public UIntPtr PeakPagefileUsage;
            public UIntPtr PrivateUsage;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetProcessTimes(
            IntPtr hProcess,
            out System.Runtime.InteropServices.ComTypes.FILETIME lpCreationTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME lpExitTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME lpKernelTime,
            out System.Runtime.InteropServices.ComTypes.FILETIME lpUserTime);

        [DllImport("psapi.dll", SetLastError = true)]
        public static extern bool GetProcessMemoryInfo(
            IntPtr hProcess,
            out PROCESS_MEMORY_COUNTERS_EX counters,
            uint size);

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

            // 1. Tentativa rapida nativa via Token do Processo
            IntPtr hProc = IntPtr.Zero;
            IntPtr hToken = IntPtr.Zero;
            try
            {
                hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
                if (hProc != IntPtr.Zero && OpenProcessToken(hProc, TOKEN_QUERY, out hToken))
                {
                    using (WindowsIdentity wi = new WindowsIdentity(hToken))
                    {
                        string full = wi.Name;
                        if (!string.IsNullOrEmpty(full))
                        {
                            int slash = full.IndexOf('\\');
                            string res = slash >= 0 ? full.Substring(slash + 1) : full;
                            if (string.Equals(res, "SYSTEM", StringComparison.OrdinalIgnoreCase))
                                return "system";
                            return res;
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

            // 2. Fallback via WMI Win32_Process GetOwner
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT ProcessId FROM Win32_Process WHERE ProcessId = " + pid))
                {
                    foreach (ManagementObject o in s.Get())
                    {
                        object[] outParams = new object[2];
                        int ret = Convert.ToInt32(o.InvokeMethod("GetOwner", outParams));
                        if (ret == 0 && outParams[0] != null && !string.IsNullOrWhiteSpace(outParams[0].ToString()))
                        {
                            string wmiUser = outParams[0].ToString().Trim();
                            int slash = wmiUser.IndexOf('\\');
                            string res = slash >= 0 ? wmiUser.Substring(slash + 1) : wmiUser;
                            if (string.Equals(res, "SYSTEM", StringComparison.OrdinalIgnoreCase))
                                return "system";
                            return res;
                        }
                    }
                }
            }
            catch { }

            // 3. Fallback via tasklist /v /fi "PID eq <pid>" /fo csv /nh
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
                                if (string.Equals(userCol, "N/A", StringComparison.OrdinalIgnoreCase))
                                    return "system";
                                int slash = userCol.IndexOf('\\');
                                string res = slash >= 0 ? userCol.Substring(slash + 1) : userCol;
                                if (string.Equals(res, "SYSTEM", StringComparison.OrdinalIgnoreCase))
                                    return "system";
                                return res;
                            }
                        }
                    }
                }
            }
            catch { }

            return "system";
        }

        public static int GetProcessIdByTcpPort(int port)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "netstat.exe";
                psi.Arguments = "-ano -p tcp";
                psi.RedirectStandardOutput = true;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                using (Process p = Process.Start(psi))
                {
                    if (p != null)
                    {
                        string output = p.StandardOutput.ReadToEnd();
                        p.WaitForExit(1000);
                        string pattern = @":(?<p>" + port + @")\s+.*?LISTENING\s+(?<pid>\d+)";
                        var m = System.Text.RegularExpressions.Regex.Match(output, pattern, System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                        if (m.Success)
                        {
                            int pid;
                            if (int.TryParse(m.Groups["pid"].Value, out pid))
                                return pid;
                        }
                    }
                }
            }
            catch { }
            return 0;
        }

        public static void GetProcessCpuAndRam(int pid, ref TimeSpan lastCpuTime, ref DateTime lastSampleTime, out double cpuPercent, out long ramMb)
        {
            cpuPercent = 0;
            ramMb = 0;
            if (pid <= 0) return;

            IntPtr hProc = IntPtr.Zero;
            try
            {
                uint access = PROCESS_QUERY_LIMITED_INFORMATION | 0x0010 | 0x0400;
                hProc = OpenProcess(access, false, pid);
                if (hProc == IntPtr.Zero)
                    hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);

                if (hProc != IntPtr.Zero)
                {
                    PROCESS_MEMORY_COUNTERS_EX counters;
                    counters.cb = (uint)Marshal.SizeOf(typeof(PROCESS_MEMORY_COUNTERS_EX));
                    if (GetProcessMemoryInfo(hProc, out counters, counters.cb))
                    {
                        ramMb = (long)counters.WorkingSetSize.ToUInt64() / (1024 * 1024);
                    }

                    System.Runtime.InteropServices.ComTypes.FILETIME ftCreate, ftExit, ftKernel, ftUser;
                    if (GetProcessTimes(hProc, out ftCreate, out ftExit, out ftKernel, out ftUser))
                    {
                        long kTicks = (((long)ftKernel.dwHighDateTime) << 32) | (uint)ftKernel.dwLowDateTime;
                        long uTicks = (((long)ftUser.dwHighDateTime) << 32) | (uint)ftUser.dwLowDateTime;
                        TimeSpan totalProcessorTime = TimeSpan.FromTicks(kTicks + uTicks);

                        DateTime now = DateTime.UtcNow;
                        if (lastSampleTime > DateTime.MinValue && now > lastSampleTime)
                        {
                            double elapsedMs = (now - lastSampleTime).TotalMilliseconds;
                            double cpuUsedMs = (totalProcessorTime - lastCpuTime).TotalMilliseconds;
                            if (elapsedMs > 0)
                            {
                                cpuPercent = Math.Round((cpuUsedMs / (elapsedMs * Environment.ProcessorCount)) * 100.0, 1);
                                if (cpuPercent < 0) cpuPercent = 0;
                                if (cpuPercent > 100) cpuPercent = 100;
                            }
                        }

                        lastCpuTime = totalProcessorTime;
                        lastSampleTime = now;
                    }
                }
            }
            catch { }
            finally
            {
                if (hProc != IntPtr.Zero) CloseHandle(hProc);
            }

            if (ramMb == 0)
            {
                try
                {
                    using (Process p = Process.GetProcessById(pid))
                    {
                        if (!p.HasExited)
                        {
                            ramMb = p.WorkingSet64 / (1024 * 1024);
                        }
                    }
                }
                catch { }
            }

            if (lastCpuTime == TimeSpan.Zero)
            {
                try
                {
                    using (Process p = Process.GetProcessById(pid))
                    {
                        if (!p.HasExited)
                        {
                            DateTime now = DateTime.UtcNow;
                            TimeSpan totalProcessorTime = p.TotalProcessorTime;

                            if (lastSampleTime > DateTime.MinValue && now > lastSampleTime)
                            {
                                double elapsedMs = (now - lastSampleTime).TotalMilliseconds;
                                double cpuUsedMs = (totalProcessorTime - lastCpuTime).TotalMilliseconds;
                                if (elapsedMs > 0)
                                {
                                    cpuPercent = Math.Round((cpuUsedMs / (elapsedMs * Environment.ProcessorCount)) * 100.0, 1);
                                    if (cpuPercent < 0) cpuPercent = 0;
                                    if (cpuPercent > 100) cpuPercent = 100;
                                }
                            }

                            lastCpuTime = totalProcessorTime;
                            lastSampleTime = now;
                        }
                    }
                }
                catch { }
            }
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
