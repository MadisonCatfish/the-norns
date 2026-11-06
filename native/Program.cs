// Program.cs - The Norns: an EVE Online DPS meter that only reads the combat logs.
//   TheNorns.exe                      the DPS window (one instance)
//   TheNorns.exe --selftest-dps <dir> the parser test and renders of the window as PNGs
//   TheNorns.exe --demo-anim <dir> [scene]  frames of the site's short recordings (Anim.cs)
//   --lang <code>                                one language for this run (en, uk, es, de, it, fr)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace TheNorns
{
    static class Program
    {
        [DllImport("shcore.dll")] static extern int SetProcessDpiAwareness(int value);

        [STAThread]
        static void Main(string[] args)
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) => Crash(e.Exception);
            AppDomain.CurrentDomain.UnhandledException += (s, e) => Crash(e.ExceptionObject as Exception);
            try { SetProcessDpiAwareness(2); } catch { }
            // "1 234" / "3.1" regardless of the Windows locale
            Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            // language: config.json "app": { "language": "de" }, else the Windows language, else English; --lang wins
            try { L.Init(L.FromConfig(File.Exists(AppInfo.ConfigPath) ? ConfigStore.Load(AppInfo.ConfigPath) : null)); } catch { L.Init(L.SystemDefault()); }
            int lang = Array.IndexOf(args, "--lang");
            if (lang >= 0 && args.Length > lang + 1) L.Init(args[lang + 1]);
            L.AutoFit();

            int sdps = Array.IndexOf(args, "--selftest-dps");
            if (sdps >= 0 && args.Length > sdps + 1) { DpsForm.SelfTest(args[sdps + 1]); return; }
            // --demo-anim <dir> [scene]: the site's short recordings, frames as PNGs (Anim.cs)
            // --scan-logs <sessions dir>: "Read logs" (LogScan.cs) into that folder, the result on the console
            int scan = Array.IndexOf(args, "--scan-logs");
            if (scan >= 0 && args.Length > scan + 1) { Console.WriteLine(DpsForm.ScanLogsInto(args[scan + 1])); return; }
            // --bench <file>: frame times (the body, the Display page cold and warm) written to the file
            int bench = Array.IndexOf(args, "--bench");
            if (bench >= 0 && args.Length > bench + 1) { DpsForm.Bench(args[bench + 1]); return; }
            int anim = Array.IndexOf(args, "--demo-anim");
            if (anim >= 0 && args.Length > anim + 1) { DpsForm.Anim(args[anim + 1], args.Length > anim + 2 ? args[anim + 2] : null); return; }

            bool first;
            using (var one = new Mutex(true, "TheNorns.Instance", out first))
            {
                // --preview-no-clients / --preview-update / --preview-rewarps / --preview-dread: to look at those states with
                // clients open, next to a running copy
                // --fleet-trace <file>: the client module (Fleet.cs) on for this run, every change of the clients written to the file
                int ft = Array.IndexOf(args, "--fleet-trace");
                if (ft >= 0 && args.Length > ft + 1) Fleet.TracePath = Path.GetFullPath(args[ft + 1]);
                bool preview = Array.IndexOf(args, "--preview-no-clients") >= 0 || Array.IndexOf(args, "--preview-update") >= 0 || Array.IndexOf(args, "--preview-rewarps") >= 0 || Array.IndexOf(args, "--preview-dread") >= 0 || Array.IndexOf(args, "--preview-competition") >= 0 || Fleet.TracePath != null;
                if (!first && !preview) return;   // already open
                Application.EnableVisualStyles();
                if (preview) DpsForm.SelfTesting = true;   // the preview keeps no position and no session statistics
                if (preview && Array.IndexOf(args, "--potato") >= 0) DpsForm.Potato = true;   // a preview in Potato mode
                if (preview && Array.IndexOf(args, "--compact") >= 0) DpsForm.Compact = true;   // a preview in Compact mode
                var f = new DpsForm(new string[0], () => null, true);
                if (Array.IndexOf(args, "--preview-no-clients") >= 0) f.DemoNoClients = true;
                if (Array.IndexOf(args, "--preview-rewarps") >= 0) f.StartReplay();
                if (Array.IndexOf(args, "--preview-dread") >= 0) f.StartDreadDemo();
                if (Array.IndexOf(args, "--preview-competition") >= 0) f.StartRodDemo();
                // --preview-update <version>: shows a newer version as if the start check had found it (no network)
                int pu = Array.IndexOf(args, "--preview-update");
                if (pu >= 0 && args.Length > pu + 1) { DpsForm.UpdateAtStart = false; f.DemoUpdate(args[pu + 1]); }
                f.ShowDps();
                Application.Run(f);
            }
        }

        static void Crash(Exception ex)
        {
            try
            {
                Directory.CreateDirectory(AppInfo.DataDir);
                File.WriteAllText(Path.Combine(AppInfo.DataDir, "crash_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt"), AppInfo.Title + Environment.NewLine + ex);
            }
            catch { }
        }
    }
}
