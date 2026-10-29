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
using System.Windows.Forms;

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

    // Step 2: the active client, live, on the power display. Windows' DWM draws the client's window into a small window
    // of ours over the display's glass - the picture never passes through this app - and keeps that relationship until
    // the client changes: the new one is made before the old one goes, so there is no empty frame. Over it a
    // click-through layered lid carries what the body has there: its corners around the rounded glass, the glass's light
    // marks and rim (DpsForm.PowerLid). A minimized client has no picture (DWM shows none): the display's animation is back.
    sealed class LivePreview : IDisposable
    {
        [StructLayout(LayoutKind.Sequential)] struct RECT { public int L, T, R, B; }
        [StructLayout(LayoutKind.Sequential)]
        struct ThumbProps { public int Flags; public RECT Dest, Source; public byte Opacity; public bool Visible, ClientAreaOnly; }
        [DllImport("dwmapi.dll")] static extern int DwmRegisterThumbnail(IntPtr dest, IntPtr source, out IntPtr thumb);
        [DllImport("dwmapi.dll")] static extern int DwmUnregisterThumbnail(IntPtr thumb);
        [DllImport("dwmapi.dll")] static extern int DwmUpdateThumbnailProperties(IntPtr thumb, ref ThumbProps p);
        [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr h, out RECT r);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int hh, uint flags);
        const int TnpDest = 0x1, TnpSource = 0x2, TnpOpacity = 0x4, TnpVisible = 0x8, TnpClientOnly = 0x10;
        const uint SwpNoZOrder = 0x4, SwpNoActivate = 0x10, SwpNoOwnerZOrder = 0x200;

        // the window the picture is drawn into: never active, the mouse goes through to the body
        sealed class Glass : Form
        {
            public Glass(bool layered)
            {
                this.layered = layered;
                FormBorderStyle = FormBorderStyle.None; ShowInTaskbar = false; StartPosition = FormStartPosition.Manual;
                AutoScaleMode = AutoScaleMode.None; BackColor = System.Drawing.Color.Black; TopMost = true;
            }
            readonly bool layered;
            public Action<int> Wheel;   // should Windows send the wheel here and not to the body under it
            protected override bool ShowWithoutActivation { get { return true; } }
            protected override CreateParams CreateParams
            {
                get
                {
                    var cp = base.CreateParams;
                    cp.ExStyle |= 0x80 /* TOOLWINDOW */ | 0x08000000 /* NOACTIVATE */;
                    if (layered) cp.ExStyle |= 0x80000 /* LAYERED */ | 0x20 /* TRANSPARENT: click-through */;
                    return cp;
                }
            }
            protected override void WndProc(ref Message m)
            {
                if (m.Msg == 0x84 /* WM_NCHITTEST */) { m.Result = (IntPtr)(-1) /* HTTRANSPARENT: to the body under it */; return; }
                if (m.Msg == 0x21 /* WM_MOUSEACTIVATE */) { m.Result = (IntPtr)3 /* MA_NOACTIVATE */; return; }
                if (m.Msg == 0x20A /* WM_MOUSEWHEEL */ && Wheel != null) { Wheel((short)((m.WParam.ToInt64() >> 16) & 0xFFFF)); m.Result = IntPtr.Zero; return; }
                base.WndProc(ref m);
            }
        }

        Glass host, lid;
        IntPtr thumb, source;
        System.Drawing.Rectangle placed;
        System.Drawing.Size lastClient, lastDest;
        public Action<string> Log;
        public Action<int> Wheel;

        public bool Shown { get { return thumb != IntPtr.Zero; } }

        // every body frame: show src (IntPtr.Zero: none) in face (body coordinates); lid() draws the lid for that frame
        public void Sync(Form body, System.Drawing.Rectangle face, IntPtr src, Func<System.Drawing.Bitmap> lid)
        {
            if (src != IntPtr.Zero && (!IsWindow(src) || IsIconic(src))) src = IntPtr.Zero;
            if (src == IntPtr.Zero || face.Width < 4 || face.Height < 4 || !body.Visible) { Hide(); return; }
            if (host == null) { host = new Glass(false) { Wheel = d => { if (Wheel != null) Wheel(d); } }; this.lid = new Glass(true); }
            var at = new System.Drawing.Rectangle(body.Left + face.X, body.Top + face.Y, face.Width, face.Height);
            if (!host.Visible) { host.Bounds = at; this.lid.Bounds = at; host.Show(body); this.lid.Show(host); placed = at; }
            else Follow(at);
            if (src != source || thumb == IntPtr.Zero) Attach(src, face.Size);
            else Fit(face.Size, false);
            if (thumb == IntPtr.Zero) { Hide(); return; }
            using (var b = lid()) AppChrome.PresentLayered(this.lid, b);
        }

        // the body moved: the picture and its lid go with it at once, not at the next frame
        public void Follow(System.Drawing.Rectangle at)
        {
            if (host == null || !host.Visible || at == placed) return;
            placed = at;
            foreach (var f in new[] { host, lid }) SetWindowPos(f.Handle, IntPtr.Zero, at.X, at.Y, at.Width, at.Height, SwpNoZOrder | SwpNoActivate | SwpNoOwnerZOrder);
        }

        void Attach(IntPtr src, System.Drawing.Size dest)
        {
            IntPtr old = thumb, th;
            int hr = DwmRegisterThumbnail(host.Handle, src, out th);
            if (hr != 0) { Note("register failed 0x" + hr.ToString("X8")); return; }   // the old picture stays until the next try
            thumb = th; source = src;
            Fit(dest, true);
            if (old != IntPtr.Zero) DwmUnregisterThumbnail(old);
        }

        // the client's picture, cut to the glass's proportions, over the whole glass; sent only when a size changed
        void Fit(System.Drawing.Size dest, bool force)
        {
            RECT c;
            if (!GetClientRect(source, out c)) return;
            var client = new System.Drawing.Size(c.R - c.L, c.B - c.T);
            if (!force && client == lastClient && dest == lastDest) return;
            var crop = Crop(client, dest);
            var p = new ThumbProps
            {
                Flags = TnpDest | TnpSource | TnpOpacity | TnpVisible | TnpClientOnly,
                Dest = new RECT { R = dest.Width, B = dest.Height },
                Source = new RECT { L = crop.Left, T = crop.Top, R = crop.Right, B = crop.Bottom },
                Opacity = 255, Visible = true, ClientAreaOnly = true
            };
            int hr = DwmUpdateThumbnailProperties(thumb, ref p);
            if (hr != 0) { Note("update failed 0x" + hr.ToString("X8")); DwmUnregisterThumbnail(thumb); thumb = IntPtr.Zero; source = IntPtr.Zero; return; }   // made again next frame
            lastClient = client; lastDest = dest;
            Note("showing " + source.ToInt64().ToString("X") + ": client " + client.Width + "x" + client.Height + ", part " + crop + ", glass " + dest.Width + "x" + dest.Height);
        }

        public void Hide()
        {
            if (thumb != IntPtr.Zero) { DwmUnregisterThumbnail(thumb); thumb = IntPtr.Zero; Note("hidden"); }
            source = IntPtr.Zero; lastClient = lastDest = System.Drawing.Size.Empty;
            if (host != null && host.Visible) { lid.Hide(); host.Hide(); }
        }

        void Note(string s) { if (Log != null) Log("preview: " + s); }

        // the part of a client (w x h) that fills the glass without bars: cut at the sides or at the top and bottom
        public static System.Drawing.Rectangle Crop(System.Drawing.Size client, System.Drawing.Size dest)
        {
            if (client.Width <= 0 || client.Height <= 0 || dest.Width <= 0 || dest.Height <= 0) return new System.Drawing.Rectangle(System.Drawing.Point.Empty, client);
            double a = (double)dest.Width / dest.Height;
            if ((double)client.Width / client.Height > a)
            {
                int w = (int)Math.Round(client.Height * a);
                return new System.Drawing.Rectangle((client.Width - w) / 2, 0, w, client.Height);
            }
            int h = (int)Math.Round(client.Width / a);
            return new System.Drawing.Rectangle(0, (client.Height - h) / 2, client.Width, h);
        }

        public void Dispose()
        {
            Hide();
            if (host != null) { lid.Dispose(); host.Dispose(); host = lid = null; }
        }
    }

    // Step 4: a client to the front, as EVE-O does it: restored when minimized (asynchronously - a busy client can't hold
    // the app up), then SetForegroundWindow and SetFocus; SwitchToThisWindow as the last resort. Windows lets only the
    // process that got the last input bring a window forward, and the wheel over an inactive window does not count (test
    // build fl4: every switch refused, the taskbar buttons flashed) - so when it refuses, the app's input queue is joined
    // to the foreground window's for that one call (AttachThreadInput) and parted again at once. No input is sent.
    static class ClientSwitch
    {
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool IsIconic(IntPtr h);
        [DllImport("user32.dll")] static extern bool ShowWindowAsync(IntPtr h, int cmd);
        [DllImport("user32.dll")] static extern bool SetForegroundWindow(IntPtr h);
        [DllImport("user32.dll")] static extern bool BringWindowToTop(IntPtr h);
        [DllImport("user32.dll")] static extern IntPtr SetFocus(IntPtr h);
        [DllImport("user32.dll")] static extern void SwitchToThisWindow(IntPtr h, bool altTab);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, IntPtr pid);
        [DllImport("user32.dll")] static extern bool AttachThreadInput(uint thread, uint to, bool attach);
        [DllImport("kernel32.dll")] static extern uint GetCurrentThreadId();
        const int SwRestore = 9;

        // true when h is in front; how: what happened, for the trace
        public static bool Activate(IntPtr h, out string how)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            how = "gone";
            if (!IsWindow(h)) return false;
            bool restored = IsIconic(h) && ShowWindowAsync(h, SwRestore);
            string path = "foreground";
            // only a refusal goes on: the switch itself completes a moment later in the client's own queue, so right after
            // the call nothing may be in front yet (test build fl7: every switch went the long way for that, 28 ms on average)
            bool front = SetForegroundWindow(h);
            if (!front)
            {
                IntPtr fg = ForegroundWatch.GetForegroundWindow();
                uint me = GetCurrentThreadId(), them = fg == IntPtr.Zero ? 0 : GetWindowThreadProcessId(fg, IntPtr.Zero);
                bool joined = them != 0 && them != me && AttachThreadInput(me, them, true);
                try { BringWindowToTop(h); front = SetForegroundWindow(h); }
                finally { if (joined) AttachThreadInput(me, them, false); }
                path = joined ? "joined input" : "refused";
            }
            SetFocus(h);
            if (!front) { SwitchToThisWindow(h, false); path += ", SwitchToThisWindow"; }
            bool inFront = ForegroundWatch.GetForegroundWindow() == h;
            how = (restored ? "restored, " : "") + path + (inFront ? ", in front" : ", not in front") + ", " + sw.Elapsed.TotalMilliseconds.ToString("0.0") + " ms";
            return inFront || front;
        }

        // the wheel's notches (120 a notch; a touchpad sends parts): up = the previous client (-1), down = the next (+1)
        public static List<int> Wheel(ref int sum, int delta)
        {
            var steps = new List<int>();
            sum += delta;
            while (Math.Abs(sum) >= 120) { int up = Math.Sign(sum); sum -= up * 120; steps.Add(-up); }
            return steps;
        }
    }

    // Step 5: hotkeys as EVE-O has them - RegisterHotKey for "next" and "previous" (config.json "app": { "fleet_next":
    // "1", "fleet_prev": "2" }, a key or a list of keys, with Control+ / Shift+ / Alt+ / Win+). Windows counts a hotkey
    // as the app's own input, so the client comes forward at once; held down, the key repeats and the clients flip by.
    // Only one program can hold a key: while EVE-O holds the same one, it can't be had here (the trace says so).
    sealed class Hotkeys : NativeWindow, IDisposable
    {
        [DllImport("user32.dll")] static extern bool RegisterHotKey(IntPtr h, int id, uint mods, uint vk);
        [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr h, int id);
        const int WmHotkey = 0x312;
        readonly Dictionary<int, int> dirOf = new Dictionary<int, int>();   // hotkey id -> +1 next, -1 previous
        readonly Action<int> step;

        public Hotkeys(Action<int> step)
        {
            this.step = step;
            CreateHandle(new CreateParams { Parent = (IntPtr)(-3) /* HWND_MESSAGE: a window only for messages */ });
        }

        // "ok", "not a key" or "taken" (another program holds it)
        public string Add(string key, int dir)
        {
            uint mods, vk;
            if (!Parse(key, out mods, out vk)) return "not a key";
            int id = dirOf.Count + 1;
            if (!RegisterHotKey(Handle, id, mods, vk)) return "taken";
            dirOf[id] = dir;
            return "ok";
        }

        protected override void WndProc(ref Message m)
        {
            int dir;
            if (m.Msg == WmHotkey && dirOf.TryGetValue(m.WParam.ToInt32(), out dir)) { step(dir); return; }
            base.WndProc(ref m);
        }

        // "1", "F16", "Control+F16", "Shift+Alt+X": modifiers, then the key (a digit alone is its key)
        public static bool Parse(string s, out uint mods, out uint vk)
        {
            mods = vk = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            var parts = s.Split('+').Select(p => p.Trim()).ToArray();
            for (int i = 0; i < parts.Length - 1; i++)
                switch (parts[i].ToLowerInvariant())
                {
                    case "alt": mods |= 1; break;
                    case "control": case "ctrl": mods |= 2; break;
                    case "shift": mods |= 4; break;
                    case "win": mods |= 8; break;
                    default: return false;
                }
            var k = parts[parts.Length - 1];
            if (k.Length == 1 && char.IsDigit(k[0])) k = "D" + k;
            Keys key;
            if (!Enum.TryParse(k, true, out key) || (key & Keys.KeyCode) == Keys.None || (key & Keys.Modifiers) != 0) return false;
            vk = (uint)(key & Keys.KeyCode);
            return true;
        }

        public void Dispose()
        {
            foreach (var id in dirOf.Keys) UnregisterHotKey(Handle, id);
            dirOf.Clear();
            if (Handle != IntPtr.Zero) DestroyHandle();
        }
    }

    // the module as the body holds it: made only when it is switched on (or for --fleet-trace)
    sealed class Fleet : IDisposable
    {
        public static bool Enabled { get { var v = DpsForm.AppSetting("fleet"); return v is bool && (bool)v; } }
        public static string TracePath;   // --fleet-trace <file>: the module on for this run, every change written there

        public readonly ClientTracker Clients;
        public readonly LivePreview Preview = new LivePreview();
        readonly ForegroundWatch watch;
        Hotkeys keys;
        readonly Dictionary<string, string> keyState = new Dictionary<string, string>();   // setting -> "ok", "taken", "not a key"

        public Fleet()
        {
            Clients = new ClientTracker(Configured("fleet_order"));
            if (TracePath != null) { Clients.Changed += Trace; Preview.Log = Write; }
            Preview.Wheel = Wheel;
            Poll();
            Clients.OnForeground(ForegroundWatch.GetForegroundWindow());
            watch = new ForegroundWatch(Clients.OnForeground);
            ReloadKeys();
        }

        // the hotkeys from config.json, again (Settings -> Eve-O changed them); a --fleet-trace run with neither set takes
        // 1 / 2, as the owner's EVE-O has them
        public void ReloadKeys()
        {
            SuspendKeys();
            keys = new Hotkeys(dir => Step(dir, "key"));
            bool none = !Configured("fleet_next").Any(k => k.Trim() != "") && !Configured("fleet_prev").Any(k => k.Trim() != "");
            Bind("fleet_next", 1, none ? "1" : null); Bind("fleet_prev", -1, none ? "2" : null);
        }

        // while a key is being set in Settings: none held, so its keys reach the field
        public void SuspendKeys()
        {
            if (keys != null) { keys.Dispose(); keys = null; }
            keyState.Clear();
        }

        public string KeyState(string setting) { string s; return keyState.TryGetValue(setting, out s) ? s : null; }

        void Bind(string setting, int dir, string testKey)
        {
            var list = Configured(setting).Where(k => k.Trim() != "").ToList();
            if (list.Count == 0 && TracePath != null && testKey != null) list.Add(testKey);
            foreach (var k in list)
            {
                var r = keys.Add(k, dir);
                if (r != "ok" || !keyState.ContainsKey(setting)) keyState[setting] = r;   // the field shows the worst
                Write("key " + (dir > 0 ? "next" : "previous") + " " + k + ": " + r);
            }
        }

        // config.json "app": a string or a list of strings ("fleet_order": [ "Pilot-1", "Pilot-2" ], "fleet_next": "1")
        public static IEnumerable<string> Configured(string setting)
        {
            var v = DpsForm.AppSetting(setting);
            if (v is string) return new[] { (string)v };
            var list = v as IEnumerable;
            if (list == null) return Enumerable.Empty<string>();
            return list.Cast<object>().Where(x => x != null).Select(x => x.ToString()).ToList();
        }

        public void Poll() { Clients.Update(ClientWindows.Find()); }

        // the wheel over the power display: a client per notch, in the cycle's order. The tracker takes a client that came
        // to the front at once (Windows' foreground event confirms it a moment later), so quick notches walk on
        int wheel;
        public void Wheel(int delta) { foreach (var dir in ClientSwitch.Wheel(ref wheel, delta)) Step(dir, "wheel"); }

        // one client on (+1) or back (-1), from a hotkey or the wheel
        void Step(int dir, string from)
        {
            var w = Clients.Next(dir);
            string what = from + " " + (dir > 0 ? "next" : "previous");
            if (w == null) { Write(what + ": no client"); return; }
            string how;
            if (ClientSwitch.Activate(w.Hwnd, out how)) Clients.OnForeground(w.Hwnd);   // a refused one would only mislead the next step
            Write(what + " -> " + w + ": " + how);
        }

        void Trace()
        {
            var a = Clients.ActiveClient;
            Write(string.Format("active: {0}  open: {1}  order: {2}  skipped: {3}  next: {4}  prev: {5}",
                a == null ? "-" : a.ToString(),
                string.Join(", ", Clients.Recent.Select(h => Clients.All.First(w => w.Hwnd == h).ToString())),
                string.Join(", ", Clients.Order), string.Join(", ", Clients.Skipped),
                Clients.Next(1) == null ? "-" : Clients.Next(1).ToString(), Clients.Next(-1) == null ? "-" : Clients.Next(-1).ToString()));
        }

        void Write(string line)
        {
            if (TracePath == null) return;
            try { File.AppendAllText(TracePath, DateTime.Now.ToString("HH:mm:ss.fff") + "  " + line + Environment.NewLine); }
            catch { }
        }

        public void Dispose() { watch.Dispose(); SuspendKeys(); Preview.Dispose(); }

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

            // the live picture's cut: the glass (66 x 35 here) is wider than 16:9, narrower than 21:9
            var glass = new System.Drawing.Size(66, 35);
            var c169 = LivePreview.Crop(new System.Drawing.Size(1600, 900), glass);
            check(c169.Width == 1600 && c169.Height == 848 && c169.Top == 26, "16:9 client: cut at the top and bottom " + c169);
            var c219 = LivePreview.Crop(new System.Drawing.Size(3440, 1440), glass);
            check(c219.Height == 1440 && c219.Width == 2715 && c219.Left == 362, "21:9 client: cut at the sides " + c219);
            check(LivePreview.Crop(System.Drawing.Size.Empty, glass).IsEmpty, "no client size: nothing to cut");

            // the wheel: a notch is 120; up is the previous client, down the next; a touchpad's parts add up
            int sum = 0;
            check(string.Join(",", ClientSwitch.Wheel(ref sum, 120)) == "-1", "a notch up: the previous client");
            check(string.Join(",", ClientSwitch.Wheel(ref sum, -240)) == "1,1", "two notches down: two clients on");
            check(ClientSwitch.Wheel(ref sum, 60).Count == 0 && string.Join(",", ClientSwitch.Wheel(ref sum, 60)) == "-1" && sum == 0, "two half notches make one");

            // hotkeys as EVE-O writes them
            uint mods, vk;
            check(Hotkeys.Parse("1", out mods, out vk) && mods == 0 && vk == (uint)Keys.D1, "key \"1\" is the digit 1");
            check(Hotkeys.Parse("Control+F16", out mods, out vk) && mods == 2 && vk == (uint)Keys.F16, "Control+F16");
            check(Hotkeys.Parse("Shift+Alt+x", out mods, out vk) && mods == 5 && vk == (uint)Keys.X, "Shift+Alt+x");
            check(!Hotkeys.Parse("Hyper+F1", out mods, out vk) && !Hotkeys.Parse("Foo", out mods, out vk) && !Hotkeys.Parse("", out mods, out vk), "not keys: refused");
            return sb.ToString();
        }
    }
}
