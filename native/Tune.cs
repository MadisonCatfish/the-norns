// Tune.cs - the tuning registry (owner, 20.11.2026): every number the meter's logic works with, by name, with its
// default, its limits and a line on what it does. The code reads Tune.X.V instead of a literal; the Tuning window
// (TuneWindow.cs) changes the values live.
// Gating: the values are read from %LOCALAPPDATA%\The Norns\tuning.json only when tuning is on - the --tune switch or a
// test build (build.ps1 -Test). A release build without --tune, --selftest-dps, --demo-anim and --bench always run on
// the defaults, so a file left behind never changes a release, a self-test render or the site's recordings.
// The file is checked once a second from the log poll (UI thread); a file that does not parse leaves the values as they are.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

namespace TheNorns
{
    static class Tune
    {
        public sealed class P
        {
            public string Key, Group, Label, Unit, Help;
            public double Def, Min, Max, Step;
            public bool Int, NewEventsOnly;
            public double V;
            public bool Changed { get { return Math.Abs(V - Def) > 1e-9; } }
            public int I { get { return (int)Math.Round(V); } }
        }

        static readonly List<P> all = new List<P>();
        public static IList<P> All { get { return all; } }
        public static IEnumerable<string> Groups { get { return all.Select(p => p.Group).Distinct(); } }
        // goes up on every change: the meter's caches compare it (DpsStats)
        public static int Version;
        public static bool Enabled;
        public static bool AnyChanged { get { return all.Any(p => p.Changed); } }

        static P D(string group, string key, string label, string unit, double def, double min, double max, double step, string help,
                   bool isInt = false, bool newOnly = false)
        {
            var p = new P { Group = group, Key = key, Label = label, Unit = unit, Def = def, Min = min, Max = max, Step = step, Help = help,
                            Int = isInt, NewEventsOnly = newOnly, V = def };
            all.Add(p);
            return p;
        }

        // ---- Log ----
        public static readonly P LogPollMs = D("Log", "log.poll_ms", "Log reading", "ms", 1000, 200, 5000, 50,
            "How often The Norns reads new lines from your combat logs. Lower = numbers react faster but use a bit more CPU. Default: once a second.", true);
        public static readonly P LogNewFileKb = D("Log", "log.new_file_tail_kb", "New character: read back", "KB", 16, 0, 256, 1,
            "When you log in another character mid-session, The Norns reads this much of the end of its log, so its DPS line is full at once.", true, true);
        public static readonly P LogRescanS = D("Log", "log.rescan_s", "Look for new characters", "s", 10, 2, 60, 1,
            "How often The Norns checks the Gamelogs folder for a log of a new character: a relog or one more client.", true);
        public static readonly P LogRecentMin = D("Log", "log.recent_min", "Fresh log age", "min", 30, 1, 240, 1,
            "At start, a combat log written within this time counts as the character's current one. Older logs are from past sessions and are skipped.", true, true);

        // ---- Vorton projector ----
        public static readonly P VortonKernelS = D("Vorton projector", "vorton.kernel_s", "Line smoothing", "s", 3, 0.5, 10, 0.1,
            "How smooth the DPS lines are: every Vorton hit is spread over this many seconds each side. Higher = calmer lines that show a change later.");
        public static readonly P VortonNowS = D("Vorton projector", "vorton.now_s", "DPS number window", "s", 10, 2, 60, 1,
            "The big DPS numbers average the damage over this many last seconds. Shorter = jumpy numbers, longer = steady numbers.", true);

        // ---- Smartbombs ----
        public static readonly P SbCycleGapS = D("Smartbombs", "sb.cycle_gap_s", "Pause between volleys", "s", 6, 1, 20, 0.1,
            "Smartbombs fire in volleys. If no smartbomb hit lands for this long, the next hits start a new volley. Raise it if one volley splits in two.");
        public static readonly P SbMinGcd = D("Smartbombs", "sb.min_gcd", "Smallest volley that counts", "", 3, 1, 8, 1,
            "Every rat in range takes one hit from each smartbomb, so a volley tells how many you fit. A volley cut by dying rats (fewer hits than this) is not counted.", true);
        public static readonly P SbMaxSlots = D("Smartbombs", "sb.max_slots", "Max smartbombs fitted", "", 8, 1, 8, 1,
            "The most smartbombs a ship can fit (its high slots). The Norns never counts more than this.", true);
        public static readonly P SbVotesToLock = D("Smartbombs", "sb.votes_to_lock", "Volleys to agree", "", 2, 1, 6, 1,
            "How many full volleys must show the same number of smartbombs before The Norns fixes that number for the site.", true);
        public static readonly P SbDefaultPeriodS = D("Smartbombs", "sb.default_period_s", "Default cycle time", "s", 9, 2, 30, 0.1,
            "Your smartbomb cycle time until The Norns measures it from two volleys. Smartbomb DPS is a volley's damage divided by this time.");
        public static readonly P SbQuietPeriods = D("Smartbombs", "sb.quiet_periods", "Bars drop after", "periods", 2, 1, 6, 0.1,
            "After the last volley the bars stay up for this many cycles (+1 s), then drop to zero: the smartbombs are off or the site is clear.");

        // ---- Anomaly timer ----
        public static readonly P WarpDedupeS = D("Anomaly timer", "warp.dedupe_s", "One fleet warp within", "s", 10, 1, 60, 1,
            "Every fleet member's log writes \"Following <leader> in warp\" for the same warp. Lines within this time count as one warp, not several sites.", true, true);
        public static readonly P IdleGapS = D("Anomaly timer", "warp.idle_gap_s", "Break between warps", "s", 120, 10, 900, 5,
            "A warp with no shooting since the last warp is a re-warp: the site was taken. If the gap is longer than this, you docked or took a break, and no timer shows.", true);
        public static readonly P NoClientAfterS = D("Anomaly timer", "warp.no_client_after_s", "No client after", "s", 5, 1, 60, 1,
            "If no EVE client is open for this long (longer than a relog), the timer goes out and the graph says no client is running.", true);
        public static readonly P CountUpS = D("Anomaly timer", "timer.count_up_s", "Clear time count-up", "s", 1.4, 0.1, 5, 0.05,
            "After the fleet warps out, the site's clear time counts up from 00:00 in this many seconds.");
        public static readonly P LinesDownS = D("Anomaly timer", "timer.lines_down_s", "DPS lines drop", "s", 0.7, 0.05, 3, 0.05,
            "When the fleet warps out, the Vorton DPS lines drop down in this time to make room for the clear time.");
        public static readonly P TimerInS = D("Anomaly timer", "timer.in_s", "Clear time fades in", "s", 0.6, 0.05, 3, 0.05,
            "Then the clear time fades in, in this time.");
        public static readonly P TimerOutS = D("Anomaly timer", "timer.out_s", "Clear time fades out", "s", 0.15, 0.02, 3, 0.01,
            "The first shot on the next site hides the clear time in this time.");
        public static readonly P LinesBackS = D("Anomaly timer", "timer.lines_back_s", "DPS lines rise back", "s", 0.6, 0.05, 3, 0.05,
            "Then the DPS lines rise back in this time.");
        public static readonly P WarpingAfterS = D("Anomaly timer", "warp.warping_after_s", "Power display slows after", "s", 8, 0, 60, 1,
            "If the fleet warped and nobody has shot since, after this long the small power display slows down: the fleet is between sites.", true);

        // ---- ISK grid ----
        public static readonly P IskHeatMin = D("ISK grid", "isk.heat_min", "Coldest bounty", "ISK", 3000, 100, 100000, 100,
            "Bounty ticks this small or smaller (frigates, cruisers) show almost black on the ISK grid.", true);
        public static readonly P IskHeatMax = D("ISK grid", "isk.heat_max", "Hottest bounty", "ISK", 270000, 10000, 5000000, 1000,
            "Bounty ticks this big or bigger (battleships, commanders) show in full yellow. Between the two the colour scale is logarithmic.", true);
        public static readonly P IskNeighbourShare = D("ISK grid", "isk.neighbour_share", "Heat from neighbours", "", 0.4, 0, 1, 0.01,
            "How much each dot takes from the 8 dots around it, so the grid reads as a heat map. 0 = every tick on its own.");
        public static readonly P IskDiagonalWeight = D("ISK grid", "isk.diagonal_weight", "Corner neighbours", "", 0.5, 0, 1, 0.05,
            "How much the corner dots count against the side dots (1) when the dots share heat.");
        public static readonly P IskSpawnTickX = D("ISK grid", "isk.spawn_tick_x", "Spawn bounty size", "x median", 4, 1, 20, 0.5,
            "A bounty at least this many times the site's typical tick, paid right after you hit a faction NPC, titan or dread, is that spawn's bounty and gets its own colour.");
        public static readonly P IskPendingS = D("ISK grid", "isk.pending_s", "Unpaid spawn shown for", "s", 90, 10, 600, 5,
            "A rare spawn (faction, titan, dread) you hit in this time, whose bounty has not come yet, shows as a marked dot after the last tick.", true);
        public static readonly P IskPendingMax = D("ISK grid", "isk.pending_max", "Unpaid spawns at most", "", 3, 0, 10, 1,
            "How many such unpaid rare spawns the grid shows at once.", true);

        // ---- Alerts ----
        public static readonly P OnGridS = D("Alerts", "alert.on_grid_s", "On grid for", "s", 20, 2, 120, 1,
            "A titan or a dread counts as on grid while it shot you or you shot it within this time. Meanwhile the hull glows and the tray icon flashes.", true);
        public static readonly P HeatRiseTau = D("Alerts", "alert.heat_rise_tau", "Hull glows up", "τ s", 0.35, 0.02, 5, 0.01,
            "How fast the hull glows red (titan) or yellow (dread) when one lands. Full glow after about 3× this time.");
        public static readonly P HeatFallTau = D("Alerts", "alert.heat_fall_tau", "Hull cools down", "τ s", 3.5, 0.1, 30, 0.1,
            "How fast the glow fades after the titan or dread is gone.");
        public static readonly P SpawnSameMin = D("Alerts", "alert.spawn_same_min", "Same spawn within", "min", 10, 1, 120, 1,
            "The same NPC name seen again within this time is the same spawn, not a new one. Read logs always uses the default.", true, true);
        public static readonly P KindNearBeforeS = D("Alerts", "alert.kind_near_before_s", "Bounty after the hit", "s", 4, 0, 30, 0.5,
            "A bounty tick that comes up to this long after you hit a rare NPC belongs to that NPC.");
        public static readonly P SoundRecentS = D("Alerts", "alert.sound_recent_s", "Alert sound only for", "s", 60, 5, 600, 5,
            "The alert sound plays only for a spawn this fresh. Old spawns that The Norns reads from the log at start stay silent.", true);

        // ---- Lightning rod ----
        public static readonly P RodWindowS = D("Lightning rod", "rod.window_s", "Number window", "s", 10, 2, 60, 1,
            "The Lightning rod numbers (damage through the runner, damage the runner takes) average over this many last seconds.", true);

        // ---- the file ----
        public static string FileOverride;   // selftest: a file in the self-test folder, never the user's
        public static string FilePath { get { return FileOverride ?? Path.Combine(AppInfo.DataDir, "tuning.json"); } }
        static DateTime fileTime = DateTime.MinValue;
        public static string LastError;

        public static P Find(string key) { return all.FirstOrDefault(p => p.Key == key); }

        // --tune or a test build: read tuning.json now and on every change
        public static void Init(bool enabled)
        {
            Enabled = enabled;
            if (enabled) Load(true);
        }

        public static void Poll()
        {
            if (!Enabled) return;
            DateTime t;
            try { t = File.Exists(FilePath) ? File.GetLastWriteTimeUtc(FilePath) : DateTime.MinValue; } catch { return; }
            if (t != fileTime) Load(false);
        }

        internal static void Load(bool first)
        {
            try
            {
                if (!File.Exists(FilePath)) { fileTime = DateTime.MinValue; return; }
                var t = File.GetLastWriteTimeUtc(FilePath);
                var d = new JavaScriptSerializer().DeserializeObject(File.ReadAllText(FilePath, Encoding.UTF8)) as Dictionary<string, object>;
                if (d == null) throw new FormatException("not a JSON object");
                var read = new double[all.Count];   // all of it parsed first: a bad value leaves every value as it was
                for (int i = 0; i < all.Count; i++)
                {
                    object o;
                    double v = all[i].Def;
                    if (d.TryGetValue(all[i].Key, out o) && o != null) v = Convert.ToDouble(o, CultureInfo.InvariantCulture);
                    read[i] = Clamp(all[i], v);
                }
                for (int i = 0; i < all.Count; i++) all[i].V = read[i];
                fileTime = t;   // only after a good read: a half-written file is tried again at the next poll
                LastError = null;
                Version++;
            }
            catch (Exception ex) { LastError = "tuning.json: " + ex.Message; }
        }

        static double Clamp(P p, double v)
        {
            if (double.IsNaN(v) || double.IsInfinity(v)) v = p.Def;
            v = Math.Max(p.Min, Math.Min(p.Max, v));
            return p.Int ? Math.Round(v) : v;
        }

        public static void Set(P p, double v)
        {
            v = Clamp(p, v);
            if (Math.Abs(v - p.V) < 1e-12) return;
            p.V = v;
            Version++;
            Save();
        }

        public static void ResetAll()
        {
            foreach (var p in all) p.V = p.Def;
            Version++;
            Save();
        }

        // only the values that differ from their defaults; the file is the user's, so a missing key means the default
        public static void Save()
        {
            if (!Enabled) return;
            try
            {
                Directory.CreateDirectory(AppInfo.DataDir);
                var sb = new StringBuilder("{\n");
                var changed = all.Where(p => p.Changed).ToList();
                for (int i = 0; i < changed.Count; i++)
                    sb.Append("  \"").Append(changed[i].Key).Append("\": ").Append(changed[i].V.ToString("R", CultureInfo.InvariantCulture))
                      .Append(i < changed.Count - 1 ? ",\n" : "\n");
                sb.Append("}\n");
                File.WriteAllText(FilePath, sb.ToString(), new UTF8Encoding(false));
                fileTime = File.GetLastWriteTimeUtc(FilePath);
                LastError = null;
            }
            catch (Exception ex) { LastError = "tuning.json: " + ex.Message; }
        }

        public static string Format(P p, double v)
        {
            if (p.Int) return ((long)Math.Round(v)).ToString("N0", CultureInfo.InvariantCulture).Replace(",", " ");
            int dec = p.Step >= 1 ? 0 : p.Step >= 0.1 ? 1 : p.Step >= 0.01 ? 2 : 3;
            return v.ToString("F" + dec, CultureInfo.InvariantCulture);
        }
    }
}
