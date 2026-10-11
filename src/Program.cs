using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace AtHomeBlocker
{
    public struct CmdResult
    {
        public int ExitCode;
        public string Output;
    }

    static class Blocker
    {
        public const string RuleName = "AtHomeNetBlocker_Rule";
        public const string TaskName = "AtHomeNetBlocker_AutoStart";
        public const string RegPath = @"Software\AtHomeNetBlocker";

        public static CmdResult RunCmd(string file, string args)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo(file, args);
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                psi.WindowStyle = ProcessWindowStyle.Hidden;
                using (Process p = Process.Start(psi))
                {
                    string so = p.StandardOutput.ReadToEnd();
                    string se = p.StandardError.ReadToEnd();
                    p.WaitForExit();
                    CmdResult r;
                    r.ExitCode = p.ExitCode;
                    r.Output = so + se;
                    return r;
                }
            }
            catch (Exception ex)
            {
                CmdResult r;
                r.ExitCode = -1;
                r.Output = ex.Message;
                return r;
            }
        }

        public static bool IsBlocked()
        {
            CmdResult r = RunCmd("netsh", "advfirewall firewall show rule name=\"" + RuleName + "\"");
            return r.ExitCode == 0 &&
                   r.Output.ToLowerInvariant().Contains(RuleName.ToLowerInvariant());
        }

        public static bool SetBlocked(bool block, string appPath)
        {
            string args;
            if (block)
                args = "advfirewall firewall add rule name=\"" + RuleName +
                       "\" dir=out program=\"" + appPath +
                       "\" action=block profile=any enable=yes";
            else
                args = "advfirewall firewall delete rule name=\"" + RuleName + "\"";
            CmdResult r = RunCmd("netsh", args);
            return r.ExitCode == 0;
        }

        public static void SetAutoStart(bool enable)
        {
            string exe = Application.ExecutablePath;
            // Task runs the exe with /silent at logon: applies block and exits, no UI.
            string trArg = "\"\\\"" + exe + "\\\" /silent\"";
            if (enable)
            {
                RunCmd("schtasks",
                    string.Format("/create /tn \"{0}\" /tr {1} /sc onlogon /rl highest /f",
                        TaskName, trArg));
            }
            else
            {
                RunCmd("schtasks",
                    string.Format("/delete /tn \"{0}\" /f", TaskName));
            }
        }

        public static bool IsAutoStart()
        {
            CmdResult r = RunCmd("schtasks", string.Format("/query /tn \"{0}\"", TaskName));
            return r.ExitCode == 0;
        }

        public static string LoadAppPath()
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RegPath))
                {
                    if (k != null)
                    {
                        object p = k.GetValue("AppPath");
                        if (p != null && p.ToString().Length > 0)
                            return p.ToString();
                    }
                }
            }
            catch { }
            return null;
        }

        public static void SaveAppPath(string path)
        {
            try
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RegPath))
                {
                    k.SetValue("AppPath", path ?? "");
                }
            }
            catch { }
        }

        public static string AutoDetectPath()
        {
            try
            {
                foreach (Process p in Process.GetProcesses())
                {
                    try
                    {
                        string fn = p.MainModule.FileName;
                        if (string.IsNullOrEmpty(fn)) continue;
                        string l = fn.ToLowerInvariant();
                        if (l.Contains("athome") || l.Contains("videostreamer") ||
                            l.Contains("streamer") || fn.Contains("掌上看家") ||
                            l.Contains("kanjia"))
                            return fn;
                    }
                    catch { }
                }
            }
            catch { }

            string[] roots = {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            };
            string[] names = {
                "AtHomeVideoStreamer.exe", "AtHomeVideoStreamer_Setup.exe",
                "AtHomeVideoStreamerSetup.exe", "AthomeVideoStreamer.exe",
                "AtHomeCamera.exe", "掌上看家.exe"
            };
            foreach (string root in roots)
            {
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                foreach (string n in names)
                {
                    string f = FindFile(root, n, 3);
                    if (f != null) return f;
                }
            }
            return null;
        }

        static string FindFile(string root, string fileName, int depth)
        {
            try
            {
                string direct = Path.Combine(root, fileName);
                if (File.Exists(direct)) return direct;
                if (depth <= 0) return null;
                string[] dirs = Directory.GetDirectories(root);
                foreach (string d in dirs)
                {
                    try
                    {
                        string f = FindFile(d, fileName, depth - 1);
                        if (f != null) return f;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }
    }

    static class Program
    {
        const string MutexName = "Global\\AtHomeNetBlocker_SingleInstance";

        [STAThread]
        static void Main(string[] args)
        {
            bool silent = false;
            foreach (string a in args)
            {
                if (a.Equals("/silent", StringComparison.OrdinalIgnoreCase) ||
                    a.Equals("-silent", StringComparison.OrdinalIgnoreCase) ||
                    a.Equals("/autostart", StringComparison.OrdinalIgnoreCase))
                    silent = true;
            }

            if (silent)
            {
                RunSilent();
                return;
            }

            bool createdNew;
            using (Mutex m = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    MessageBox.Show(
                        "AtHome Camera Network Blocker is already running.",
                        "Already Running", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
        }

        // Boot-time mode: ensure block is active, then exit. No window, no tray.
        static void RunSilent()
        {
            try
            {
                string appPath = Blocker.LoadAppPath();
                if (string.IsNullOrEmpty(appPath))
                    appPath = Blocker.AutoDetectPath();
                if (!string.IsNullOrEmpty(appPath) && File.Exists(appPath))
                {
                    Blocker.SetBlocked(true, appPath);
                    Blocker.SaveAppPath(appPath);
                }
            }
            catch { }
        }
    }

    public class MainForm : Form
    {
        Label lblStatus;
        Label lblPathLabel;
        TextBox txtPath;
        Button btnBrowse;
        Button btnDetect;
        Button btnToggle;
        CheckBox chkAutoStart;

        string AppPath
        {
            get { return txtPath.Text.Trim(); }
        }

        public MainForm()
        {
            Text = "AtHome Camera Network Blocker";
            ShowInTaskbar = false;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Width = 510;
            Height = 320;
            StartPosition = FormStartPosition.CenterScreen;

            BuildUi();
            LoadConfig();
            RefreshState();
        }

        void BuildUi()
        {
            lblStatus = new Label();
            lblStatus.Location = new Point(16, 16);
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            Controls.Add(lblStatus);

            lblPathLabel = new Label();
            lblPathLabel.Text = "Target program (.exe):";
            lblPathLabel.Location = new Point(16, 70);
            lblPathLabel.AutoSize = true;
            lblPathLabel.Font = new Font("Segoe UI", 9.5f);
            Controls.Add(lblPathLabel);

            txtPath = new TextBox();
            txtPath.Location = new Point(16, 92);
            txtPath.Width = 300;
            Controls.Add(txtPath);

            btnBrowse = new Button();
            btnBrowse.Text = "Browse...";
            btnBrowse.Location = new Point(322, 90);
            btnBrowse.Width = 62;
            btnBrowse.Click += delegate { BrowsePath(); };
            Controls.Add(btnBrowse);

            btnDetect = new Button();
            btnDetect.Text = "Detect";
            btnDetect.Location = new Point(390, 90);
            btnDetect.Width = 80;
            btnDetect.Click += delegate { DetectPath(); };
            Controls.Add(btnDetect);

            btnToggle = new Button();
            btnToggle.Location = new Point(16, 148);
            btnToggle.Width = 260;
            btnToggle.Height = 46;
            btnToggle.Font = new Font("Segoe UI", 11f, FontStyle.Bold);
            btnToggle.Click += delegate { Toggle(); };
            Controls.Add(btnToggle);

            chkAutoStart = new CheckBox();
            chkAutoStart.Text = "Auto-start on boot (silent, blocks network by default)";
            chkAutoStart.Location = new Point(16, 210);
            chkAutoStart.AutoSize = true;
            chkAutoStart.CheckedChanged += delegate { ApplyAutoStart(); };
            Controls.Add(chkAutoStart);

            Label note = new Label();
            note.Text = "No tray icon. At boot this tool runs silently, blocks the network, and exits.\r\nRun this .exe to open the control panel; closing it exits the program.";
            note.Location = new Point(16, 245);
            note.AutoSize = true;
            note.ForeColor = Color.Gray;
            note.Font = new Font("Segoe UI", 8.5f);
            Controls.Add(note);
        }

        void BrowsePath()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Title = "Select the AtHome Camera program (.exe)";
            ofd.Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*";
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                txtPath.Text = ofd.FileName;
                Blocker.SaveAppPath(AppPath);
                RefreshState();
            }
        }

        void DetectPath()
        {
            string found = Blocker.AutoDetectPath();
            if (found != null)
            {
                txtPath.Text = found;
                Blocker.SaveAppPath(AppPath);
                MessageBox.Show("Target found:\n" + found, "Detected",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Could not find the AtHome Camera program automatically.\nPlease use Browse to select its .exe file.",
                    "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            RefreshState();
        }

        void Toggle()
        {
            if (string.IsNullOrEmpty(AppPath))
            {
                MessageBox.Show("Please set the target program first (Browse or Detect).",
                    "No Target", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (!File.Exists(AppPath))
            {
                MessageBox.Show("Target file not found:\n" + AppPath,
                    "File Missing", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            bool nowBlocked = Blocker.IsBlocked();
            bool ok = Blocker.SetBlocked(!nowBlocked, AppPath);
            RefreshState();
            if (!ok)
                MessageBox.Show("Failed to update the firewall rule.\nMake sure this program is run as Administrator.",
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        void RefreshState()
        {
            bool blocked = Blocker.IsBlocked();
            bool hasTarget = !string.IsNullOrEmpty(AppPath) && File.Exists(AppPath);

            if (blocked)
            {
                lblStatus.Text = "STATUS:  NETWORK BLOCKED  (OFFLINE)";
                lblStatus.ForeColor = Color.Firebrick;
                btnToggle.Text = "UNBLOCK  -  restore network";
            }
            else
            {
                lblStatus.Text = "STATUS:  NETWORK ALLOWED  (ONLINE)";
                lblStatus.ForeColor = Color.ForestGreen;
                btnToggle.Text = "BLOCK NETWORK  -  disconnect";
            }
            btnToggle.Enabled = hasTarget;
        }

        void ApplyAutoStart()
        {
            bool enable = chkAutoStart.Checked;
            Blocker.SetAutoStart(enable);
            chkAutoStart.CheckedChanged -= delegate { ApplyAutoStart(); };
            chkAutoStart.Checked = Blocker.IsAutoStart();
            chkAutoStart.CheckedChanged += delegate { ApplyAutoStart(); };
        }

        void LoadConfig()
        {
            string p = Blocker.LoadAppPath();
            if (!string.IsNullOrEmpty(p))
                txtPath.Text = p;
            else
            {
                string d = Blocker.AutoDetectPath();
                if (d != null)
                    txtPath.Text = d;
            }
            chkAutoStart.Checked = Blocker.IsAutoStart();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            Blocker.SaveAppPath(AppPath);
        }
    }
}
