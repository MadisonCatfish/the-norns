// Program.cs - The Norns: an EVE Online DPS meter that only reads the combat logs.
//   TheNorns.exe                      the DPS window (one instance)
//   TheNorns.exe --selftest-dps <dir> the parser test and renders of the window as PNGs
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

            bool first;
            using (var one = new Mutex(true, "TheNorns.Instance", out first))
            {
                if (!first) return;   // already open
                Application.EnableVisualStyles();
                var f = new DpsForm(new string[0], () => null, true);
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
