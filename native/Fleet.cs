// Fleet.cs - the client preview & switching module (roadmap 15, owner 29.10.2026), after the behaviour EVE-O Preview
// documents (its code is GPL-3 and is not used here). Off by default: config.json "app": { "fleet": true } turns it on;
// while it is off nothing in this file runs and no hook is installed.
// Step 1, the client tracker: the EVE windows by HWND (a relog keeps its window's place), the login windows, the client
// in front, the cycle order and this session's skips. It reads window titles, each process's exe name and the
// foreground window - nothing from the game.
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace TheNorns
{
    // one EVE client window: its character, or null on the login screen (title "EVE")
    class ClientWindow
    {
        public IntPtr Hwnd;
        public int Pid;
        public string Character;
        public bool Login { get { return Character == null; } }
        public override string ToString() { return Login ? "login (pid " + Pid + ")" : Character; }
    }

    // the open EVE client windows: "EVE - <character>" or "EVE", and only those of exefile.exe
    static class ClientWindows
    {
        delegate bool EnumWindowsProc(IntPtr h, IntPtr l);
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowsProc cb, IntPtr l);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
        [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr h);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out int pid);
        [DllImport("kernel32.dll")] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern bool QueryFullProcessImageName(IntPtr p, int flags, StringBuilder s, ref int n);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr h);
        const uint QueryLimited = 0x1000;   // PROCESS_QUERY_LIMITED_INFORMATION: the exe path, nothing of the process's memory

        // pid -> whether it is exefile.exe, asked once per process (the windows are polled every second)
        static readonly Dictionary<int, bool> isEve = new Dictionary<int, bool>();

        public static List<ClientWindow> Find()
        {
            var list = new List<ClientWindow>();
            var seen = new HashSet<int>();
            var sb = new StringBuilder(256);
            EnumWindows((h, l) =>
            {
                if (!IsWindowVisible(h)) return true;
                sb.Length = 0;
                GetWindowText(h, sb, 256);
                var t = sb.ToString();
                string character;
                if (t.StartsWith("EVE - ", StringComparison.Ordinal)) character = t.Substring(6);
                else if (t.Equals("EVE", StringComparison.OrdinalIgnoreCase)) character = null;
                else return true;
                int pid;
                GetWindowThreadProcessId(h, out pid);
                seen.Add(pid);
                if (IsEve(pid)) list.Add(new ClientWindow { Hwnd = h, Pid = pid, Character = character });
                return true;
            }, IntPtr.Zero);
            foreach (var p in isEve.Keys.Where(p => !seen.Contains(p)).ToList()) isEve.Remove(p);   // a closed process's pid can come back
            return list;
        }

        static bool IsEve(int pid)
        {
            bool r;
            if (isEve.TryGetValue(pid, out r)) return r;
            r = true;   // the exe can't be asked: trust the title, as the meter always did
            IntPtr p = OpenProcess(QueryLimited, false, pid);
            if (p != IntPtr.Zero)
                try
                {
                    var s = new StringBuilder(1024); int n = s.Capacity;
                    if (QueryFullProcessImageName(p, 0, s, ref n)) r = Path.GetFileName(s.ToString()).Equals("exefile.exe", StringComparison.OrdinalIgnoreCase);
                }
                finally { CloseHandle(p); }
            isEve[pid] = r;
            return r;
        }
    }

    // the clients over time: which are open (by HWND), which is in front, the cycle order and this session's skips
    class ClientTracker
    {
        static readonly StringComparer Names = StringComparer.OrdinalIgnoreCase;
        readonly Dictionary<IntPtr, ClientWindow> byHwnd = new Dictionary<IntPtr, ClientWindow>();
        readonly List<IntPtr> recent = new List<IntPtr>();   // activation order, oldest first; a new window starts oldest
        IntPtr pending;   // a window in front that the last poll did not know yet (a client that just opened)

        public readonly List<string> Order = new List<string>();   // the cycle: config "fleet_order" first, then as they appear
        public readonly HashSet<string> Skipped = new HashSet<string>(Names);   // out of the cycle for this session only
        public IntPtr Active { get; private set; }   // the last EVE client in front; kept while another app is in front
        public event Action Changed;

        public ClientTracker(IEnumerable<string> order)
        {
            if (order != null) foreach (var c in order) if (!string.IsNullOrEmpty(c) && !Order.Contains(c, Names)) Order.Add(c);
        }

        public IEnumerable<ClientWindow> All { get { return byHwnd.Values; } }
        public IEnumerable<IntPtr> Recent { get { return recent; } }
        public int CharacterCount { get { return byHwnd.Values.Count(w => !w.Login); } }
        public ClientWindow ActiveClient { get { ClientWindow w; return byHwnd.TryGetValue(Active, out w) ? w : null; } }

        // one poll's windows: new ones are added, closed ones dropped; a relog (or login -> character) keeps the window
        // and its place in the activation order
        public bool Update(IList<ClientWindow> now)
        {
            bool changed = false;
            var live = new HashSet<IntPtr>(now.Select(w => w.Hwnd));
            foreach (var h in byHwnd.Keys.Where(h => !live.Contains(h)).ToList())
            {
                byHwnd.Remove(h); recent.Remove(h);
                if (Active == h) Active = IntPtr.Zero;
                changed = true;
            }
            foreach (var w in now)
            {
                ClientWindow old;
                if (!byHwnd.TryGetValue(w.Hwnd, out old)) { byHwnd[w.Hwnd] = w; recent.Insert(0, w.Hwnd); changed = true; }
                else if (old.Pid != w.Pid) { byHwnd[w.Hwnd] = w; changed = true; }   // a handle reused by another process
                else if (!string.Equals(old.Character, w.Character, StringComparison.Ordinal)) { old.Character = w.Character; changed = true; }
                if (!w.Login && !Order.Contains(w.Character, Names)) Order.Add(w.Character);
            }
            if (pending != IntPtr.Zero && byHwnd.ContainsKey(pending)) { changed |= Activate(pending); pending = IntPtr.Zero; }
            if (changed && Changed != null) Changed();
            return changed;
        }

        // the window in front changed: an EVE client becomes the active one; any other window leaves it as it was
        public void OnForeground(IntPtr h)
        {
            if (h == IntPtr.Zero) return;   // nothing in front for a moment, during an activation
            if (!byHwnd.ContainsKey(h)) { pending = h; return; }
            pending = IntPtr.Zero;
            if (Activate(h) && Changed != null) Changed();
        }

        bool Activate(IntPtr h)
        {
            bool changed = Active != h;
            Active = h;
            recent.Remove(h); recent.Add(h);
            return changed;
        }

        // the client after the active one in the cycle, dir +1 or -1: by Order, past closed and skipped characters,
        // once around; null when there is none. Without an active character it starts at the first (or last) one.
        // Login windows are not in the cycle.
        public ClientWindow Next(int dir)
        {
            int n = Order.Count;
            if (n == 0) return null;
            var cur = ActiveClient;
            int start = cur != null && !cur.Login ? Order.FindIndex(c => Names.Equals(c, cur.Character)) : -1;
            for (int k = 1; k <= n; k++)
            {
                int i = start < 0 ? (dir > 0 ? k - 1 : n - k) : ((start + dir * k) % n + n) % n;
                if (Skipped.Contains(Order[i])) continue;
                var w = byHwnd.Values.FirstOrDefault(x => !x.Login && Names.Equals(x.Character, Order[i]));
                if (w != null) return w;
            }
            return null;
        }

        // Toggle Skip: true when the character is now out of the cycle
        public bool ToggleSkip(string character)
        {
            if (string.IsNullOrEmpty(character)) return false;
            bool on = !Skipped.Remove(character);
            if (on) Skipped.Add(character);
            if (Changed != null) Changed();
            return on;
        }
    }

    // the window in front, from Windows' foreground event: out of context, so nothing is loaded into other processes
    sealed class ForegroundWatch : IDisposable
    {
        delegate void WinEventProc(IntPtr hook, uint ev, IntPtr hwnd, int obj, int child, uint thread, uint time);
        [DllImport("user32.dll")] static extern IntPtr SetWinEventHook(uint min, uint max, IntPtr module, WinEventProc proc, uint pid, uint thread, uint flags);
        [DllImport("user32.dll")] static extern bool UnhookWinEvent(IntPtr hook);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        const uint EventSystemForeground = 3, OutOfContext = 0;

        readonly WinEventProc proc;   // kept: Windows calls it for as long as the hook lives
        IntPtr hook;

        // on the UI thread: the events come through its message loop
        public ForegroundWatch(Action<IntPtr> onForeground)
        {
            proc = (h, e, hwnd, o, c, t, ms) => onForeground(hwnd);
            hook = SetWinEventHook(EventSystemForeground, EventSystemForeground, IntPtr.Zero, proc, 0, 0, OutOfContext);
        }

        public void Dispose() { if (hook != IntPtr.Zero) { UnhookWinEvent(hook); hook = IntPtr.Zero; } }
    }

    // the module as the body holds it: made only when it is switched on (or for --fleet-trace)
    sealed class Fleet : IDisposable
    {
        public static bool Enabled { get { var v = DpsForm.AppSetting("fleet"); return v is bool && (bool)v; } }
        public static string TracePath;   // --fleet-trace <file>: the module on for this run, every change written there

        public readonly ClientTracker Clients;
        readonly ForegroundWatch watch;

        public Fleet()
        {
            Clients = new ClientTracker(ConfiguredOrder());
            if (TracePath != null) Clients.Changed += Trace;
            Poll();
            Clients.OnForeground(ForegroundWatch.GetForegroundWindow());
            watch = new ForegroundWatch(Clients.OnForeground);
        }

        // config.json "app": { "fleet_order": [ "Pilot-1", "Pilot-2" ] }
        static IEnumerable<string> ConfiguredOrder()
        {
            var v = DpsForm.AppSetting("fleet_order") as IEnumerable;
            if (v == null || v is string) return Enumerable.Empty<string>();
            return v.Cast<object>().Where(x => x != null).Select(x => x.ToString()).ToList();
        }

        public void Poll() { Clients.Update(ClientWindows.Find()); }

        void Trace()
        {
            try
            {
                var a = Clients.ActiveClient;
                var line = string.Format("{0:HH:mm:ss.fff}  active: {1}  open: {2}  order: {3}  skipped: {4}  next: {5}  prev: {6}",
                    DateTime.Now, a == null ? "-" : a.ToString(),
                    string.Join(", ", Clients.Recent.Select(h => Clients.All.First(w => w.Hwnd == h).ToString())),
                    string.Join(", ", Clients.Order), string.Join(", ", Clients.Skipped),
                    Clients.Next(1) == null ? "-" : Clients.Next(1).ToString(), Clients.Next(-1) == null ? "-" : Clients.Next(-1).ToString());
                File.AppendAllText(TracePath, line + Environment.NewLine);
            }
            catch { }
        }

        public void Dispose() { watch.Dispose(); }

        // selftest: the tracker on made-up windows
        public static string SelfTest()
        {
            var sb = new StringBuilder();
            Func<int, int, string, ClientWindow> win = (h, pid, c) => new ClientWindow { Hwnd = (IntPtr)h, Pid = pid, Character = c };
            Action<bool, string> check = (ok, what) => { if (!ok) throw new Exception("fleet: " + what); sb.AppendLine("fleet: " + what); };

            var t = new ClientTracker(new[] { "Pilot-3", "Pilot-1" });
            int changes = 0; t.Changed += () => changes++;
            t.Update(new[] { win(10, 1, "Pilot-1"), win(20, 2, "Pilot-2"), win(30, 3, "Pilot-3"), win(40, 4, null) });
            check(t.CharacterCount == 3 && t.All.Count() == 4, "three characters and a login window");
            check(string.Join(",", t.Order) == "Pilot-3,Pilot-1,Pilot-2", "order: config first, then as they appear");
            check(t.Next(1).Character == "Pilot-3" && t.Next(-1).Character == "Pilot-2", "no active client: next is the first, previous the last");

            t.OnForeground((IntPtr)10);
            check(t.ActiveClient.Character == "Pilot-1", "the window in front is the active client");
            check(t.Next(1).Character == "Pilot-2" && t.Next(-1).Character == "Pilot-3", "next and previous around Pilot-1");
            t.OnForeground((IntPtr)999);
            check(t.ActiveClient.Character == "Pilot-1", "another app in front keeps the active client");

            t.ToggleSkip("Pilot-2");
            check(t.Next(1).Character == "Pilot-3", "a skipped character is passed over");
            t.OnForeground((IntPtr)20);
            check(t.Next(1).Character == "Pilot-3" && t.Next(-1).Character == "Pilot-1", "from a skipped active client the cycle goes on from its place");
            t.ToggleSkip("Pilot-2");
            check(!t.Skipped.Any(), "skip toggled back");

            t.Update(new[] { win(10, 1, "Pilot-1"), win(20, 2, "Pilot-5"), win(30, 3, "Pilot-3"), win(40, 4, "Pilot-4") });
            check(t.ActiveClient.Hwnd == (IntPtr)20 && t.ActiveClient.Character == "Pilot-5", "a relog keeps the window active");
            check(t.Recent.Last() == (IntPtr)20 && t.Recent.Contains((IntPtr)40), "the login window became Pilot-4 in its place");
            check(t.Next(1).Character == "Pilot-4" && t.Next(-1).Character == "Pilot-1", "relogged characters join the cycle");

            t.Update(new[] { win(10, 1, "Pilot-1"), win(30, 3, "Pilot-3") });
            check(t.ActiveClient == null && t.CharacterCount == 2, "the active client closed");
            t.OnForeground((IntPtr)50);
            t.Update(new[] { win(10, 1, "Pilot-1"), win(30, 3, "Pilot-3"), win(50, 6, "Pilot-6") });
            check(t.ActiveClient != null && t.ActiveClient.Character == "Pilot-6", "a client in front before the poll knew it becomes active");

            t.ToggleSkip("Pilot-1"); t.ToggleSkip("Pilot-3");
            check(t.Next(1).Character == "Pilot-6", "only the active client left: it stays");
            t.ToggleSkip("Pilot-6");
            check(t.Next(1) == null && t.Next(-1) == null, "all skipped: no next client");
            check(changes > 0, "changes reported (" + changes + ")");
            return sb.ToString();
        }
    }
}
