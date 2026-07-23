using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace CtrlAltStand
{
    public partial class MainWindow : Window
    {
        private readonly AppSettings settings = new AppSettings();
        private readonly DispatcherTimer clock;
        private readonly DispatcherTimer profileArmTimer;
        private WinForms.NotifyIcon trayIcon;

        private DeskPhase phase = DeskPhase.Sit;
        private bool running;
        private bool saveProfileArmed;
        private bool updatingControls;
        private DateTime phaseEndsAt;
        private TimeSpan pausedRemaining;
        private int phaseTotalSeconds;
        private DeskPhase? accentPhase;
        private CueWindow currentCue;

        private const double RingR = 95, RingCx = 107, RingCy = 107;

        public MainWindow(bool smoke)
        {
            InitializeComponent();

            settings.Load();
            Topmost = settings.AlwaysOnTop;

            // Reflect settings into controls without triggering handlers.
            updatingControls = true;
            SitValue.Text = settings.Plan.SitMinutes.ToString();
            StandValue.Text = settings.Plan.StandMinutes.ToString();
            MoveValue.Text = settings.Plan.MoveMinutes.ToString();
            MoveSwitch.IsChecked = settings.Plan.MoveEnabled;
            SoundSwitch.IsChecked = settings.SoundEnabled;
            TopSwitch.IsChecked = settings.AlwaysOnTop;
            if (settings.Plan.StartPhase == DeskPhase.Stand) StandSeg.IsChecked = true; else SitSeg.IsChecked = true;
            MoveTile.IsEnabled = settings.Plan.MoveEnabled;
            MoveTile.Opacity = settings.Plan.MoveEnabled ? 1.0 : 0.45;
            updatingControls = false;

            clock = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            clock.Tick += (s, e) => TickClock();
            clock.Start();

            profileArmTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(6000) };
            profileArmTimer.Tick += (s, e) =>
            {
                DisarmProfileSave();
                MemHint.Text = "Save cancelled; choose Set to try again";
            };

            SetupTray();

            phase = settings.Plan.StartPhase;
            phaseTotalSeconds = settings.Plan.SecondsFor(phase);
            pausedRemaining = TimeSpan.FromSeconds(phaseTotalSeconds);
            UpdateDisplay();

            if (smoke)
            {
                Opacity = 0;
                ShowInTaskbar = false;
                DispatcherTimer smokeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(900) };
                smokeTimer.Tick += (s, e) => { smokeTimer.Stop(); Close(); };
                smokeTimer.Start();
            }
        }

        // ---------- Window placement / chrome ----------

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Rect wa = SystemParameters.WorkArea;

            // Auto-fit: if the window is taller (or wider) than the available work area
            // — small display, or high DPI scaling — shrink the whole card to fit.
            double availH = wa.Height * 0.98;
            double availW = wa.Width * 0.98;
            if (ActualHeight > availH || ActualWidth > availW)
            {
                double s = Math.Min(availH / ActualHeight, availW / ActualWidth);
                RootScale.ScaleX = s;
                RootScale.ScaleY = s;
                UpdateLayout();
            }

            Left = wa.Right - ActualWidth;   // dock to the top-right of the work area
            Top = wa.Top;
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) DragMove();
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) { WindowState = WindowState.Minimized; }
        private void Close_Click(object sender, RoutedEventArgs e) { Close(); }

        // ---------- Transport ----------

        private void Start_Click(object sender, RoutedEventArgs e) { ToggleRunning(); }
        private void Skip_Click(object sender, RoutedEventArgs e) { SkipPhase(true); }
        private void Reset_Click(object sender, RoutedEventArgs e) { ResetCycle(); }

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
            phase = settings.Plan.StartPhase;
            phaseTotalSeconds = settings.Plan.SecondsFor(phase);
            pausedRemaining = TimeSpan.FromSeconds(phaseTotalSeconds);
            UpdateDisplay();
        }

        private void SkipPhase(bool announce)
        {
            phase = settings.Plan.Next(phase);
            phaseTotalSeconds = settings.Plan.SecondsFor(phase);
            pausedRemaining = TimeSpan.FromSeconds(phaseTotalSeconds);
            if (running) phaseEndsAt = DateTime.UtcNow.Add(pausedRemaining);
            if (announce) AnnouncePhase();
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

        // ---------- Settings (steppers / toggles / segmented) ----------

        private void SitMinus_Click(object s, RoutedEventArgs e) { Adjust(DeskPhase.Sit, -1); }
        private void SitPlus_Click(object s, RoutedEventArgs e) { Adjust(DeskPhase.Sit, +1); }
        private void StandMinus_Click(object s, RoutedEventArgs e) { Adjust(DeskPhase.Stand, -1); }
        private void StandPlus_Click(object s, RoutedEventArgs e) { Adjust(DeskPhase.Stand, +1); }
        private void MoveMinus_Click(object s, RoutedEventArgs e) { Adjust(DeskPhase.Move, -1); }
        private void MovePlus_Click(object s, RoutedEventArgs e) { Adjust(DeskPhase.Move, +1); }

        private static int Clamp(int v) { return Math.Max(1, Math.Min(180, v)); }

        private void Adjust(DeskPhase p, int delta)
        {
            if (updatingControls) return;
            switch (p)
            {
                case DeskPhase.Sit:
                    settings.Plan.SitMinutes = Clamp(settings.Plan.SitMinutes + delta);
                    SitValue.Text = settings.Plan.SitMinutes.ToString();
                    break;
                case DeskPhase.Stand:
                    settings.Plan.StandMinutes = Clamp(settings.Plan.StandMinutes + delta);
                    StandValue.Text = settings.Plan.StandMinutes.ToString();
                    break;
                case DeskPhase.Move:
                    settings.Plan.MoveMinutes = Clamp(settings.Plan.MoveMinutes + delta);
                    MoveValue.Text = settings.Plan.MoveMinutes.ToString();
                    break;
            }
            TrySaveSettings();
            UpdateDisplay();
        }

        private void Setting_Toggled(object sender, RoutedEventArgs e) { ApplySettingsFromControls(); }

        private void ApplySettingsFromControls()
        {
            if (updatingControls) return;

            DeskPhase selectedStart = StandSeg.IsChecked == true ? DeskPhase.Stand : DeskPhase.Sit;
            bool startPhaseChanged = settings.Plan.StartPhase != selectedStart;

            settings.Plan.MoveEnabled = MoveSwitch.IsChecked == true;
            settings.Plan.StartPhase = selectedStart;
            settings.SoundEnabled = SoundSwitch.IsChecked == true;
            settings.AlwaysOnTop = TopSwitch.IsChecked == true;

            MoveTile.IsEnabled = settings.Plan.MoveEnabled;
            MoveTile.Opacity = settings.Plan.MoveEnabled ? 1.0 : 0.45;
            Topmost = settings.AlwaysOnTop;

            if (startPhaseChanged && !running)
            {
                phase = settings.Plan.StartPhase;
                phaseTotalSeconds = settings.Plan.SecondsFor(phase);
                pausedRemaining = TimeSpan.FromSeconds(phaseTotalSeconds);
            }

            TrySaveSettings();
            UpdateDisplay();
        }

        // ---------- Memory ----------

        private void Set_Click(object sender, RoutedEventArgs e) { ArmProfileSave(); }
        private void Profile1_Click(object sender, RoutedEventArgs e) { ProfileButtonPressed(1); }
        private void Profile2_Click(object sender, RoutedEventArgs e) { ProfileButtonPressed(2); }
        private void Defaults_Click(object sender, RoutedEventArgs e) { RestoreDefaults(); }

        private void ArmProfileSave()
        {
            saveProfileArmed = true;
            profileArmTimer.Stop();
            profileArmTimer.Start();
            SetChip.Background = (Brush)Resources["AccentSoftBrush"] ?? SetChip.Background;
            SetChip.BorderBrush = (Brush)Resources["AccentBrush"] ?? SetChip.BorderBrush;
            SetChip.Foreground = (Brush)Resources["AccentBrush"] ?? SetChip.Foreground;
            MemHint.Text = "Press 1 or 2 to save the current schedule";
        }

        private void DisarmProfileSave()
        {
            saveProfileArmed = false;
            profileArmTimer.Stop();
            SetChip.Background = (Brush)FindResource("Surface2Brush");
            SetChip.BorderBrush = (Brush)FindResource("StrokeBrush");
            SetChip.Foreground = (Brush)FindResource("TextDimBrush");
        }

        private void ProfileButtonPressed(int slot)
        {
            if (saveProfileArmed)
            {
                DisarmProfileSave();
                settings.SaveProfile(slot);
                TrySaveSettings();
                MemHint.Text = "Memory " + slot + " saved";
                return;
            }

            CyclePlan profile = settings.GetProfile(slot);
            if (profile == null)
            {
                MemHint.Text = "Memory " + slot + " is empty — choose Set first";
                return;
            }

            ApplyPlanAndReset(profile);
            MemHint.Text = "Memory " + slot + " loaded";
        }

        private void RestoreDefaults()
        {
            DisarmProfileSave();
            ApplyPlanAndReset(new CyclePlan());
            MemHint.Text = "Default schedule restored";
        }

        private void ApplyPlanAndReset(CyclePlan plan)
        {
            updatingControls = true;
            settings.Plan.SitMinutes = plan.SitMinutes;
            settings.Plan.StandMinutes = plan.StandMinutes;
            settings.Plan.MoveMinutes = plan.MoveMinutes;
            settings.Plan.MoveEnabled = plan.MoveEnabled;
            settings.Plan.StartPhase = plan.StartPhase;

            SitValue.Text = plan.SitMinutes.ToString();
            StandValue.Text = plan.StandMinutes.ToString();
            MoveValue.Text = plan.MoveMinutes.ToString();
            MoveSwitch.IsChecked = plan.MoveEnabled;
            if (plan.StartPhase == DeskPhase.Stand) StandSeg.IsChecked = true; else SitSeg.IsChecked = true;
            MoveTile.IsEnabled = plan.MoveEnabled;
            MoveTile.Opacity = plan.MoveEnabled ? 1.0 : 0.45;
            updatingControls = false;

            TrySaveSettings();
            ResetCycle();
        }

        private void TrySaveSettings()
        {
            try { settings.Save(); }
            catch { /* a settings write failure must never stop the active timer */ }
        }

        // ---------- Rendering ----------

        private void UpdateDisplay()
        {
            Color accent = ColorForPhase(phase);
            if (accentPhase != phase)
            {
                ApplyAccent(accent);
                accentPhase = phase;
            }

            PhaseName.Text = NameForPhase(phase).ToUpperInvariant();

            int secs = Math.Max(0, (int)Math.Ceiling(pausedRemaining.TotalSeconds));
            TimerText.Text = SelfTests.FormatClock(secs);

            StartButton.Content = running ? "Pause" : "Start";

            if (running)
            {
                StatusText.Text = "CYCLE RUNNING";
                StatusText.Foreground = new SolidColorBrush(accent);
                StatusPill.Background = TintBrush(accent, 0.16);
                StatusPill.BorderBrush = TintBrush(accent, 0.40);
                StatusDot.Fill = new SolidColorBrush(accent);
            }
            else
            {
                StatusText.Text = "PAUSED · PRESS START";
                StatusText.Foreground = (Brush)FindResource("TextDimBrush");
                StatusPill.Background = new SolidColorBrush(Color.FromArgb(0x1F, 0xA3, 0xB1, 0xC6));
                StatusPill.BorderBrush = (Brush)FindResource("StrokeBrush");
                StatusDot.Fill = (Brush)FindResource("TextFaintBrush");
            }

            DeskPhase next = settings.Plan.Next(phase);
            NextText.Text = "Next · " + NameForPhase(next) + " " + settings.Plan.SecondsFor(next) / 60 + "m";

            double frac = pausedRemaining.TotalSeconds / Math.Max(1, phaseTotalSeconds);
            if (frac < 0) frac = 0; else if (frac > 1) frac = 1;
            RingProgress.Data = BuildRing(frac);
        }

        // Accent-follows-phase: swap the dynamic brushes + code-driven accents.
        private void ApplyAccent(Color c)
        {
            Resources["AccentBrush"] = new SolidColorBrush(c);
            Resources["AccentSoftBrush"] = new SolidColorBrush(Color.FromArgb(0x33, c.R, c.G, c.B));

            RingGlow.Color = c;

            StartButton.Background = new LinearGradientBrush(c, Darken(c, 0.78), new Point(0, 0), new Point(0, 1));

            RadialGradientBrush wash = new RadialGradientBrush
            {
                GradientOrigin = new Point(0.5, -0.05),
                Center = new Point(0.5, -0.05),
                RadiusX = 0.75,
                RadiusY = 0.45
            };
            wash.GradientStops.Add(new GradientStop(Color.FromArgb(0x73, c.R, c.G, c.B), 0));
            wash.GradientStops.Add(new GradientStop(Color.FromArgb(0x00, c.R, c.G, c.B), 1));
            Wash.Fill = wash;
        }

        private Geometry BuildRing(double frac)
        {
            if (frac <= 0.0001) return Geometry.Empty;
            if (frac >= 0.9999) return new EllipseGeometry(new Point(RingCx, RingCy), RingR, RingR);

            double sweep = frac * 360.0;
            double theta = sweep * Math.PI / 180.0;
            Point start = new Point(RingCx, RingCy - RingR);
            Point end = new Point(RingCx + RingR * Math.Sin(theta), RingCy - RingR * Math.Cos(theta));

            PathFigure fig = new PathFigure { StartPoint = start, IsClosed = false, IsFilled = false };
            fig.Segments.Add(new ArcSegment(end, new Size(RingR, RingR), 0, sweep > 180.0, SweepDirection.Clockwise, true));
            PathGeometry g = new PathGeometry();
            g.Figures.Add(fig);
            return g;
        }

        private static SolidColorBrush TintBrush(Color c, double alpha)
        {
            return new SolidColorBrush(Color.FromArgb((byte)(alpha * 255), c.R, c.G, c.B));
        }

        private static Color Darken(Color c, double factor)
        {
            return Color.FromRgb((byte)(c.R * factor), (byte)(c.G * factor), (byte)(c.B * factor));
        }

        private static string NameForPhase(DeskPhase v)
        {
            if (v == DeskPhase.Stand) return "Stand";
            if (v == DeskPhase.Move) return "Move";
            return "Sit";
        }

        private static Color ColorForPhase(DeskPhase v)
        {
            if (v == DeskPhase.Stand) return Color.FromRgb(0x10, 0xB9, 0x81);
            if (v == DeskPhase.Move) return Color.FromRgb(0xF5, 0x9E, 0x0B);
            return Color.FromRgb(0x3B, 0x82, 0xF6);
        }

        // ---------- Transition cue / tray / flash / sound ----------

        private void AnnouncePhase()
        {
            string phaseName = NameForPhase(phase);
            string title = "TIME TO " + phaseName.ToUpperInvariant();
            string detail;
            if (phase == DeskPhase.Stand) detail = "Raise the desk and change position.";
            else if (phase == DeskPhase.Move) detail = "Walk, march, or do a few calf raises.";
            else detail = "Lower the desk and sit comfortably.";

            if (settings.SoundEnabled)
            {
                try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
            }

            if (trayIcon != null)
            {
                trayIcon.BalloonTipTitle = "Ctrl+Alt+Stand — " + phaseName;
                trayIcon.BalloonTipText = detail;
                trayIcon.BalloonTipIcon = WinForms.ToolTipIcon.Info;
                trayIcon.ShowBalloonTip(5000);
            }

            // Persistent cue: keep at most one on screen; a newer phase replaces the older one.
            if (currentCue != null)
            {
                try { currentCue.Close(); } catch { }
                currentCue = null;
            }
            currentCue = new CueWindow(title, detail, ColorForPhase(phase));
            currentCue.Closed += delegate { currentCue = null; };
            currentCue.Show();

            FlashTaskbar();
        }

        private void SetupTray()
        {
            trayIcon = new WinForms.NotifyIcon
            {
                Icon = Drawing.SystemIcons.Information,
                Text = "Ctrl+Alt+Stand",
                Visible = true
            };
            trayIcon.DoubleClick += (s, e) => RestoreWindow();

            WinForms.ContextMenuStrip menu = new WinForms.ContextMenuStrip();
            WinForms.ToolStripMenuItem show = new WinForms.ToolStripMenuItem("Show Ctrl+Alt+Stand");
            WinForms.ToolStripMenuItem toggle = new WinForms.ToolStripMenuItem("Start / pause");
            WinForms.ToolStripMenuItem skip = new WinForms.ToolStripMenuItem("Skip phase");
            WinForms.ToolStripMenuItem exit = new WinForms.ToolStripMenuItem("Exit");
            show.Click += (s, e) => RestoreWindow();
            toggle.Click += (s, e) => ToggleRunning();
            skip.Click += (s, e) => SkipPhase(true);
            exit.Click += (s, e) => Close();
            menu.Items.Add(show);
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add(toggle);
            menu.Items.Add(skip);
            menu.Items.Add(new WinForms.ToolStripSeparator());
            menu.Items.Add(exit);
            trayIcon.ContextMenuStrip = menu;
        }

        private void RestoreWindow()
        {
            if (!IsVisible) Show();
            WindowState = WindowState.Normal;
            Activate();
        }

        private void FlashTaskbar()
        {
            FLASHWINFO info = new FLASHWINFO();
            info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
            info.hwnd = new WindowInteropHelper(this).Handle;
            info.dwFlags = 3 | 12;          // FLASHW_ALL (caption+tray) | FLASHW_TIMERNOFG (until foreground)
            info.uCount = uint.MaxValue;    // keep flashing until the window is brought to the foreground
            info.dwTimeout = 0;
            FlashWindowEx(ref info);
        }

        protected override void OnClosed(EventArgs e)
        {
            clock.Stop();
            profileArmTimer.Stop();
            if (currentCue != null)
            {
                try { currentCue.Close(); } catch { }
                currentCue = null;
            }
            if (trayIcon != null)
            {
                trayIcon.Visible = false;
                trayIcon.Dispose();
                trayIcon = null;
            }
            base.OnClosed(e);
            System.Windows.Application.Current.Shutdown();
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
}
