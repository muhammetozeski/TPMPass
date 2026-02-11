using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SecureTPMVault
{
    // Using LibraryImport (requires the containing class to be partial)
    static partial class SecurityScanner
    {
        const uint TH32CS_SNAPPROCESS = 0x00000002;

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
        /// Finds the calling parent process and scans it using Windows Defender.
        /// Returns TRUE if malware is found, FALSE otherwise.
        /// </summary>
        public static bool ScanCallerProcess()
        {
            try
            {
                int myPid = Environment.ProcessId;
                int parentPid = GetParentProcessId(myPid);

                if (parentPid == 0) return false; // Parent process could not be found

                using Process parentProc = Process.GetProcessById(parentPid);
                string? parentPath = parentProc.MainModule?.FileName;

                if (string.IsNullOrWhiteSpace(parentPath)) return false;

                // System processes (Explorer, CMD, etc.) are often protected by real-time scanning.
                // We skip them to avoid unnecessary scanning overhead.
                string procName = parentProc.ProcessName.ToLowerInvariant();
                if (procName is "explorer" or "cmd" or "powershell" or "pwsh" or "windowsterminal")
                {
                    // These are system shells; Defender already protects them in real-time.
                    return false;
                }

                // STARTING DEFENDER SCAN
                return RunDefenderScan(parentPath);
            }
            catch (Exception ex)
            {
                // If scan fails, log the error and continue without blocking execution.
                Log($"[SCAN WARNING] Could not scan parent process: {ex.Message}", ConsoleColor.DarkYellow);
                return false;
            }
        }

        static bool RunDefenderScan(string filePath)
        {
            // Path to the Windows Defender command-line utility
            string defenderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                                                   "Windows Defender", "MpCmdRun.exe");

            if (!File.Exists(defenderPath))
            {
                Log("[SCAN INFO] Windows Defender not found. Skipping scan.", ConsoleColor.DarkGray);
                return false;
            }

            Log($"[SECURITY] Scanning caller process: {Path.GetFileName(filePath)}...", ConsoleColor.Cyan, WriteToDisk: false);

            ProcessStartInfo psi = new()
            {
                FileName = defenderPath,
                // -ScanType 3 = File/Custom Scan
                Arguments = $"-Scan -ScanType 3 -File \"{filePath}\" -DisableRemediation",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true
            };

            using var p = Process.Start(psi);
            if (p == null) return false;

            p.WaitForExit();

            // MpCmdRun Exit Codes:
            // 0 = Clean / No Threat Found
            // 2 = Threat Detected
            if (p.ExitCode == 2)
            {
                Log($"[SECURITY ALERT] MALWARE DETECTED IN CALLER PROCESS! ID: {Path.GetFileName(filePath)}", ConsoleColor.Red);
                return true; // THREAT DETECTED
            }

            Log("[SECURITY] Caller process is clean.", ConsoleColor.Green, WriteToDisk: false);
            return false;
        }

        static int GetParentProcessId(int myPid)
        {
            IntPtr snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
            if (snapshot == IntPtr.Zero) return 0;

            // Fixed CA2263: Using generic Marshal.SizeOf<T>() for cleaner code
            PROCESSENTRY32 procEntry = new() { dwSize = (uint)Marshal.SizeOf<PROCESSENTRY32>() };

            if (Process32First(snapshot, ref procEntry))
            {
                do
                {
                    if (procEntry.th32ProcessID == myPid)
                    {
                        return (int)procEntry.th32ParentProcessID;
                    }
                } while (Process32Next(snapshot, ref procEntry));
            }

            return 0;
        }
    }
}