// Core.cs - what the DPS meter needs from outside its own window: the app's name, version and folders, config.json
// reading / writing, the list of open EVE client windows (titles only) and the pilot colours.
// The Norns only reads the game's combat logs and window titles; it never sends input to the game.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

[assembly: System.Reflection.AssemblyTitle("The Norns")]
[assembly: System.Reflection.AssemblyProduct("The Norns")]
[assembly: System.Reflection.AssemblyDescription("A DPS meter for EVE Online from the combat log. Not affiliated with CCP hf.")]
[assembly: System.Reflection.AssemblyVersion("1.2.0.0")]
[assembly: System.Reflection.AssemblyFileVersion("1.2.0.0")]
[assembly: System.Reflection.AssemblyInformationalVersion("1.2.0")]

namespace TheNorns
{
    static class AppInfo
    {
        public const string Name = "The Norns";   // the app's name everywhere the user sees it
        public const string Version = "1.2.0";
        // semantic versioning MAJOR.MINOR.PATCH; Channel "" = a stable release, "beta" / "rc.1" for previews
        public const string Channel = "";
        public static string VersionText { get { return Version + (Channel != "" ? " " + Channel : ""); } }
        public static string Title { get { return Name + " " + VersionText; } }

        // per-user data: config.json, the Thundpack switch, crash reports -> %LOCALAPPDATA%\The Norns
        public static string DataDir { get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Name); } }
        public static string ConfigPath { get { return Path.Combine(DataDir, "config.json"); } }

        // the icon embedded in the exe - for the window and the taskbar
        static Icon icon;
        public static Icon Icon
        {
            get
            {
                if (icon == null) try { icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch { }
                return icon;
            }
        }
    }

    // config.json: plain JSON, key order kept, the last version saved next to it as config.json.bak
    static class ConfigStore
    {
        public static Dictionary<string, object> Load(string path)
        {
            var js = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            return (Dictionary<string, object>)js.DeserializeObject(File.ReadAllText(path, Encoding.UTF8).TrimStart('﻿'));
        }

        public static void Save(string path, Dictionary<string, object> root)
        {
            var sb = new StringBuilder();
            Write(sb, root, 0);
            sb.Append('\n');
            var tmp = path + ".tmp";
            File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
            if (File.Exists(path)) File.Copy(path, path + ".bak", true);
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }

        public static Dictionary<string, object> Obj(Dictionary<string, object> d, string key)
        {
            object v;
            if (d.TryGetValue(key, out v) && v is Dictionary<string, object>) return (Dictionary<string, object>)v;
            var n = new Dictionary<string, object>(); d[key] = n; return n;
        }

        static void Write(StringBuilder sb, object v, int ind)
        {
            string pad = new string(' ', ind + 2), end = new string(' ', ind);
            if (v == null) sb.Append("null");
            else if (v is string) Str(sb, (string)v);
            else if (v is bool) sb.Append((bool)v ? "true" : "false");
            else if (v is int || v is long) sb.Append(Convert.ToString(v, CultureInfo.InvariantCulture));
            else if (v is decimal || v is double || v is float) sb.Append(Convert.ToDouble(v).ToString("R", CultureInfo.InvariantCulture));
            else if (v is IDictionary<string, object>)
            {
                var d = (IDictionary<string, object>)v;
                if (d.Count == 0) { sb.Append("{}"); return; }
                sb.Append("{\n"); int i = 0;
                foreach (var kv in d)
                {
                    sb.Append(pad); Str(sb, kv.Key); sb.Append(": "); Write(sb, kv.Value, ind + 2);
                    sb.Append(++i < d.Count ? ",\n" : "\n");
                }
                sb.Append(end).Append('}');
            }
            else if (v is IEnumerable)
            {
                var items = ((IEnumerable)v).Cast<object>().ToList();
                if (items.Count == 0) { sb.Append("[]"); return; }
                sb.Append("[\n");
                for (int i = 0; i < items.Count; i++) { sb.Append(pad); Write(sb, items[i], ind + 2); sb.Append(i + 1 < items.Count ? ",\n" : "\n"); }
                sb.Append(end).Append(']');
            }
            else Str(sb, v.ToString());
        }

        static void Str(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (var ch in s)
            {
                switch (ch)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: if (ch < 0x20) sb.AppendFormat("\\u{0:x4}", (int)ch); else sb.Append(ch); break;
                }
            }
            sb.Append('"');
        }
    }

    class EveWindow
    {
        public string Character;
    }

    // the open EVE clients by their window title "EVE - <character>" - titles only, nothing is read from the game
    static class Eve
    {
        delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);

        public static List<EveWindow> FindWindows(string prefix)
        {
            var list = new List<EveWindow>();
            EnumWindows((h, l) =>
            {
                if (!IsWindowVisible(h)) return true;
                var sb = new StringBuilder(256);
                GetWindowText(h, sb, 256);
                var t = sb.ToString();
                if (t.StartsWith(prefix)) list.Add(new EveWindow { Character = t.Substring(prefix.Length) });
                return true;
            }, IntPtr.Zero);
            return list;
        }
    }

    // one colour per pilot, in the order the pilots appear
    static class Pilots
    {
        public static readonly Color[] Colors =
        {
            Theme.Mint, Theme.Blue, Theme.Pink, Theme.Yellow, Color.FromArgb(168, 120, 240),
            Color.FromArgb(251, 146, 60), Color.FromArgb(45, 212, 191), Color.FromArgb(244, 63, 94)
        };
    }
}
