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

            // Bottom-right of the primary working area, 24px inset.
            Rect wa = SystemParameters.WorkArea;
            Left = wa.Right - Width - 24;
            Top = wa.Bottom - Height - 24;

            MouseLeftButtonDown += (s, e) => Close();
        }
    }
}
