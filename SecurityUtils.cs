using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SecureTPMVault;

/// <summary>
/// Provides low-level security functions using modern Source Generated P/Invokes.
/// </summary>
internal static partial class SecurityUtils
{
    private const uint SEM_NOGPFAULTERRORBOX = 0x0002;
    private const uint TH32CS_SNAPPROCESS = 0x00000002;

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool IsDebuggerPresent();

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool CheckRemoteDebuggerPresent(IntPtr hProcess, [MarshalAs(UnmanagedType.Bool)] ref bool isDebuggerPresent);

    [LibraryImport("kernel32.dll")]
    private static partial uint SetErrorMode(uint uMode);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    public static partial IntPtr CreateToolhelp32Snapshot(uint dwFlags, uint th32ProcessID);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Process32First(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [LibraryImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool Process32Next(IntPtr hSnapshot, ref PROCESSENTRY32 lppe);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private unsafe struct PROCESSENTRY32
    {
        public uint dwSize;
        public uint cntUsage;
        public uint th32ProcessID;
        public IntPtr th32DefaultHeapID;
        public uint th32ModuleID;
        public uint cntThreads;
        public uint th32ParentProcessID;
        public int pcPriClassBase;
        public uint dwFlags;
        public fixed char szExeFile[260];
    }

    /// <summary>
    /// Prevents Windows from generating a crash dump file.
    /// </summary>
    public static void DisableCrashDumps() => _ = SetErrorMode(SEM_NOGPFAULTERRORBOX);

    /// <summary>
    /// Checks if a debugger is attached to the process.
    /// </summary>
    /// <returns>True if a debugger is detected, otherwise false.</returns>
    public static bool IsDebuggerAttached()
    {
        if (IsDebuggerPresent() || Debugger.IsAttached) return true;

        bool isRemoteDebugger = false;
        try
        {
            CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref isRemoteDebugger);
        }
        catch { /* Best effort check */ }

        return isRemoteDebugger;
    }

    /// <summary>
    /// Identifies and scans the parent process using Windows Defender.
    /// </summary>
    /// <returns>True if the parent process is flagged as a threat.</returns>
    public static bool ScanCallerProcess()
    {
        try
        {
            int parentPid = GetParentProcessId(Environment.ProcessId);
            if (parentPid == 0) return false;

            using Process parentProc = Process.GetProcessById(parentPid);
            string? parentPath = parentProc.MainModule?.FileName;

            if (string.IsNullOrWhiteSpace(parentPath)) return false;

            string procName = parentProc.ProcessName.ToLowerInvariant();

            if (procName is "explorer" or "cmd" or "powershell" or "pwsh" or "windowsterminal" or "devenv")
                return false;

            return RunDefenderScan(parentPath);
        }
        catch
        {
            return false;
        }
    }

    private static int GetParentProcessId(int myPid)
    {
        IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
        if (snapshot == IntPtr.Zero) return 0;

        PROCESSENTRY32 procEntry = new()
        {
            dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>()
        };

        try
        {
            if (Process32First(snapshot, ref procEntry))
            {
                do
                {
                    if (procEntry.th32ProcessID == myPid)
                        return (int)procEntry.th32ParentProcessID;
                } while (Process32Next(snapshot, ref procEntry));
            }
        }
        finally
        {
            // Snapshot handle is cleaned up by OS eventually
        }
        return 0;
    }

    private static bool RunDefenderScan(string filePath)
    {
        string defenderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                           "Windows Defender", "MpCmdRun.exe");

        if (!File.Exists(defenderPath)) return false;

        using Process? p = Process.Start(new ProcessStartInfo
        {
            FileName = defenderPath,
            Arguments = $"-Scan -ScanType 3 -File \"{filePath}\" -DisableRemediation",
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (p == null) return false;
        p.WaitForExit();

        return p.ExitCode == 2;
    }
}