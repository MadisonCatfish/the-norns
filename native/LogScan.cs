// LogScan.cs - "Read logs" on the Statistics page (owner, 05.11.2026): reads the game's combat logs (read-only, as the
// meter does) for faction NPCs and commanders (★), titans and pirate dreadnoughts, and adds them to the saved sessions
// they fall in. No session is made: a spawn outside every saved session is left out. Each count only goes up (a session
// already holds what the meter saw live; the logs can only add what it missed, e.g. before 1.5.0 kept titans).
// A spawn is counted once however many clients saw it: the same name again within 10 minutes is the same spawn.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace TheNorns
{
    partial class DpsForm
    {
        bool scanning;
        string scanNote; double scanNoteUntil;

        void ReadLogsIntoSessions()
        {
            if (scanning) return;
            scanning = true; scanNote = L.T("Reading logs…"); scanNoteUntil = double.MaxValue; Render();
            string dir = GameLogTail.Dir, skip = SessionFile;
            new System.Threading.Thread(() =>
            {
                string note;
                try { note = ScanLogs(dir, SessionDir, skip); }
                catch (Exception ex) { note = L.T("Could not read the logs") + ": " + ex.Message; }
                try { BeginInvoke((Action)(() => { scanning = false; sessions = null; scanNote = note; scanNoteUntil = Clock + 8; Render(); })); } catch { }
            }) { IsBackground = true }.Start();
        }

        // --scan-logs <sessions dir>: the same into another folder (a copy), for checking without the page
        public static string ScanLogsInto(string sessionDir) { return ScanLogs(GameLogTail.Dir, sessionDir, null); }

        // the note for the page: how many logs were read and what was added
        static string ScanLogs(string logDir, string sessionDir, string skipFile)
        {
            if (!Directory.Exists(logDir)) return L.T("No combat logs found");
            // every rare NPC hit or hitting, from every log, in time order
            var seen = new List<Tuple<DateTime, string, int>>();
            int files = 0;
            foreach (var f in Directory.GetFiles(logDir, "*.txt"))
            {
                files++;
                try
                {
                    using (var fs = new FileStream(f, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    using (var r = new StreamReader(fs, Encoding.UTF8))
                    {
                        string line;
                        while ((line = r.ReadLine()) != null)
                        {
                            if (line.IndexOf("(combat)", StringComparison.Ordinal) < 0) continue;
                            var e = GameLogParser.Parse(line, "");
                            if (e == null || string.IsNullOrEmpty(e.Other) || DpsStats.IsPlayerOrFriendly(e.Other)) continue;
                            int sk = DpsStats.SpecialKind(e.Other);
                            if (sk == 0) continue;
                            if (DpsStats.IsDread(e.Other)) sk = 3;
                            seen.Add(Tuple.Create(e.T, e.Other, sk));
                        }
                    }
                }
                catch { }   // a log the game holds in a way we can't share: skipped
            }
            // spawns: a name seen again within 10 minutes is the same one (as the meter counts them live)
            var last = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
            var spawns = new List<Tuple<DateTime, int>>();
            foreach (var s in seen.OrderBy(x => x.Item1))
            {
                DateTime prev;
                if (!last.TryGetValue(s.Item2, out prev) || (s.Item1 - prev).TotalMinutes > 10) spawns.Add(Tuple.Create(s.Item1.ToLocalTime(), s.Item3));
                last[s.Item2] = s.Item1;
            }
            if (!Directory.Exists(sessionDir)) return L.F("{0} logs read: no saved sessions", files);

            int updated = 0, addStars = 0, addTitans = 0, addDreads = 0;
            var js = new JavaScriptSerializer();
            foreach (var path in Directory.GetFiles(sessionDir, "*.json"))
            {
                if (string.Equals(path, skipFile, StringComparison.OrdinalIgnoreCase)) continue;   // this run's own session: counted live
                try
                {
                    var o = js.DeserializeObject(File.ReadAllText(path)) as Dictionary<string, object>;
                    if (o == null) continue;
                    var start = DateTime.Parse((string)o["start"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime();
                    var end = DateTime.Parse((string)o["end"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToLocalTime();
                    // a minute of grace each side: the session's ends are when the meter saved it
                    var inside = spawns.Where(x => x.Item1 >= start.AddMinutes(-1) && x.Item1 <= end.AddMinutes(1)).ToList();
                    if (inside.Count == 0) continue;
                    int stars = inside.Count(x => x.Item2 == 1), titans = inside.Count(x => x.Item2 == 2), dreads = inside.Count(x => x.Item2 == 3);
                    Func<string, int> had = key => o.ContainsKey(key) ? Convert.ToInt32(o[key], CultureInfo.InvariantCulture) : 0;
                    int s0 = had("spawns"), t0 = had("titans"), d0 = had("dreads");
                    if (stars <= s0 && titans <= t0 && dreads <= d0) continue;
                    o["spawns"] = Math.Max(s0, stars); o["titans"] = Math.Max(t0, titans); o["dreads"] = Math.Max(d0, dreads);
                    if (!o.ContainsKey("format") || Convert.ToInt32(o["format"], CultureInfo.InvariantCulture) < SessionFormat) o["format"] = SessionFormat;
                    File.WriteAllText(path, js.Serialize(o));
                    updated++; addStars += Math.Max(0, stars - s0); addTitans += Math.Max(0, titans - t0); addDreads += Math.Max(0, dreads - d0);
                }
                catch { }
            }
            if (updated == 0) return L.F("{0} logs read: nothing new", files);
            return L.F("{0} logs: {1} sessions, +{2} ★ +{3} titans +{4} dreads", files, updated, addStars, addTitans, addDreads);
        }
    }
}
