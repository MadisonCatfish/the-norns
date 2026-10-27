// DpsGraph.cs - what the meter reads and computes (the window itself is in Body.cs since the Norns body, 26.10.2026):
//   * GameLogParser / GameLogTail - combat, bounty and fleet-warp lines of the followed clients' logs
//   * AnomalyTracker - ISK per anomaly and per session, every bounty tick of every client
//   * DpsStats - DPS per pilot over the last 60 s, the smartbomb modules of the last cycle, faction detection
//   * MaterialIcon - Google Material icons drawn from their SVG paths
// The log: Documents\EVE\logs\Gamelogs, READ ONLY and only while the app runs (player 24.10.2026). Only the files of the
// configured pilots are followed - a file is taken when its header says "Listener: <pilot>"; nothing is ever written there.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;

namespace TheNorns
{
    class DmgEvent
    {
        public DateTime T;          // UTC (the log's EVE time)
        public string Pilot, Other, Quality, Weapon;
        public bool Out;            // true: the pilot's damage on a target; false: damage taken
        public double Amount;
        public int Kind { get { return GameLogParser.KindOf(Quality); } }   // 0 good, 1 glancing, 2 miss
    }

    class BountyEvent
    {
        public DateTime T;
        public string Pilot;
        public long Amount;
    }

    class WarpFollowEvent
    {
        public DateTime T;
        public string Pilot;
        public string Leader;
    }

    class AnomalyTracker
    {
        public int AnomalyCount;
        public long TotalSessionIsk;
        public long CurrentAnomalyIsk;
        public long LastAnomalyIsk;
        public readonly List<long> Bounties = new List<long>();
        public readonly List<DateTime> BountyTimes = new List<DateTime>();   // when each tick came (the grid colours commander / titan kills)
        public string LastWarpLeader = "";
        public DateTime LastWarpTime = DateTime.MinValue;
        public DateTime PrevWarpTime = DateTime.MinValue;   // the warp before: warp to warp is the anomaly's clear time
        DateTime siteStart = DateTime.MinValue;
        // a site is done (start = its first bounty, end = the warp out, its ISK): the session statistics keep it
        public event Action<DateTime, DateTime, long> SiteClosed;

        // every client in the fleet logs "Following <leader> in warp" for the same warp (the leader's own log does not):
        // the first one ends the anomaly, the others within 10 s are the same warp
        public void OnWarp(string leader, DateTime t)
        {
            if (string.Equals(leader, LastWarpLeader, StringComparison.OrdinalIgnoreCase) && Math.Abs((t - LastWarpTime).TotalSeconds) <= 10) return;
            LastWarpLeader = leader;
            PrevWarpTime = LastWarpTime;
            LastWarpTime = t;
            if (Bounties.Count > 0 || CurrentAnomalyIsk > 0)
            {
                AnomalyCount++;
                LastAnomalyIsk = CurrentAnomalyIsk;
                var h = SiteClosed;
                if (h != null && CurrentAnomalyIsk > 0) h(siteStart == DateTime.MinValue ? t : siteStart, t, CurrentAnomalyIsk);
            }
            CurrentAnomalyIsk = 0;
            Bounties.Clear(); BountyTimes.Clear();
            siteStart = DateTime.MinValue;
        }

        public void OnBounty(long amount, DateTime t, bool isPrimary, int clientCount)
        {
            if (amount <= 0) return;
            // logs are read one client after another: a bounty from before the warp another client already reported
            // belongs to the anomaly just closed
            if (t < LastWarpTime) { TotalSessionIsk += amount; LastAnomalyIsk += amount; return; }
            if (CurrentAnomalyIsk == 0) siteStart = t;
            CurrentAnomalyIsk += amount;
            TotalSessionIsk += amount;
            // every client's tick is a cell of the heat grid (the Norns body, 26.10.2026); isPrimary / clientCount are
            // kept for the callers but no longer fold the fleet into the anomaly source's ticks
            Bounties.Add(amount); BountyTimes.Add(t);
        }

        public void ResetAll()
        {
            AnomalyCount = 0;
            TotalSessionIsk = 0;
            CurrentAnomalyIsk = 0;
            LastAnomalyIsk = 0;
            Bounties.Clear(); BountyTimes.Clear();
            LastWarpLeader = "";
            LastWarpTime = PrevWarpTime = DateTime.MinValue;
        }
    }

    static class GameLogParser
    {
        static readonly Regex LineHead = new Regex(@"^\[\s*(\d{4}\.\d{2}\.\d{2} \d{2}:\d{2}:\d{2})\s*\]\s*\((combat|notify|bounty)\)\s*(.*)$");
        static readonly Regex Tag = new Regex("<[^>]*>");
        static readonly Regex Hit = new Regex(@"^(\d+)\s+(to|from)\s+(.+?)\s+-\s+(.+)$");
        static readonly Regex MissOut = new Regex(@"^Your (?:group of )?.+? misses (.+?) completely", RegexOptions.IgnoreCase);
        static readonly Regex MissIn = new Regex(@"^(.+?) misses you completely", RegexOptions.IgnoreCase);
        static readonly Regex BountyMatch = new Regex(@"^([\d\s\u00A0\.,]+)\s*ISK\s+added to next bounty payout", RegexOptions.IgnoreCase);
        static readonly Regex WarpFollow = new Regex(@"^Following\s+(.+?)\s+in warp", RegexOptions.IgnoreCase);

        // Parses any supported game log line: combat damage (DmgEvent), bounty payout (BountyEvent), or fleet warp follow (WarpFollowEvent)
        public static object ParseLine(string line, string pilot)
        {
            var h = LineHead.Match(line ?? "");
            if (!h.Success) return null;
            DateTime t;
            if (!DateTime.TryParseExact(h.Groups[1].Value, "yyyy.MM.dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out t)) return null;
            string channel = h.Groups[2].Value;
            var text = Regex.Replace(Tag.Replace(h.Groups[3].Value, ""), @"\s+", " ").Trim();

            if (channel == "combat")
            {
                var m = Hit.Match(text);
                if (m.Success)
                {
                    var parts = m.Groups[4].Value.Split(new[] { " - " }, StringSplitOptions.None);
                    string weapon = parts.Length > 1 ? parts[0].Trim() : "";
                    return new DmgEvent { T = t, Pilot = pilot, Out = m.Groups[2].Value == "to", Amount = double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture),
                                          Other = m.Groups[3].Value.Trim(), Quality = parts[parts.Length - 1].Trim(), Weapon = weapon };
                }
                if ((m = MissOut.Match(text)).Success) return new DmgEvent { T = t, Pilot = pilot, Out = true, Amount = 0, Other = m.Groups[1].Value, Quality = "Misses" };
                if ((m = MissIn.Match(text)).Success) return new DmgEvent { T = t, Pilot = pilot, Out = false, Amount = 0, Other = m.Groups[1].Value, Quality = "Misses" };
            }
            else if (channel == "bounty")
            {
                var bm = BountyMatch.Match(text);
                if (bm.Success)
                {
                    // whole ISK with the client's thousands separator ("29 250", "29,250", "29.250"); a separator followed by
                    // exactly two digits at the end is a decimal part ("29,250.00", "29.250,00") and is dropped - groups have three
                    string raw = bm.Groups[1].Value.Trim();
                    raw = Regex.Replace(raw, @"[.,]\d{2}$", "");
                    raw = raw.Replace(" ", "").Replace("\u00A0", "").Replace(",", "").Replace(".", "");
                    long amt;
                    if (long.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out amt))
                    {
                        return new BountyEvent { T = t, Pilot = pilot, Amount = amt };
                    }
                }
            }
            else if (channel == "notify")
            {
                var wm = WarpFollow.Match(text);
                if (wm.Success)
                {
                    return new WarpFollowEvent { T = t, Pilot = pilot, Leader = wm.Groups[1].Value.Trim() };
                }
            }
            return null;   // neutralizer, repairs, warp scrambles, ... - not damage or tracked event
        }

        public static DmgEvent Parse(string line, string pilot)
        {
            return ParseLine(line, pilot) as DmgEvent;
        }

        public static int KindOf(string q)
        {
            q = (q ?? "").ToLowerInvariant();
            if (q.StartsWith("miss")) return 2;
            if (q.StartsWith("glance") || q.StartsWith("graze")) return 1;
            return 0;
        }
    }

    // Follows the newest log file of every configured or auto-discovered pilot from its current end
    class GameLogTail
    {
        // the combat log folder: the one chosen in the settings (config.json "logs": { "dir": ... }), else the default one
        public static string Dir { get { var c = Chosen; return string.IsNullOrEmpty(c) ? Detect() : c; } }
        static string chosen; static bool chosenRead;
        public static string Chosen
        {
            get
            {
                if (!chosenRead)
                {
                    chosenRead = true;
                    try
                    {
                        object l, d;
                        if (File.Exists(AppInfo.ConfigPath) && ConfigStore.Load(AppInfo.ConfigPath).TryGetValue("logs", out l) && l is Dictionary<string, object> && ((Dictionary<string, object>)l).TryGetValue("dir", out d))
                            chosen = d as string;
                    }
                    catch { }
                }
                return chosen;
            }
            set { chosen = value; chosenRead = true; }
        }
        // where EVE writes them: Documents\EVE\logs\Gamelogs, also when Documents is moved to OneDrive
        public static string Detect()
        {
            var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            foreach (var root in new[] { docs, Path.Combine(home, "OneDrive", "Documents"), Path.Combine(home, "Documents") })
            {
                var d = Path.Combine(root, "EVE", "logs", "Gamelogs");
                if (Directory.Exists(d)) return d;
            }
            return Path.Combine(docs, "EVE", "logs", "Gamelogs");
        }
        readonly List<string> pilots;
        public readonly bool AutoDiscover;
        readonly DateTime since = DateTime.UtcNow;
        readonly Dictionary<string, string> fileOf = new Dictionary<string, string>();   // pilot -> file
        readonly Dictionary<string, long> pos = new Dictionary<string, long>();
        readonly Dictionary<string, string> rest = new Dictionary<string, string>();      // a line still being written
        // one decoder per file keeps the bytes of a character cut by the end of a read (Cyrillic, accented names)
        readonly Dictionary<string, Decoder> utf8 = new Dictionary<string, Decoder>();
        readonly HashSet<string> notOurs = new HashSet<string>();                          // headers already read: another character
        DateTime nextScan = DateTime.MinValue;
        public string Status = L.T("waiting for the combat log");
        public bool Following;   // a log of at least one pilot is followed (the header then says "last 60 s")
        string anomalySourcePilot;

        public GameLogTail(IEnumerable<string> pilots, bool autoDiscover = false)
        {
            this.pilots = (pilots ?? Enumerable.Empty<string>()).Where(p => !string.IsNullOrEmpty(p)).ToList();
            AutoDiscover = autoDiscover || this.pilots.Count == 0;
        }

        public List<DmgEvent> Poll(DpsStats stats = null)
        {
            var res = new List<DmgEvent>();
            var fleet = new List<Tuple<DateTime, int, object, string>>();   // (time, 0 bounty / 1 warp, event, pilot)
            if (DateTime.UtcNow >= nextScan && (AutoDiscover || fileOf.Count < pilots.Count)) { nextScan = DateTime.UtcNow.AddSeconds(10); Scan(); }
            if (stats != null)
            {
                foreach (var p in pilots) stats.EnsurePilot(p);
            }
            foreach (var kv in fileOf.ToList())
            {
                try
                {
                    // read only, the client keeps writing: share read / write / delete, never lock
                    using (var fs = new FileStream(kv.Value, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                    {
                        long p = pos[kv.Value];
                        Decoder dec;
                        if (!utf8.TryGetValue(kv.Value, out dec)) utf8[kv.Value] = dec = Encoding.UTF8.GetDecoder();
                        if (fs.Length < p) { p = 0; rest.Remove(kv.Value); dec.Reset(); }   // the file started over
                        if (fs.Length == p) continue;
                        fs.Seek(p, SeekOrigin.Begin);
                        var buf = new byte[fs.Length - p];
                        int n = fs.Read(buf, 0, buf.Length);
                        pos[kv.Value] = p + n;
                        string prev; rest.TryGetValue(kv.Value, out prev);
                        var chars = new char[dec.GetCharCount(buf, 0, n)];
                        int nc = dec.GetChars(buf, 0, n, chars, 0);
                        var text = (prev ?? "") + new string(chars, 0, nc);
                        var lines = text.Split('\n');
                        rest[kv.Value] = lines[lines.Length - 1];
                        for (int i = 0; i < lines.Length - 1; i++)
                        {
                            var obj = GameLogParser.ParseLine(lines[i].TrimEnd('\r'), kv.Key);
                            var e = obj as DmgEvent;
                            if (e != null)
                            {
                                res.Add(e);
                                if (stats != null) stats.EnsurePilot(e.Pilot);
                            }
                            else if (stats != null)
                            {
                                if (anomalySourcePilot == null || !fileOf.ContainsKey(anomalySourcePilot))
                                    anomalySourcePilot = kv.Key;

                                // a new log is read from 16 KB before its end (the DPS graph shows the last 60 s at once);
                                // bounties and warps written before the app started are not part of this session
                                var b = obj as BountyEvent;
                                if (b != null && b.T >= since) fleet.Add(Tuple.Create(b.T, 0, (object)b, kv.Key));
                                var w = obj as WarpFollowEvent;
                                if (w != null && w.T >= since) fleet.Add(Tuple.Create(w.T, 1, (object)w, kv.Key));
                            }
                        }
                    }
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            // bounties and warps of all clients in time order (a bounty in the warp's second still belongs to the site
            // just left): each log is read whole, so one client's later bounty would otherwise come before another's warp
            foreach (var ev in fleet.OrderBy(x => x.Item1).ThenBy(x => x.Item2))
            {
                var b = ev.Item3 as BountyEvent;
                if (b != null) stats.Tracker.OnBounty(b.Amount, b.T, ev.Item4 == anomalySourcePilot, Math.Max(1, fileOf.Count));
                var w = ev.Item3 as WarpFollowEvent;
                if (w != null) stats.Tracker.OnWarp(w.Leader, w.T);   // from any client: the anomaly source may be the leader, whose log has no such line
            }
            return res;
        }

        // the newest files written since the run began; a file is ours when its header names one of the pilots (or any active pilot in auto-discovery)
        void Scan()
        {
            try
            {
                if (AutoDiscover)
                {
                    try
                    {
                        var running = Eve.FindWindows("EVE - ").Select(w => w.Character).Where(c => !string.IsNullOrEmpty(c)).Distinct(StringComparer.OrdinalIgnoreCase);
                        foreach (var p in running)
                            if (!pilots.Contains(p, StringComparer.OrdinalIgnoreCase))
                                pilots.Add(p);
                    }
                    catch { }
                }
                if (!Directory.Exists(Dir)) { Status = L.T("no combat log folder"); return; }
                var files = new DirectoryInfo(Dir).GetFiles("*.txt")
                    .Where(x => x.LastWriteTimeUtc >= since.AddMinutes(-30))
                    .OrderByDescending(x => x.LastWriteTimeUtc);
                foreach (var f in files)
                {
                    if (notOurs.Contains(f.FullName) || fileOf.ContainsValue(f.FullName)) continue;
                    var who = Listener(f.FullName);
                    if (string.IsNullOrEmpty(who)) continue;
                    var pilot = pilots.FirstOrDefault(p => string.Equals(p, who, StringComparison.OrdinalIgnoreCase));
                    if (pilot == null && AutoDiscover)
                    {
                        pilot = who;
                        pilots.Add(pilot);
                    }
                    if (pilot == null) { notOurs.Add(f.FullName); continue; }
                    if (fileOf.ContainsKey(pilot)) continue;   // an older file of the same pilot
                    fileOf[pilot] = f.FullName; pos[f.FullName] = Math.Max(0, f.Length - 16384);
                }
                Status = fileOf.Count == 0 ? L.T("waiting for the combat log") :
                         AutoDiscover ? L.F("{0} pilots", fileOf.Count) :
                         L.F("{0} of {1} pilots", fileOf.Count, pilots.Count);
                Following = fileOf.Count > 0;
            }
            catch (Exception ex) { Status = L.T("combat log:") + " " + ex.Message; }
        }

        // "Listener: Pilot-1" in the first lines of the file
        static string Listener(string path)
        {
            try
            {
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
                {
                    var buf = new byte[Math.Min(1024, (int)fs.Length)];
                    int n = fs.Read(buf, 0, buf.Length);
                    var m = Regex.Match(Encoding.UTF8.GetString(buf, 0, n), @"Listener:\s*(.+?)\s*[\r\n]");
                    return m.Success ? m.Groups[1].Value : null;
                }
            }
            catch { return null; }
        }
    }

    class NpcProfile
    {
        public string Faction;
        public double Em, Thermal, Kinetic, Explosive;
        public NpcProfile(string faction, double em, double th, double ki, double ex)
        {
            Faction = faction; Em = em; Thermal = th; Kinetic = ki; Explosive = ex;
        }
    }

    // the last 60 s per pilot, high-resolution continuous spline
    class DpsStats
    {
        public const int Seconds = 60, N = 121;
        readonly List<DmgEvent> events = new List<DmgEvent>();
        public readonly List<string> Pilots;
        public bool Thundpack;
        public string DetectedFaction;

        // the rare spawns (owner 27.10.2026): an asteroid belt's commander battleship (1) and a titan (2), by their name;
        // every hit to or from one is kept with its time
        readonly List<Tuple<DateTime, int>> special = new List<Tuple<DateTime, int>>();
        // the rare spawns of this run (commanders, faction NPCs, titans): one per NPC name, a name seen again after
        // 10 minutes without it is a new spawn. Kept for the whole run (the session statistics count them)
        public readonly List<Tuple<DateTime, string>> Spawns = new List<Tuple<DateTime, string>>();
        readonly Dictionary<string, DateTime> spawnSeen = new Dictionary<string, DateTime>(StringComparer.OrdinalIgnoreCase);
        static readonly Regex TitanName = new Regex(@"\bTitan\b", RegexOptions.IgnoreCase), CommanderName = new Regex(@"\bCommander\b|^(Shadow Serpentis|Dread Guristas|Dark Blood|True Sansha|Domination)\b", RegexOptions.IgnoreCase);   // + faction spawns
        public static int SpecialKind(string npc)
        {
            if (string.IsNullOrEmpty(npc)) return 0;
            if (TitanName.IsMatch(npc)) return 2;
            return CommanderName.IsMatch(npc) ? 1 : 0;
        }
        // a bounty tick at t is the commander's / titan's when one was being hit in the 4 s before it (titan wins)
        public int KindNear(DateTime t)
        {
            int k = 0;
            foreach (var x in special) if (x.Item1 <= t.AddSeconds(1) && x.Item1 >= t.AddSeconds(-4)) k = Math.Max(k, x.Item2);
            return k;
        }
        // the last hit to or from a titan
        public DateTime LastTitan { get { var t = DateTime.MinValue; foreach (var x in special) if (x.Item2 == 2 && x.Item1 > t) t = x.Item1; return t; } }
        public void DemoSpecial(DateTime t, int kind) { special.Add(Tuple.Create(t, kind)); }
        public readonly AnomalyTracker Tracker = new AnomalyTracker();
        // outgoing DPS counts only this weapon (the Norns body shows "EFF. DPS · VORTON PROJECTOR"); null = every weapon
        public string OutWeapon = "Vorton";
        public DpsStats(IEnumerable<string> pilots) { Pilots = (pilots ?? Enumerable.Empty<string>()).ToList(); }

        // NPC resistance profiles from Pyfa / Reddit: [name],[EM %],[Thermal %],[Kinetic %],[Explosive %].
        // Not used for the numbers shown: the game log already reports damage after resists (Series / Now sum it as is).
        // Kept with GetResistFactor for later use (review 26.10.2026); only IdentifyFaction feeds the faction badge.
        public static readonly Dictionary<string, NpcProfile> Profiles = new Dictionary<string, NpcProfile>(StringComparer.OrdinalIgnoreCase)
        {
            { "Serpentis", new NpcProfile("Serpentis", 47.0, 38.0, 29.0, 56.0) },
            { "Angel Cartel", new NpcProfile("Angel Cartel", 57.0, 48.0, 39.0, 30.0) },
            { "Blood Raiders", new NpcProfile("Blood Raiders", 28.0, 38.0, 47.0, 56.0) },
            { "Guristas", new NpcProfile("Guristas", 57.0, 38.0, 29.0, 48.0) },
            { "Sansha", new NpcProfile("Sansha", 28.0, 37.0, 47.0, 56.0) },
            { "Rogue Drones", new NpcProfile("Rogue Drones", 33.0, 39.0, 47.0, 55.0) },
            { "Sleepers", new NpcProfile("Sleepers", 30.0, 30.0, 30.0, 30.0) },
            { "Drifters", new NpcProfile("Drifters", 30.0, 30.0, 30.0, 30.0) },
            { "Triglavian", new NpcProfile("Triglavian", 35.0, 35.0, 35.0, 35.0) },
            { "Amarr", new NpcProfile("Amarr", 26.0, 34.0, 43.0, 51.0) },
            { "Caldari", new NpcProfile("Caldari", 54.0, 37.0, 29.0, 46.0) },
            { "Gallente", new NpcProfile("Gallente", 45.0, 36.0, 28.0, 54.0) },
            { "Minmatar", new NpcProfile("Minmatar", 55.0, 45.0, 38.0, 29.0) },
            { "CONCORD", new NpcProfile("CONCORD", 42.0, 41.0, 42.0, 42.0) },
            { "Mordu", new NpcProfile("Mordu", 42.0, 35.0, 32.0, 35.0) },
            { "Khanid", new NpcProfile("Khanid", 26.0, 29.0, 37.0, 42.0) },
            { "Thukker", new NpcProfile("Thukker", 30.0, 27.0, 32.0, 31.0) }
        };

        public static string IdentifyFaction(string target)
        {
            if (string.IsNullOrWhiteSpace(target)) return null;
            string t = target.Trim();

            // Serpentis: Core, Coreli, Corelior, Corelum, Corelatis, Shadow Serpentis, Serpentis
            if (Regex.IsMatch(t, @"\b(Core|Coreli|Corelior|Corelum|Corelatis|Serpentis)\b", RegexOptions.IgnoreCase))
                return "Serpentis";

            // Guristas: Pith, Pithi, Pithior, Pithum, Pithatis, Dread Guristas, Guristas
            if (Regex.IsMatch(t, @"\b(Pith|Pithi|Pithior|Pithum|Pithatis|Guristas)\b", RegexOptions.IgnoreCase))
                return "Guristas";

            // Angel Cartel: Gist, Gistii, Gistior, Gistum, Gistatis, Arch Gist, Domination, Angel
            if (Regex.IsMatch(t, @"\b(Gist|Gistii|Gistior|Gistum|Gistatis|Domination|Angel)\b", RegexOptions.IgnoreCase))
                return "Angel Cartel";

            // Blood Raiders: Corpus, Corpii, Corpior, Corpum, Corpatis, Dark Blood, Blood Raider
            if (Regex.IsMatch(t, @"\b(Corpus|Corpii|Corpior|Corpum|Corpatis|Blood)\b", RegexOptions.IgnoreCase))
                return "Blood Raiders";

            // Sansha's Nation: Centus, Centii, Centior, Centum, Centatis, True Sansha, Sansha, Kuvakei
            if (Regex.IsMatch(t, @"\b(Centus|Centii|Centior|Centum|Centatis|Sansha|Kuvakei)\b", RegexOptions.IgnoreCase))
                return "Sansha";

            // Rogue Drones: Alvus, Alvi, Alvior, Alvum, Alvatis, Sentient, Drone, Decimator, Infester, Render, Splinter, Silverfish, Barracuda
            if (Regex.IsMatch(t, @"\b(Alvus|Alvi|Alvior|Alvum|Alvatis|Sentient|Drone|Decimator|Infester|Render|Splinter|Silverfish|Barracuda)\b", RegexOptions.IgnoreCase))
                return "Rogue Drones";

            // Sleepers: Sleepless, Awakened, Emergent, Sleeper
            if (Regex.IsMatch(t, @"\b(Sleepless|Awakened|Emergent|Sleeper)\b", RegexOptions.IgnoreCase))
                return "Sleepers";

            // Drifters: Drifter, Apollo, Artemis, Tyrannos, Autothysian
            if (Regex.IsMatch(t, @"\b(Drifter|Apollo|Artemis|Tyrannos|Autothysian)\b", RegexOptions.IgnoreCase))
                return "Drifters";

            // Triglavian: Triglavian, Zorya, Veles, Dazh, Perun, Svarog
            if (Regex.IsMatch(t, @"\b(Triglavian|Zorya|Veles|Dazh|Perun|Svarog)\b", RegexOptions.IgnoreCase))
                return "Triglavian";

            // Empire / other factions
            if (Regex.IsMatch(t, @"\b(Amarr|Imperial)\b", RegexOptions.IgnoreCase)) return "Amarr";
            if (Regex.IsMatch(t, @"\b(Caldari|State|Taibu)\b", RegexOptions.IgnoreCase)) return "Caldari";
            if (Regex.IsMatch(t, @"\b(Gallente|Federation)\b", RegexOptions.IgnoreCase)) return "Gallente";
            if (Regex.IsMatch(t, @"\b(Minmatar|Republic)\b", RegexOptions.IgnoreCase)) return "Minmatar";
            if (Regex.IsMatch(t, @"\b(CONCORD|Police)\b", RegexOptions.IgnoreCase)) return "CONCORD";
            if (Regex.IsMatch(t, @"\b(Mordu)\b", RegexOptions.IgnoreCase)) return "Mordu";

            return null;
        }

        public double GetResistFactor(string weapon, string target)
        {
            var faction = DetectedFaction ?? IdentifyFaction(target);
            if (string.IsNullOrEmpty(faction)) return 0;

            NpcProfile prof;
            if (!Profiles.TryGetValue(faction, out prof)) return 0;

            weapon = weapon ?? "";

            // Vorton Projector (EM + Kinetic)
            if (weapon.IndexOf("Vorton", StringComparison.OrdinalIgnoreCase) >= 0)
                return (prof.Em + prof.Kinetic) / 200.0;

            // Smartbombs & weapons with single damage types:
            if (weapon.IndexOf("Graviton", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("EMP", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("Proton", StringComparison.OrdinalIgnoreCase) >= 0)
                return prof.Em / 100.0;

            if (weapon.IndexOf("Plasma", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("Phase", StringComparison.OrdinalIgnoreCase) >= 0)
                return prof.Thermal / 100.0;

            if (weapon.IndexOf("Fusion", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("Explosive", StringComparison.OrdinalIgnoreCase) >= 0)
                return prof.Explosive / 100.0;

            if (weapon.IndexOf("Neutron", StringComparison.OrdinalIgnoreCase) >= 0)
                return prof.Kinetic / 100.0;

            // Triglavian Entropic Disintegrator (Thermal + Explosive)
            if (weapon.IndexOf("Entropic", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("Disintegrator", StringComparison.OrdinalIgnoreCase) >= 0)
                return (prof.Thermal + prof.Explosive) / 200.0;

            // Lasers (EM + Thermal)
            if (weapon.IndexOf("Laser", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("Beam", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("Pulse", StringComparison.OrdinalIgnoreCase) >= 0)
                return (prof.Em + prof.Thermal) / 200.0;

            // Hybrids (Kinetic + Thermal)
            if (weapon.IndexOf("Railgun", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("Blaster", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("Hybrid", StringComparison.OrdinalIgnoreCase) >= 0)
                return (prof.Kinetic + prof.Thermal) / 200.0;

            // Projectiles (Explosive + Kinetic)
            if (weapon.IndexOf("Cannon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("Autocannon", StringComparison.OrdinalIgnoreCase) >= 0 ||
                weapon.IndexOf("Artillery", StringComparison.OrdinalIgnoreCase) >= 0)
                return (prof.Explosive + prof.Kinetic) / 200.0;

            // Default: average of all 4 resistances for this faction
            return (prof.Em + prof.Thermal + prof.Kinetic + prof.Explosive) / 400.0;
        }

        static readonly Regex PlayerName = new Regex(@"^[^\[\]()]+(\[[^\[\]()]*\])?\([^()]+\)$");
        public static bool IsPlayerOrFriendly(string name, IEnumerable<string> knownPilots = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            // Player targets in EVE combat logs have: "Character[TICKER](ShipType)" or "Character(ShipType)" or "Drone / Character".
            // The ship in brackets closes the name: an NPC with brackets inside its name stays an NPC (review 26.10.2026)
            if (PlayerName.IsMatch(name) || name.Contains(" / ")) return true;
            if (knownPilots != null)
            {
                foreach (var p in knownPilots)
                {
                    if (string.IsNullOrEmpty(p)) continue;
                    if (name.Equals(p, StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith(p + "[", StringComparison.OrdinalIgnoreCase) ||
                        name.StartsWith(p + "(", StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            return false;
        }

        public void EnsurePilot(string pilot)
        {
            if (!string.IsNullOrEmpty(pilot) && !Pilots.Contains(pilot, StringComparer.OrdinalIgnoreCase))
                Pilots.Add(pilot);
        }

        public void Add(IEnumerable<DmgEvent> es, DateTime now)
        {
            foreach (var e in es)
            {
                events.Add(e);
                if (!IsPlayerOrFriendly(e.Other, Pilots))
                {
                    int sk = SpecialKind(e.Other);
                    if (sk != 0)
                    {
                        special.Add(Tuple.Create(e.T, sk));
                        DateTime seen;
                        if (!spawnSeen.TryGetValue(e.Other, out seen) || (e.T - seen).TotalMinutes > 10) Spawns.Add(Tuple.Create(e.T, e.Other));
                        if (!spawnSeen.ContainsKey(e.Other) || e.T > spawnSeen[e.Other]) spawnSeen[e.Other] = e.T;
                    }
                    var f = IdentifyFaction(e.Other);
                    if (!string.IsNullOrEmpty(f)) DetectedFaction = f;
                }
            }
            var cut = now.AddSeconds(-Seconds - 10);
            events.RemoveAll(e => e.T < cut);
            special.RemoveAll(x => x.Item1 < now.AddMinutes(-40));
            version++;
        }

        // the window redraws at 25 fps (Thundpack, marquees, lerps) but the events change once a second: the events of one
        // pilot and direction are picked once per change (in time order) instead of on every frame (review 26.10.2026)
        int version, cacheVersion = -1, cachePilots; bool cacheThund;
        readonly Dictionary<string, List<DmgEvent>> picked = new Dictionary<string, List<DmgEvent>>();
        List<DmgEvent> Picked(string pilot, bool outgoing, string weapon = null)
        {
            if (cacheVersion != version || cacheThund != Thundpack || cachePilots != Pilots.Count)
            {
                picked.Clear(); cacheVersion = version; cacheThund = Thundpack; cachePilots = Pilots.Count;
            }
            var key = (pilot ?? "\0") + (outgoing ? "\0out" : "\0in") + "\0" + weapon;
            var w = weapon ?? OutWeapon;
            List<DmgEvent> l;
            if (!picked.TryGetValue(key, out l))
            {
                l = events.Where(x => (pilot == null || x.Pilot == pilot) && x.Out == outgoing && (!Thundpack || !IsPlayerOrFriendly(x.Other, Pilots))
                                     && (!outgoing || w == null || (x.Weapon ?? "").IndexOf(w, StringComparison.OrdinalIgnoreCase) >= 0))
                          .OrderBy(x => x.T).ToList();   // stable: a log's own order is kept, so the sums stay the same
                picked[key] = l;
            }
            return l;
        }

        // DPS per second over the last 60 s, continuous Hann-smoothed kernel (Variant B: direct effective combat log HP)
        public double[] Series(string pilot, bool outgoing, DateTime now, string weapon = null)
        {
            var s = new double[N];
            const double W = 3.0; // Hann kernel radius in seconds
            var matching = Picked(pilot, outgoing, weapon);
            if (matching.Count == 0) return s;

            int lo = 0;   // the samples go forward in time: only the events within ±W of a sample are visited
            for (int j = 0; j < N; j++)
            {
                double u = (double)j / (N - 1);
                var tj = now.AddSeconds(-(1.0 - u) * Seconds);
                while (lo < matching.Count && (tj - matching[lo].T).TotalSeconds > W) lo++;
                double sum = 0;
                for (int i = lo; i < matching.Count; i++)
                {
                    var e = matching[i];
                    if ((e.T - tj).TotalSeconds > W) break;
                    double dt = Math.Abs((tj - e.T).TotalSeconds);
                    if (dt <= W)
                    {
                        double w = 0.5 * (1.0 + Math.Cos(Math.PI * dt / W)) / W;
                        sum += e.Amount * w;
                    }
                }
                s[j] = sum;
            }
            return s;
        }

        // DPS of the last 10 s (Variant B: direct effective combat log HP)
        public double Now(string pilot, bool outgoing, DateTime now)
        {
            return Picked(pilot, outgoing).Where(x => (now - x.T).TotalSeconds <= 10).Sum(x => x.Amount) / 10;
        }

        // ---- smartbombs: one reading per module, for the cycle that just went off ----
        // A smartbomb hits every NPC in range once per cycle, so in the log a cycle is a burst of hits a few seconds
        // apart from the next burst (more than 6 s), and an NPC name hit 7, 14 or 21 times in a cycle means 1, 2 or 3
        // NPCs of that name x 7 smartbombs: the number of modules is the GCD of those counts in the first cycle seen.
        // The k-th hit on an NPC within a cycle is module k (mod n); a module's bar is its damage this cycle against its
        // best cycle so far, its DPS the damage over the cycle period (median gap between cycle starts, 9 s by default).
        public class SmartbombReading { public string Pilot; public int Modules; public double[] Dps, Frac; public double Total; }
        readonly Dictionary<string, int> sbModules = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, double[]> sbBest = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, double> sbPeriod = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        int sbVersion = -1; SmartbombReading sbCache;

        // the last damage any pilot put on an NPC (friendly fire left out): the power display slows down after a fleet
        // warp with none since
        int lastNpcVersion = -1; DateTime lastNpcHit = DateTime.MinValue;
        public DateTime LastNpcHit
        {
            get
            {
                if (lastNpcVersion != version)
                {
                    lastNpcVersion = version;
                    foreach (var e in events)
                        if (e.Out && e.Amount > 0 && e.T > lastNpcHit && !IsPlayerOrFriendly(e.Other, Pilots)) lastNpcHit = e.T;
                }
                return lastNpcHit;
            }
        }

        public static bool IsSmartbomb(DmgEvent e) { return (e.Weapon ?? "").IndexOf("Smartbomb", StringComparison.OrdinalIgnoreCase) >= 0; }

        public SmartbombReading Smartbombs(DateTime now)
        {
            if (sbVersion == version && sbCache != null) return sbCache;
            sbVersion = version;
            var all = events.Where(e => e.Out && e.Amount > 0 && IsSmartbomb(e) && !IsPlayerOrFriendly(e.Other, Pilots)).OrderBy(e => e.T).ToList();
            if (all.Count == 0) return sbCache = null;
            string pilot = all[all.Count - 1].Pilot;   // the ship whose smartbombs went off last
            var mine = all.Where(e => e.Pilot == pilot).ToList();
            var cycles = new List<List<DmgEvent>> { new List<DmgEvent> { mine[0] } };
            for (int i = 1; i < mine.Count; i++)
            {
                if ((mine[i].T - mine[i - 1].T).TotalSeconds > 6) cycles.Add(new List<DmgEvent>());
                cycles[cycles.Count - 1].Add(mine[i]);
            }
            // decided once per site, from the cycles after the fleet warp (owner, 26.10.2026). NPCs of one name add up
            // (2 of them -> 14 or 28 hits for 7 modules) and a ship fits at most 8 high slots: a cycle's vote is the
            // largest divisor of its GCD that is 8 or less. A cycle cut by NPCs dying mid-volley votes low (3, 6 ...)
            // or not at all (GCD under 3), so the count is the first value two complete cycles agree on; until then the
            // most voted (then the larger) value is shown. Owner's log 21:23: votes 3, 7, 7 -> 7.
            if (sbWarp != Tracker.LastWarpTime) { sbWarp = Tracker.LastWarpTime; sbModules.Clear(); sbBest.Clear(); }
            int n;
            if (!sbModules.TryGetValue(pilot, out n))
            {
                var votes = new Dictionary<int, int>();
                for (int i = 0; i < cycles.Count; i++)
                {
                    bool complete = i < cycles.Count - 1 || (now - cycles[i][cycles[i].Count - 1].T).TotalSeconds > 6;
                    if (cycles[i][0].T < sbWarp || !complete) continue;
                    int gcd = cycles[i].GroupBy(e => e.Other).Select(gr => gr.Count()).Aggregate(0, Gcd);
                    if (gcd < 3) continue;
                    int v = ModulesOf(gcd), c;
                    votes.TryGetValue(v, out c); votes[v] = ++c;
                    if (c >= 2) { sbModules[pilot] = n = v; break; }
                }
                if (n == 0)
                    n = votes.Count > 0 ? votes.OrderByDescending(kv => kv.Value).ThenByDescending(kv => kv.Key).First().Key
                                        : ModulesOf(Math.Max(1, cycles[cycles.Count - 1].GroupBy(e => e.Other).Select(gr => gr.Count()).Aggregate(0, Gcd)));
            }
            if (cycles.Count > 1)
            {
                var gaps = new List<double>();
                for (int i = 1; i < cycles.Count; i++) gaps.Add((cycles[i][0].T - cycles[i - 1][0].T).TotalSeconds);
                gaps.Sort();
                sbPeriod[pilot] = Math.Max(1, gaps[gaps.Count / 2]);
            }
            double period; if (!sbPeriod.TryGetValue(pilot, out period)) period = 9;
            Func<List<DmgEvent>, double[]> perModule = cycle =>
            {
                var d = new double[n];
                var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var e in cycle)
                {
                    int k; seen.TryGetValue(e.Other, out k); seen[e.Other] = k + 1;
                    d[k % n] += e.Amount;
                }
                return d;
            };
            double[] best;
            if (!sbBest.TryGetValue(pilot, out best) || best.Length != n) sbBest[pilot] = best = new double[n];
            foreach (var cycle in cycles) { var d = perModule(cycle); for (int k = 0; k < n; k++) best[k] = Math.Max(best[k], d[k]); }
            var last = cycles[cycles.Count - 1];
            // older than two periods, or from before the fleet warp (the site just left): the smartbombs are quiet now
            var dmg = (now - last[last.Count - 1].T).TotalSeconds <= period * 2 + 1 && last[last.Count - 1].T >= sbWarp ? perModule(last) : new double[n];
            var r = new SmartbombReading { Pilot = pilot, Modules = n, Dps = new double[n], Frac = new double[n] };
            for (int k = 0; k < n; k++)
            {
                r.Dps[k] = dmg[k] / period;
                r.Frac[k] = best[k] > 0 ? dmg[k] / best[k] : 0;
                r.Total += r.Dps[k];
            }
            return sbCache = r;
        }

        static int Gcd(int a, int b) { while (b != 0) { int t = a % b; a = b; b = t; } return a; }
        static int ModulesOf(int gcd) { for (int d = Math.Min(8, gcd); d >= 1; d--) if (gcd % d == 0) return d; return 1; }
        DateTime sbWarp = DateTime.MinValue;

        // shots of the active window (10 s in Thundpack mode, 60 s in standard mode): good, glancing, missed
        public int[] Hits(string pilot, DateTime now)
        {
            var r = new int[3];
            int window = Thundpack ? 10 : Seconds;
            foreach (var e in Picked(pilot, true).Where(x => (now - x.T).TotalSeconds <= window))
                r[e.Kind]++;
            return r;
        }
    }

    // Google Material icons (canonical 24x24 vector paths)
    static class MaterialIcon
    {
        public const string Refresh = "M17.65 6.35C16.2 4.9 14.21 4 12 4c-4.42 0-7.99 3.58-7.99 8s3.57 8 7.99 8c3.73 0 6.84-2.55 7.73-6h-2.08c-.82 2.33-3.04 4-5.65 4-3.31 0-6-2.69-6-6s2.69-6 6-6c1.66 0 3.14.69 4.22 1.78L13 11h7V4l-2.35 2.35z";
        public const string Description = "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z";
        // damage types on the damage display (Material Icons: bolt, local_fire_department, trip_origin, flare)
        public const string Bolt = "M11 21h-1l1-7H7.5c-.58 0-.57-.32-.38-.66.19-.34.05-.08.07-.12C8.48 10.94 10.42 7.54 13 3h1l-1 7h3.5c.49 0 .56.33.47.51l-.07.15C12.96 17.55 11 21 11 21z";
        public const string Fire = "M12 12.9l-2.13 2.09c-.56.56-.87 1.29-.87 2.07C9 18.68 10.35 20 12 20s3-1.32 3-2.94c0-.78-.31-1.52-.87-2.07L12 12.9zM16 6l-.44.55C14.38 8.02 12 7.19 12 5.3V2S4 6 4 13c0 2.92 1.56 5.47 3.89 6.86-.56-.79-.89-1.76-.89-2.8 0-1.32.52-2.56 1.47-3.5L12 10.1l3.53 3.47c.95.93 1.47 2.17 1.47 3.5 0 1.02-.31 1.96-.85 2.75 1.89-1.15 3.29-3.06 3.71-5.3.66-3.55-1.07-6.9-3.86-8.52z";
        public const string TripOrigin = "M2 12C2 6.48 6.48 2 12 2s10 4.48 10 10-4.48 10-10 10S2 17.52 2 12zm10 6c3.31 0 6-2.69 6-6s-2.69-6-6-6-6 2.69-6 6 2.69 6 6 6z";
        public const string Flare = "M7 11H1v2h6v-2zm2.17-3.24L7.05 5.64 5.64 7.05l2.12 2.12 1.41-1.41zM13 1h-2v6h2V1zm5.36 6.05l-1.41-1.41-2.12 2.12 1.41 1.41 2.12-2.12zM17 11v2h6v-2h-6zm-5-2c-1.66 0-3 1.34-3 3s1.34 3 3 3 3-1.34 3-3-1.34-3-3-3zm2.83 7.24l2.12 2.12 1.41-1.41-2.12-2.12-1.41 1.41zm-9.19.71l1.41 1.41 2.12-2.12-1.41-1.41-2.12 2.12zM11 23h2v-6h-2v6z";
        public const string Settings = "M19.14 12.94c.04-.3.06-.61.06-.94 0-.32-.02-.64-.07-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.05.3-.09.63-.09.94s.02.64.07.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z";
        public const string Check = "M9 16.17L4.83 12l-1.42 1.41L9 19 21 7l-1.41-1.41z";
        public const string Folder = "M10 4H4c-1.1 0-1.99.9-1.99 2L2 18c0 1.1.9 2 2 2h16c1.1 0 2-.9 2-2V8c0-1.1-.9-2-2-2h-8l-2-2z";
        public const string Search = "M15.5 14h-.79l-.28-.27C15.41 12.59 16 11.11 16 9.5 16 5.91 13.09 3 9.5 3S3 5.91 3 9.5 5.91 16 9.5 16c1.61 0 3.09-.59 4.23-1.57l.27.28v.79l5 4.99L20.49 19l-4.99-5zm-6 0C7.01 14 5 11.99 5 9.5S7.01 5 9.5 5 14 7.01 14 9.5 11.99 14 9.5 14z";
        public const string Star = "M12 17.27L18.18 21l-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z";
        public const string Close = "M19 6.41L17.59 5 12 10.59 6.41 5 5 6.41 10.59 12 5 17.59 6.41 19 12 13.41 17.59 19 19 17.59 13.41 12z";

        static readonly Regex TokenRegex = new Regex(@"([a-zA-Z]|[-+]?(?:\d*\.\d+|\d+)(?:[eE][-+]?\d+)?)", RegexOptions.Compiled);

        // Material Symbols (fonts.google.com/icons) use a 960 viewBox from y -960: CreatePath(svg, target, -960, 960)
        public const string Function = "M400-240v-80h62l105-120-105-120h-66l-64 344q-8 45-37 70.5T221-120q-45 0-73-24t-28-64q0-32 17-51.5t43-19.5q25 0 42.5 17t17.5 41q0 5-.5 9t-1.5 9q5-1 8.5-5.5T252-221l62-339H200v-80h129l21-114q7-38 37.5-62t72.5-24q44 0 72 26t28 65q0 30-17 49.5T500-680q-25 0-42.5-17T440-739q0-5 .5-9t1.5-9q-6 2-9 6t-5 12l-17 99h189v80h-32l52 59 52-59h-32v-80h200v80h-62L673-440l105 120h62v80H640v-80h32l-52-60-52 60h32v80H400Z";

        public static GraphicsPath CreatePath(string svg, RectangleF target) { return CreatePath(svg, target, 0, 24); }

        public static GraphicsPath CreatePath(string svg, RectangleF target, float vbY, float vbSize)
        {
            var path = new GraphicsPath();
            var matches = TokenRegex.Matches(svg ?? "");
            int i = 0;
            float curX = 0, curY = 0, startX = 0, startY = 0;
            float lastX2 = 0, lastY2 = 0, lastQX = 0, lastQY = 0;
            bool lastWasCurve = false, lastWasQuad = false;
            char cmd = 'M';

            Func<float> nextFloat = () =>
            {
                if (i >= matches.Count) return 0f;
                float v;
                float.TryParse(matches[i++].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
                return v;
            };

            while (i < matches.Count)
            {
                string tok = matches[i].Value;
                if (char.IsLetter(tok[0]))
                {
                    cmd = tok[0];
                    i++;
                }

                switch (cmd)
                {
                    case 'M':
                        curX = nextFloat(); curY = nextFloat();
                        startX = curX; startY = curY;
                        path.StartFigure();
                        cmd = 'L';
                        lastWasCurve = false;
                        break;
                    case 'm':
                        curX += nextFloat(); curY += nextFloat();
                        startX = curX; startY = curY;
                        path.StartFigure();
                        cmd = 'l';
                        lastWasCurve = false;
                        break;
                    case 'L':
                        {
                            float x = nextFloat(), y = nextFloat();
                            path.AddLine(curX, curY, x, y);
                            curX = x; curY = y;
                            lastWasCurve = false;
                            break;
                        }
                    case 'l':
                        {
                            float x = curX + nextFloat(), y = curY + nextFloat();
                            path.AddLine(curX, curY, x, y);
                            curX = x; curY = y;
                            lastWasCurve = false;
                            break;
                        }
                    case 'H':
                        {
                            float x = nextFloat();
                            path.AddLine(curX, curY, x, curY);
                            curX = x;
                            lastWasCurve = false;
                            break;
                        }
                    case 'h':
                        {
                            float x = curX + nextFloat();
                            path.AddLine(curX, curY, x, curY);
                            curX = x;
                            lastWasCurve = false;
                            break;
                        }
                    case 'V':
                        {
                            float y = nextFloat();
                            path.AddLine(curX, curY, curX, y);
                            curY = y;
                            lastWasCurve = false;
                            break;
                        }
                    case 'v':
                        {
                            float y = curY + nextFloat();
                            path.AddLine(curX, curY, curX, y);
                            curY = y;
                            lastWasCurve = false;
                            break;
                        }
                    case 'C':
                        {
                            float x1 = nextFloat(), y1 = nextFloat();
                            float x2 = nextFloat(), y2 = nextFloat();
                            float x = nextFloat(), y = nextFloat();
                            path.AddBezier(curX, curY, x1, y1, x2, y2, x, y);
                            lastX2 = x2; lastY2 = y2; lastWasCurve = true;
                            curX = x; curY = y;
                            break;
                        }
                    case 'c':
                        {
                            float x1 = curX + nextFloat(), y1 = curY + nextFloat();
                            float x2 = curX + nextFloat(), y2 = curY + nextFloat();
                            float x = curX + nextFloat(), y = curY + nextFloat();
                            path.AddBezier(curX, curY, x1, y1, x2, y2, x, y);
                            lastX2 = x2; lastY2 = y2; lastWasCurve = true;
                            curX = x; curY = y;
                            break;
                        }
                    case 'S':
                        {
                            float x1 = lastWasCurve ? 2 * curX - lastX2 : curX;
                            float y1 = lastWasCurve ? 2 * curY - lastY2 : curY;
                            float x2 = nextFloat(), y2 = nextFloat();
                            float x = nextFloat(), y = nextFloat();
                            path.AddBezier(curX, curY, x1, y1, x2, y2, x, y);
                            lastX2 = x2; lastY2 = y2; lastWasCurve = true;
                            curX = x; curY = y;
                            break;
                        }
                    case 's':
                        {
                            float x1 = lastWasCurve ? 2 * curX - lastX2 : curX;
                            float y1 = lastWasCurve ? 2 * curY - lastY2 : curY;
                            float x2 = curX + nextFloat(), y2 = curY + nextFloat();
                            float x = curX + nextFloat(), y = curY + nextFloat();
                            path.AddBezier(curX, curY, x1, y1, x2, y2, x, y);
                            lastX2 = x2; lastY2 = y2; lastWasCurve = true;
                            curX = x; curY = y;
                            break;
                        }
                    case 'Q':
                    case 'q':
                    case 'T':
                    case 't':
                        {
                            // quadratic Bezier (Material Symbols paths), drawn as the equivalent cubic
                            bool rel = cmd == 'q' || cmd == 't', smooth = cmd == 'T' || cmd == 't';
                            float qx, qy;
                            if (smooth) { qx = lastWasQuad ? 2 * curX - lastQX : curX; qy = lastWasQuad ? 2 * curY - lastQY : curY; }
                            else { qx = nextFloat(); qy = nextFloat(); if (rel) { qx += curX; qy += curY; } }
                            float x = nextFloat(), y = nextFloat();
                            if (rel) { x += curX; y += curY; }
                            path.AddBezier(curX, curY, curX + 2f / 3 * (qx - curX), curY + 2f / 3 * (qy - curY), x + 2f / 3 * (qx - x), y + 2f / 3 * (qy - y), x, y);
                            lastQX = qx; lastQY = qy; lastWasCurve = false;
                            curX = x; curY = y;
                            lastWasQuad = true;
                            continue;
                        }
                    case 'Z':
                    case 'z':
                        path.CloseFigure();
                        curX = startX; curY = startY;
                        lastWasCurve = false;
                        break;
                    default:
                        i++;
                        break;
                }
                lastWasQuad = false;   // any other command ends a run of quadratic curves (T reflects only after Q / T)
            }

            if (target.Width > 0 && target.Height > 0)
            {
                using (var m = new Matrix())
                {
                    float scale = Math.Min(target.Width / vbSize, target.Height / vbSize);
                    float offX = target.Left + (target.Width - vbSize * scale) / 2f;
                    float offY = target.Top + (target.Height - vbSize * scale) / 2f;
                    m.Translate(offX, offY);
                    m.Scale(scale, scale);
                    m.Translate(0, -vbY);
                    path.Transform(m);
                }
            }
            return path;
        }

        public static void DrawSymbol(Graphics g, string svg, RectangleF target, Brush brush)
        {
            using (var path = CreatePath(svg, target, -960, 960)) g.FillPath(brush, path);
        }

        public static void Draw(Graphics g, string svg, RectangleF target, Brush brush)
        {
            using (var path = CreatePath(svg, target))
            {
                g.FillPath(brush, path);
            }
        }
    }

}
