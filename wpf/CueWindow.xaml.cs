using System;
using System.Windows;
using System.Windows.Media;

namespace CtrlAltStand
{
    public partial class CueWindow : Window
    {
        // The cue stays on screen until the user acknowledges it (clicks it) — it does not auto-close.
        public CueWindow(string title, string detail, Color color)
        {
            InitializeComponent();

            TitleText.Text = title;
            DetailText.Text = detail;
            Root.Background = new SolidColorBrush(color);

            // Size is dynamic (SizeToContent), so place the window once it has measured.
            Loaded += (s, e) => PositionTopRight();

            MouseLeftButtonDown += (s, e) => Close();
        }

        // Top-right of the primary working area, 24px inset. If the cue would fully cover
        // the main app window (which also docks top-right), nudge it down so both stay visible.
        private void PositionTopRight()
        {
            Rect wa = SystemParameters.WorkArea;
            Left = wa.Right - ActualWidth - 24;
            Top = wa.Top + 24;

            Window app = System.Windows.Application.Current != null
                ? System.Windows.Application.Current.MainWindow
                : null;
            if (app != null && app != this && app.IsVisible && app.WindowState == WindowState.Normal
                && app.ActualWidth > 0 && app.ActualHeight > 0)
            {
                Rect cue = new Rect(Left, Top, ActualWidth, ActualHeight);
                Rect main = new Rect(app.Left, app.Top, app.ActualWidth, app.ActualHeight);
                Rect overlap = Rect.Intersect(cue, main);
                // The app also docks top-right, so the smaller cue lands right on top of it.
                // If the cue is essentially covered by the app, drop it just below the app —
                // as long as there's room to keep it fully on screen.
                bool covered = !overlap.IsEmpty
                    && overlap.Width >= cue.Width - 1
                    && overlap.Height >= cue.Height - 1;
                if (covered)
                {
                    double below = app.Top + app.ActualHeight + 12;
                    if (below + ActualHeight + 12 <= wa.Bottom) Top = below;
                }
            }
        }
    }
}
