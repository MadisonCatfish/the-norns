// Update.cs - the update in one click (roadmap item 16, owner 04.11.2026): when a newer release is out, About's key
// reads "Update to <version>"; a click downloads that release's installer from GitHub (the same file as the release
// page offers), checks it against the SHA-256 printed in the release notes, starts it and closes the app so the installer
// can replace the files. A failed download leaves the key on "Download failed": a click then opens the release page.
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace TheNorns
{
    partial class DpsForm
    {
        int updatePercent;
        const string ReleaseFiles = "https://github.com/MadisonCatfish/the-norns/releases";

        void StartUpdate()
        {
            if (string.IsNullOrEmpty(updateLatest) || updateState == 6) return;
            var v = updateLatest;
            var file = "TheNorns-Setup-" + v + ".exe";
            var url = ReleaseFiles + "/download/v" + v + "/" + file;
            var path = Path.Combine(Path.GetTempPath(), file);
            updateState = 6; updatePercent = 0; Render();
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
            var wc = new WebClient();
            wc.Headers.Add("User-Agent", "TheNorns");
            wc.DownloadProgressChanged += (s, e) => { if (e.ProgressPercentage != updatePercent) { updatePercent = e.ProgressPercentage; Render(); } };
            wc.DownloadFileCompleted += (s, e) =>
            {
                wc.Dispose();
                string error = e.Error != null ? e.Error.Message : e.Cancelled ? "cancelled" : Verify(path, v, file);
                if (error != null)
                {
                    try { File.Delete(path); } catch { }
                    updateState = 7; updatePage = ReleaseFiles + "/tag/v" + v; Render();
                    return;
                }
                try
                {
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true, WorkingDirectory = Path.GetTempPath() });
                    SavePos(); Close();   // the installer replaces TheNorns.exe: the app must not hold it
                }
                catch { updateState = 7; Render(); }   // the start refused (cancelled at a prompt): the release page then
            };
            try { wc.DownloadFileAsync(new Uri(url), path); }
            catch { wc.Dispose(); updateState = 7; Render(); }
        }

        // null when the downloaded file is the installer: a Windows program, and when the release notes print its SHA-256
        // (they do from 1.3.0 on), that hash. The notes are read from the release page, not the API (no rate limit)
        static string Verify(string path, string version, string file)
        {
            try
            {
                var fi = new FileInfo(path);
                if (!fi.Exists || fi.Length < 1000000) return "too small";
                using (var f = File.OpenRead(path)) if (f.ReadByte() != 'M' || f.ReadByte() != 'Z') return "not a program";
                string notes;
                using (var wc = new WebClient()) { wc.Headers.Add("User-Agent", "TheNorns"); wc.Encoding = System.Text.Encoding.UTF8; notes = wc.DownloadString(ReleaseFiles + "/tag/v" + version); }
                var m = Regex.Match(notes, Regex.Escape(file) + @"[\s\S]{0,400}?\b([0-9A-Fa-f]{64})\b");
                if (!m.Success) return null;   // no hash in the notes: the file from the release itself is taken
                string hash;
                using (var sha = SHA256.Create()) using (var f = File.OpenRead(path)) hash = BitConverter.ToString(sha.ComputeHash(f)).Replace("-", "");
                return hash.Equals(m.Groups[1].Value, StringComparison.OrdinalIgnoreCase) ? null : "SHA-256 differs";
            }
            catch (Exception ex) { return ex.Message; }
        }
    }
}
