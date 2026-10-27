// Statistics.cs - the session statistics (owner 27.10.2026): every run of the app is a session; each site done in it
// (its first bounty to the warp out) and its ISK are kept in %LOCALAPPDATA%\The Norns\sessions\<start>.json, written
// after every site and on close. Only this PC; nothing leaves it. The Statistics page lists this session and the earlier ones.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace TheNorns
{
    class SessionInfo
    {
        public DateTime Start, End;
        public int Sites;
        public long Isk;
        public int Spawns;
        public List<long[]> SiteList = new List<long[]>();   // start ticks, end ticks, ISK
    }

    partial class DpsForm
    {
        readonly SessionInfo session = new SessionInfo { Start = DateTime.Now, End = DateTime.Now };
        List<SessionInfo> sessions;   // the earlier ones, newest first (read when the page opens)
        static string SessionDir { get { return Path.Combine(AppInfo.DataDir, "sessions"); } }
        string SessionFile { get { return Path.Combine(SessionDir, session.Start.ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture) + ".json"); } }

        void StartSessionStats()
        {
            stats.Tracker.SiteClosed += (start, end, isk) =>
            {
                session.Sites++; session.Isk += isk; session.End = DateTime.Now;
                session.SiteList.Add(new[] { start.Ticks, end.Ticks, isk });
                SaveSession();
            };
            FormClosed += (s, e) => SaveSession();
        }

        SessionInfo CurrentSession()
        {
            // the site under way counts too
            var tr = stats.Tracker;
            return new SessionInfo { Start = session.Start, End = DateTime.Now, Sites = session.Sites + (tr.CurrentAnomalyIsk > 0 ? 1 : 0), Isk = session.Isk + tr.CurrentAnomalyIsk, Spawns = stats.Spawns.Count };
        }

        void SaveSession()
        {
            if (SelfTesting || !Standalone || session.Sites == 0) return;
            try
            {
                Directory.CreateDirectory(SessionDir);
                var o = new Dictionary<string, object>
                {
                    { "start", session.Start.ToString("o", CultureInfo.InvariantCulture) },
                    { "end", DateTime.Now.ToString("o", CultureInfo.InvariantCulture) },
                    { "sites", session.Sites }, { "isk", session.Isk }, { "spawns", stats.Spawns.Count },
                    { "site_list", session.SiteList.Select(x => new Dictionary<string, object> { { "start", new DateTime(x[0]).ToString("o", CultureInfo.InvariantCulture) }, { "end", new DateTime(x[1]).ToString("o", CultureInfo.InvariantCulture) }, { "isk", x[2] } }).ToList() },
                };
                File.WriteAllText(SessionFile, new JavaScriptSerializer().Serialize(o));
            }
            catch { }
        }

        public List<SessionInfo> DemoSessions;   // selftest: earlier sessions shown instead of the PC's own

        List<SessionInfo> ReadSessions()
        {
            if (DemoSessions != null) return DemoSessions;
            var res = new List<SessionInfo>();
            if (SelfTesting) return res;
            try
            {
                if (!Directory.Exists(SessionDir)) return res;
                var js = new JavaScriptSerializer();
                foreach (var f in Directory.GetFiles(SessionDir, "*.json").OrderByDescending(x => x))
                {
                    if (string.Equals(f, SessionFile, StringComparison.OrdinalIgnoreCase)) continue;
                    try
                    {
                        var o = js.DeserializeObject(File.ReadAllText(f)) as Dictionary<string, object>;
                        if (o == null) continue;
                        res.Add(new SessionInfo
                        {
                            Start = DateTime.Parse((string)o["start"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                            End = DateTime.Parse((string)o["end"], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind),
                            Sites = Convert.ToInt32(o["sites"], CultureInfo.InvariantCulture),
                            Isk = Convert.ToInt64(o["isk"], CultureInfo.InvariantCulture),
                            Spawns = o.ContainsKey("spawns") ? Convert.ToInt32(o["spawns"], CultureInfo.InvariantCulture) : 0,   // before 1.2: none kept
                        });
                    }
                    catch { }
                }
            }
            catch { }
            return res;
        }
    }
}
