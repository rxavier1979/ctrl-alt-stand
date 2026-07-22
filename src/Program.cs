using System;
using System.Drawing;
using System.IO;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[assembly: AssemblyTitle("Ctrl+Alt+Stand")]
[assembly: AssemblyDescription("A standing-desk routine timer for Windows")]
[assembly: AssemblyCompany("Raul Soto")]
[assembly: AssemblyProduct("Ctrl+Alt+Stand")]
[assembly: AssemblyVersion("0.1.0.0")]
[assembly: AssemblyFileVersion("0.1.0.0")]

namespace CtrlAltStand
{
    internal enum DeskPhase
    {
        Sit,
        Stand,
        Move
    }

    internal sealed class CyclePlan
    {
        public int SitMinutes = 30;
        public int StandMinutes = 20;
        public int MoveMinutes = 3;
        public bool MoveEnabled = true;

        public int SecondsFor(DeskPhase phase)
        {
            switch (phase)
            {
                case DeskPhase.Stand:
                    return StandMinutes * 60;
                case DeskPhase.Move:
                    return MoveMinutes * 60;
                default:
                    return SitMinutes * 60;
            }
        }

        public DeskPhase Next(DeskPhase current)
        {
            if (current == DeskPhase.Sit)
            {
                return DeskPhase.Stand;
            }

            if (current == DeskPhase.Stand && MoveEnabled)
            {
                return DeskPhase.Move;
            }

            return DeskPhase.Sit;
        }
    }

    internal sealed class AppSettings
    {
        public readonly CyclePlan Plan = new CyclePlan();
        public bool SoundEnabled = true;
        public bool AlwaysOnTop = true;

        private static string SettingsPath
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CtrlAltStand",
                    "settings.ini");
            }
        }

        public void Load()
        {
            if (!File.Exists(SettingsPath))
            {
                return;
            }

            foreach (string rawLine in File.ReadAllLines(SettingsPath))
            {
                string line = rawLine.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
                {
                    continue;
                }

                int separator = line.IndexOf('=');
                if (separator < 1)
                {
                    continue;
                }

                string key = line.Substring(0, separator).Trim();
                string value = line.Substring(separator + 1).Trim();
                int parsedNumber;
                bool parsedBoolean;

                if (key == "SitMinutes" && int.TryParse(value, out parsedNumber))
                {
                    Plan.SitMinutes = ClampMinutes(parsedNumber);
                }
                else if (key == "StandMinutes" && int.TryParse(value, out parsedNumber))
                {
                    Plan.StandMinutes = ClampMinutes(parsedNumber);
                }
                else if (key == "MoveMinutes" && int.TryParse(value, out parsedNumber))
                {
                    Plan.MoveMinutes = ClampMinutes(parsedNumber);
                }
                else if (key == "MoveEnabled" && bool.TryParse(value, out parsedBoolean))
                {
                    Plan.MoveEnabled = parsedBoolean;
                }
                else if (key == "SoundEnabled" && bool.TryParse(value, out parsedBoolean))
                {
                    SoundEnabled = parsedBoolean;
                }
                else if (key == "AlwaysOnTop" && bool.TryParse(value, out parsedBoolean))
                {
                    AlwaysOnTop = parsedBoolean;
                }
            }
        }

        public void Save()
        {
            string directory = Path.GetDirectoryName(SettingsPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            string[] lines =
            {
                "SitMinutes=" + Plan.SitMinutes,
                "StandMinutes=" + Plan.StandMinutes,
                "MoveMinutes=" + Plan.MoveMinutes,
                "MoveEnabled=" + Plan.MoveEnabled,
                "SoundEnabled=" + SoundEnabled,
                "AlwaysOnTop=" + AlwaysOnTop
            };

            File.WriteAllLines(SettingsPath, lines);
        }

        private static int ClampMinutes(int value)
        {
            return Math.Max(1, Math.Min(180, value));
        }
    }

    internal sealed class CueForm : Form
    {
        private readonly Timer closeTimer;

        public CueForm(string title, string detail, Color color)
        {
            Text = "Ctrl+Alt+Stand reminder";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            TopMost = true;
            BackColor = color;
            ClientSize = new Size(440, 180);
            Padding = new Padding(20);

            TableLayoutPanel layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 1;
            layout.RowCount = 3;
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 27));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 15));

            Label titleLabel = new Label();
            titleLabel.Text = title;
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.TextAlign = ContentAlignment.BottomCenter;
            titleLabel.Font = new Font("Segoe UI Semibold", 31F, FontStyle.Bold);
            titleLabel.ForeColor = Color.White;

            Label detailLabel = new Label();
            detailLabel.Text = detail;
            detailLabel.Dock = DockStyle.Fill;
            detailLabel.TextAlign = ContentAlignment.MiddleCenter;
            detailLabel.Font = new Font("Segoe UI", 12F, FontStyle.Regular);
            detailLabel.ForeColor = Color.White;

            Label dismissLabel = new Label();
            dismissLabel.Text = "Click to dismiss";
            dismissLabel.Dock = DockStyle.Fill;
            dismissLabel.TextAlign = ContentAlignment.BottomRight;
            dismissLabel.Font = new Font("Segoe UI", 8F);
            dismissLabel.ForeColor = Color.FromArgb(225, 255, 255, 255);

            layout.Controls.Add(titleLabel, 0, 0);
            layout.Controls.Add(detailLabel, 0, 1);
            layout.Controls.Add(dismissLabel, 0, 2);
            Controls.Add(layout);

            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 24, area.Bottom - Height - 24);

            EventHandler dismiss = delegate { Close(); };
            Click += dismiss;
            layout.Click += dismiss;
            titleLabel.Click += dismiss;
            detailLabel.Click += dismiss;
            dismissLabel.Click += dismiss;

            closeTimer = new Timer();
            closeTimer.Interval = 8000;
            closeTimer.Tick += delegate
            {
                closeTimer.Stop();
                Close();
            };
            closeTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                closeTimer.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    internal sealed class MainForm : Form
    {
        private static readonly Color WindowColor = Color.FromArgb(15, 23, 42);
        private static readonly Color SurfaceColor = Color.FromArgb(30, 41, 59);
        private static readonly Color MutedTextColor = Color.FromArgb(148, 163, 184);
        private static readonly Color SitColor = Color.FromArgb(37, 99, 235);
        private static readonly Color StandColor = Color.FromArgb(5, 150, 105);
        private static readonly Color MoveColor = Color.FromArgb(217, 119, 6);

        private readonly AppSettings settings;
        private readonly Timer clock;
        private readonly NotifyIcon trayIcon;
        private readonly Label phaseLabel;
        private readonly Label clockLabel;
        private readonly Label statusLabel;
        private readonly Label nextLabel;
        private readonly Panel progressTrack;
        private readonly Panel progressFill;
        private readonly Button startPauseButton;
        private readonly NumericUpDown sitInput;
        private readonly NumericUpDown standInput;
        private readonly NumericUpDown moveInput;
        private readonly CheckBox moveEnabledInput;
        private readonly CheckBox soundInput;
        private readonly CheckBox topMostInput;

        private DeskPhase phase = DeskPhase.Sit;
        private bool running;
        private DateTime phaseEndsAt;
        private TimeSpan pausedRemaining;
        private int phaseTotalSeconds;

        public MainForm(AppSettings appSettings)
        {
            settings = appSettings;
            settings.Load();

            AutoScaleMode = AutoScaleMode.Dpi;
            AutoScaleDimensions = new SizeF(96F, 96F);
            Text = "Ctrl+Alt+Stand";
            Icon = SystemIcons.Information;
            BackColor = WindowColor;
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(470, 760);
            MinimumSize = new Size(486, 799);
            MaximumSize = new Size(486, 799);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = settings.AlwaysOnTop;
            Padding = new Padding(20, 16, 20, 16);

            Rectangle area = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(area.Right - Width - 24, area.Top + 24);

            TableLayoutPanel root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.ColumnCount = 1;
            root.RowCount = 5;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 75));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 310));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            Panel header = new Panel();
            header.Dock = DockStyle.Fill;
            Label appName = new Label();
            appName.Text = "Ctrl+Alt+Stand";
            appName.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold);
            appName.ForeColor = Color.White;
            appName.AutoSize = true;
            appName.Location = new Point(0, 0);
            Label subtitle = new Label();
            subtitle.Text = "Your standing-desk routine starts when you do";
            subtitle.Font = new Font("Segoe UI", 9F);
            subtitle.ForeColor = MutedTextColor;
            subtitle.AutoSize = true;
            subtitle.Location = new Point(2, 45);
            header.Controls.Add(appName);
            header.Controls.Add(subtitle);

            Panel phaseCard = new Panel();
            phaseCard.Dock = DockStyle.Fill;
            phaseCard.BackColor = SurfaceColor;
            phaseCard.Padding = new Padding(18);

            TableLayoutPanel phaseLayout = new TableLayoutPanel();
            phaseLayout.Dock = DockStyle.Fill;
            phaseLayout.ColumnCount = 1;
            phaseLayout.RowCount = 5;
            phaseLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            phaseLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            phaseLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 35));
            phaseLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 10));
            phaseLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            phaseLabel = new Label();
            phaseLabel.Dock = DockStyle.Fill;
            phaseLabel.TextAlign = ContentAlignment.MiddleCenter;
            phaseLabel.Font = new Font("Segoe UI Semibold", 33F, FontStyle.Bold);

            clockLabel = new Label();
            clockLabel.Dock = DockStyle.Fill;
            clockLabel.TextAlign = ContentAlignment.MiddleCenter;
            clockLabel.Font = new Font("Segoe UI", 40F, FontStyle.Regular);
            clockLabel.ForeColor = Color.White;

            statusLabel = new Label();
            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleCenter;
            statusLabel.Font = new Font("Segoe UI Semibold", 10F);
            statusLabel.ForeColor = MutedTextColor;

            progressTrack = new Panel();
            progressTrack.Dock = DockStyle.Fill;
            progressTrack.BackColor = Color.FromArgb(51, 65, 85);
            progressFill = new Panel();
            progressFill.Dock = DockStyle.Left;
            progressTrack.Controls.Add(progressFill);

            nextLabel = new Label();
            nextLabel.Dock = DockStyle.Fill;
            nextLabel.TextAlign = ContentAlignment.BottomCenter;
            nextLabel.Font = new Font("Segoe UI", 9F);
            nextLabel.ForeColor = MutedTextColor;

            phaseLayout.Controls.Add(phaseLabel, 0, 0);
            phaseLayout.Controls.Add(clockLabel, 0, 1);
            phaseLayout.Controls.Add(statusLabel, 0, 2);
            phaseLayout.Controls.Add(progressTrack, 0, 3);
            phaseLayout.Controls.Add(nextLabel, 0, 4);
            phaseCard.Controls.Add(phaseLayout);

            TableLayoutPanel buttons = new TableLayoutPanel();
            buttons.Dock = DockStyle.Fill;
            buttons.ColumnCount = 3;
            buttons.RowCount = 1;
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27.5F));
            buttons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 27.5F));
            buttons.Padding = new Padding(0, 8, 0, 4);

            startPauseButton = MakeButton("Start", SitColor);
            Button skipButton = MakeButton("Skip", Color.FromArgb(71, 85, 105));
            Button resetButton = MakeButton("Reset", Color.FromArgb(71, 85, 105));
            startPauseButton.Click += delegate { ToggleRunning(); };
            skipButton.Click += delegate { SkipPhase(true); };
            resetButton.Click += delegate { ResetCycle(); };
            buttons.Controls.Add(startPauseButton, 0, 0);
            buttons.Controls.Add(skipButton, 1, 0);
            buttons.Controls.Add(resetButton, 2, 0);

            GroupBox settingsBox = new GroupBox();
            settingsBox.Text = "  Schedule  ";
            settingsBox.Dock = DockStyle.Fill;
            settingsBox.ForeColor = Color.White;
            settingsBox.Padding = new Padding(12, 8, 12, 10);

            TableLayoutPanel settingsLayout = new TableLayoutPanel();
            settingsLayout.Dock = DockStyle.Fill;
            settingsLayout.ColumnCount = 3;
            settingsLayout.RowCount = 3;
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33F));
            settingsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34F));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            settingsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            sitInput = MakeDurationInput(settings.Plan.SitMinutes);
            standInput = MakeDurationInput(settings.Plan.StandMinutes);
            moveInput = MakeDurationInput(settings.Plan.MoveMinutes);
            settingsLayout.Controls.Add(MakeDurationPanel("SIT", sitInput, SitColor), 0, 0);
            settingsLayout.Controls.Add(MakeDurationPanel("STAND", standInput, StandColor), 1, 0);
            settingsLayout.Controls.Add(MakeDurationPanel("MOVE", moveInput, MoveColor), 2, 0);

            moveEnabledInput = MakeCheckBox("Include movement break", settings.Plan.MoveEnabled);
            soundInput = MakeCheckBox("Sound", settings.SoundEnabled);
            topMostInput = MakeCheckBox("Always on top", settings.AlwaysOnTop);
            settingsLayout.Controls.Add(moveEnabledInput, 0, 1);
            settingsLayout.SetColumnSpan(moveEnabledInput, 2);
            settingsLayout.Controls.Add(soundInput, 2, 1);
            settingsLayout.Controls.Add(topMostInput, 0, 2);
            settingsLayout.SetColumnSpan(topMostInput, 2);
            Label applyHint = new Label();
            applyHint.Text = "Changes apply next phase";
            applyHint.ForeColor = MutedTextColor;
            applyHint.Dock = DockStyle.Fill;
            applyHint.TextAlign = ContentAlignment.MiddleRight;
            applyHint.Font = new Font("Segoe UI", 8F);
            settingsLayout.Controls.Add(applyHint, 2, 2);
            settingsBox.Controls.Add(settingsLayout);

            Label footer = new Label();
            footer.Text = "Tip: movement helps circulation more than standing still.";
            footer.Dock = DockStyle.Fill;
            footer.TextAlign = ContentAlignment.MiddleCenter;
            footer.ForeColor = MutedTextColor;
            footer.Font = new Font("Segoe UI", 8.5F);

            root.Controls.Add(header, 0, 0);
            root.Controls.Add(phaseCard, 0, 1);
            root.Controls.Add(buttons, 0, 2);
            root.Controls.Add(settingsBox, 0, 3);
            root.Controls.Add(footer, 0, 4);
            Controls.Add(root);

            trayIcon = new NotifyIcon();
            trayIcon.Icon = SystemIcons.Information;
            trayIcon.Text = "Ctrl+Alt+Stand";
            trayIcon.Visible = true;
            trayIcon.DoubleClick += delegate { RestoreWindow(); };
            trayIcon.ContextMenuStrip = BuildTrayMenu();

            clock = new Timer();
            clock.Interval = 250;
            clock.Tick += delegate { TickClock(); };
            clock.Start();

            sitInput.ValueChanged += SettingsChanged;
            standInput.ValueChanged += SettingsChanged;
            moveInput.ValueChanged += SettingsChanged;
            moveEnabledInput.CheckedChanged += SettingsChanged;
            soundInput.CheckedChanged += SettingsChanged;
            topMostInput.CheckedChanged += SettingsChanged;

            moveInput.Enabled = settings.Plan.MoveEnabled;
            pausedRemaining = TimeSpan.FromSeconds(settings.Plan.SecondsFor(phase));
            phaseTotalSeconds = settings.Plan.SecondsFor(phase);
            UpdateDisplay();
        }

        public void BeginSmokeTest()
        {
            Timer smokeTimer = new Timer();
            smokeTimer.Interval = 900;
            smokeTimer.Tick += delegate
            {
                smokeTimer.Stop();
                smokeTimer.Dispose();
                Close();
            };
            smokeTimer.Start();
        }

        private static Button MakeButton(string text, Color color)
        {
            Button button = new Button();
            button.Text = text;
            button.Dock = DockStyle.Fill;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = color;
            button.ForeColor = Color.White;
            button.Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold);
            button.Margin = new Padding(3);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private static NumericUpDown MakeDurationInput(int value)
        {
            NumericUpDown input = new NumericUpDown();
            input.Minimum = 1;
            input.Maximum = 180;
            input.Value = value;
            input.Width = 68;
            input.Font = new Font("Segoe UI Semibold", 12F, FontStyle.Bold);
            input.TextAlign = HorizontalAlignment.Center;
            input.BackColor = Color.FromArgb(15, 23, 42);
            input.ForeColor = Color.White;
            input.BorderStyle = BorderStyle.FixedSingle;
            return input;
        }

        private static Panel MakeDurationPanel(string label, NumericUpDown input, Color color)
        {
            Panel panel = new Panel();
            panel.Dock = DockStyle.Fill;
            Label title = new Label();
            title.Text = label;
            title.ForeColor = color;
            title.Font = new Font("Segoe UI Semibold", 8F, FontStyle.Bold);
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Dock = DockStyle.Top;
            title.Height = 19;
            input.Location = new Point(27, 25);
            Label minutes = new Label();
            minutes.Text = "min";
            minutes.ForeColor = MutedTextColor;
            minutes.AutoSize = true;
            minutes.Location = new Point(96, 31);
            panel.Controls.Add(input);
            panel.Controls.Add(minutes);
            panel.Controls.Add(title);
            return panel;
        }

        private static CheckBox MakeCheckBox(string text, bool isChecked)
        {
            CheckBox checkBox = new CheckBox();
            checkBox.Text = text;
            checkBox.Checked = isChecked;
            checkBox.Dock = DockStyle.Fill;
            checkBox.ForeColor = Color.White;
            checkBox.FlatStyle = FlatStyle.Flat;
            checkBox.AutoSize = true;
            return checkBox;
        }

        private ContextMenuStrip BuildTrayMenu()
        {
            ContextMenuStrip menu = new ContextMenuStrip();
            ToolStripMenuItem showItem = new ToolStripMenuItem("Show Ctrl+Alt+Stand");
            ToolStripMenuItem toggleItem = new ToolStripMenuItem("Start / pause");
            ToolStripMenuItem skipItem = new ToolStripMenuItem("Skip phase");
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            showItem.Click += delegate { RestoreWindow(); };
            toggleItem.Click += delegate { ToggleRunning(); };
            skipItem.Click += delegate { SkipPhase(true); };
            exitItem.Click += delegate { Close(); };
            menu.Items.Add(showItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(toggleItem);
            menu.Items.Add(skipItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);
            return menu;
        }

        private void ToggleRunning()
        {
            if (running)
            {
                TimeSpan remaining = phaseEndsAt - DateTime.UtcNow;
                pausedRemaining = remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
                running = false;
            }
            else
            {
                if (pausedRemaining <= TimeSpan.Zero)
                {
                    phaseTotalSeconds = settings.Plan.SecondsFor(phase);
                    pausedRemaining = TimeSpan.FromSeconds(phaseTotalSeconds);
                }
                phaseEndsAt = DateTime.UtcNow.Add(pausedRemaining);
                running = true;
            }

            UpdateDisplay();
        }

        private void ResetCycle()
        {
            running = false;
            phase = DeskPhase.Sit;
            phaseTotalSeconds = settings.Plan.SecondsFor(phase);
            pausedRemaining = TimeSpan.FromSeconds(phaseTotalSeconds);
            UpdateDisplay();
        }

        private void SkipPhase(bool announce)
        {
            phase = settings.Plan.Next(phase);
            phaseTotalSeconds = settings.Plan.SecondsFor(phase);
            pausedRemaining = TimeSpan.FromSeconds(phaseTotalSeconds);
            if (running)
            {
                phaseEndsAt = DateTime.UtcNow.Add(pausedRemaining);
            }
            if (announce)
            {
                AnnouncePhase();
            }
            UpdateDisplay();
        }

        private void TickClock()
        {
            if (running)
            {
                TimeSpan remaining = phaseEndsAt - DateTime.UtcNow;
                if (remaining <= TimeSpan.Zero)
                {
                    SkipPhase(true);
                    return;
                }
                pausedRemaining = remaining;
            }
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            Color phaseColor = ColorForPhase(phase);
            string phaseName = NameForPhase(phase);
            phaseLabel.Text = phaseName.ToUpperInvariant();
            phaseLabel.ForeColor = phaseColor;
            progressFill.BackColor = phaseColor;
            startPauseButton.BackColor = phaseColor;
            startPauseButton.Text = running ? "Pause" : "Start";
            statusLabel.Text = running ? "CYCLE RUNNING" : "PAUSED — PRESS START";
            statusLabel.ForeColor = running ? phaseColor : MutedTextColor;

            int totalSeconds = Math.Max(1, (int)Math.Ceiling(pausedRemaining.TotalSeconds));
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            clockLabel.Text = hours > 0
                ? string.Format("{0}:{1:00}:{2:00}", hours, minutes, seconds)
                : string.Format("{0:00}:{1:00}", minutes, seconds);

            DeskPhase next = settings.Plan.Next(phase);
            nextLabel.Text = "Next: " + NameForPhase(next) + " for " +
                settings.Plan.SecondsFor(next) / 60 + " min";

            double elapsedRatio = 1.0 - Math.Min(1.0, pausedRemaining.TotalSeconds / Math.Max(1, phaseTotalSeconds));
            int availableWidth = progressTrack.ClientSize.Width;
            progressFill.Width = Math.Max(0, Math.Min(availableWidth, (int)(availableWidth * elapsedRatio)));
        }

        private void AnnouncePhase()
        {
            string phaseName = NameForPhase(phase);
            string title = "TIME TO " + phaseName.ToUpperInvariant();
            string detail;

            if (phase == DeskPhase.Stand)
            {
                detail = "Raise the desk and change position.";
            }
            else if (phase == DeskPhase.Move)
            {
                detail = "Walk, march, or do a few calf raises.";
            }
            else
            {
                detail = "Lower the desk and sit comfortably.";
            }

            if (settings.SoundEnabled)
            {
                SystemSounds.Exclamation.Play();
            }

            trayIcon.BalloonTipTitle = "Ctrl+Alt+Stand — " + phaseName;
            trayIcon.BalloonTipText = detail;
            trayIcon.BalloonTipIcon = ToolTipIcon.Info;
            trayIcon.ShowBalloonTip(5000);

            CueForm cue = new CueForm(title, detail, ColorForPhase(phase));
            cue.Show();
            FlashTaskbar();
        }

        private void SettingsChanged(object sender, EventArgs e)
        {
            settings.Plan.SitMinutes = (int)sitInput.Value;
            settings.Plan.StandMinutes = (int)standInput.Value;
            settings.Plan.MoveMinutes = (int)moveInput.Value;
            settings.Plan.MoveEnabled = moveEnabledInput.Checked;
            settings.SoundEnabled = soundInput.Checked;
            settings.AlwaysOnTop = topMostInput.Checked;
            moveInput.Enabled = settings.Plan.MoveEnabled;
            TopMost = settings.AlwaysOnTop;

            try
            {
                settings.Save();
            }
            catch
            {
                // A settings write failure should never stop the active timer.
            }

            UpdateDisplay();
        }

        private void RestoreWindow()
        {
            if (!Visible)
            {
                Show();
            }
            WindowState = FormWindowState.Normal;
            Activate();
            BringToFront();
        }

        private static string NameForPhase(DeskPhase value)
        {
            if (value == DeskPhase.Stand)
            {
                return "Stand";
            }
            if (value == DeskPhase.Move)
            {
                return "Move";
            }
            return "Sit";
        }

        private static Color ColorForPhase(DeskPhase value)
        {
            if (value == DeskPhase.Stand)
            {
                return StandColor;
            }
            if (value == DeskPhase.Move)
            {
                return MoveColor;
            }
            return SitColor;
        }

        private void FlashTaskbar()
        {
            FLASHWINFO info = new FLASHWINFO();
            info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
            info.hwnd = Handle;
            info.dwFlags = 2 | 12;
            info.uCount = 4;
            info.dwTimeout = 0;
            FlashWindowEx(ref info);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            clock.Stop();
            trayIcon.Visible = false;
            trayIcon.Dispose();
            clock.Dispose();
            base.OnFormClosed(e);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FLASHWINFO
        {
            public uint cbSize;
            public IntPtr hwnd;
            public uint dwFlags;
            public uint uCount;
            public uint dwTimeout;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FlashWindowEx(ref FLASHWINFO info);
    }

    internal static class SelfTests
    {
        public static bool Run()
        {
            CyclePlan plan = new CyclePlan();
            if (plan.SecondsFor(DeskPhase.Sit) != 1800) return false;
            if (plan.SecondsFor(DeskPhase.Stand) != 1200) return false;
            if (plan.SecondsFor(DeskPhase.Move) != 180) return false;
            if (plan.Next(DeskPhase.Sit) != DeskPhase.Stand) return false;
            if (plan.Next(DeskPhase.Stand) != DeskPhase.Move) return false;
            if (plan.Next(DeskPhase.Move) != DeskPhase.Sit) return false;
            plan.MoveEnabled = false;
            if (plan.Next(DeskPhase.Stand) != DeskPhase.Sit) return false;
            return true;
        }
    }

    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            if (Array.Exists(args, delegate(string value)
                { return string.Equals(value, "--self-test", StringComparison.OrdinalIgnoreCase); }))
            {
                return SelfTests.Run() ? 0 : 1;
            }

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                MainForm form = new MainForm(new AppSettings());
                if (Array.Exists(args, delegate(string value)
                    { return string.Equals(value, "--smoke-test", StringComparison.OrdinalIgnoreCase); }))
                {
                    form.Opacity = 0;
                    form.ShowInTaskbar = false;
                    form.BeginSmokeTest();
                }
                Application.Run(form);
                return 0;
            }
            catch (Exception error)
            {
                try
                {
                    string directory = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "CtrlAltStand");
                    Directory.CreateDirectory(directory);
                    File.AppendAllText(
                        Path.Combine(directory, "error.log"),
                        DateTime.Now.ToString("s") + Environment.NewLine + error + Environment.NewLine);
                }
                catch
                {
                }
                return 1;
            }
        }
    }
}
