using System;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Drawing;

namespace planer
{
    /// <summary>
    /// Main form that displays current status, remaining time and the daily schedule.
    /// The form exposes methods used by the TimeCyclerApplicationContext to update the UI
    /// from the background timer thread in a thread-safe manner.
    /// </summary>
    public partial class Form1 : Form
    {
        private List<(TimeSpan start, TimeSpan end, string label)> scheduleEntries = new List<(TimeSpan, TimeSpan, string)>();

        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Update status label and remaining time on the form. This method is thread-safe and
        /// will marshal calls to the UI thread when needed.
        /// </summary>
        public void UpdateStatus(string status, string remaining)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(status, remaining)));
                return;
            }

            var statusCtrls = this.Controls.Find("lblStatus", true);
            if (statusCtrls.Length > 0 && statusCtrls[0] is Label lblStatus)
            {
                lblStatus.Text = $"Status: {status}";
            }

            var remainingCtrls = this.Controls.Find("lblRemaining", true);
            if (remainingCtrls.Length > 0 && remainingCtrls[0] is Label lblRemaining)
            {
                lblRemaining.Text = $"Pozosta³o: {remaining}";
            }
        }

        /// <summary>
        /// Set the schedule text displayed in the schedule box. The method also parses the
        /// textual lines into internal schedule entries for highlighting logic.
        /// </summary>
        public void SetSchedule(string scheduleText)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetSchedule(scheduleText)));
                return;
            }

            var ctrls = this.Controls.Find("txtSchedule", true);
            if (ctrls.Length > 0 && ctrls[0] is RichTextBox rtb)
            {
                rtb.Text = scheduleText;
            }

            // parse lines into scheduleEntries
            scheduleEntries.Clear();
            var lines = scheduleText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                // expected format: HH:mm - HH:mm label
                try
                {
                    var parts = line.Split(new[] { ' ' }, 4, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4 && parts[0].Length == 5 && parts[2].Length == 5)
                    {
                        var start = TimeSpan.Parse(parts[0]);
                        var end = TimeSpan.Parse(parts[2]);
                        var label = parts[3];
                        scheduleEntries.Add((start, end, label));
                    }
                }
                catch
                {
                    // ignore parsing errors - schedule display is best-effort
                }
            }
        }

        /// <summary>
        /// Highlight the current schedule line based on the provided time. The highlight is
        /// applied to the RichTextBox selection background. The method is thread-safe.
        /// </summary>
        public void HighlightCurrentPeriod(DateTime now)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => HighlightCurrentPeriod(now)));
                return;
            }

            if (scheduleEntries.Count == 0) return;

            var t = now.TimeOfDay;

            int charIndex = 0;
            var ctrls = this.Controls.Find("txtSchedule", true);
            if (ctrls.Length > 0 && ctrls[0] is RichTextBox rtb)
            {
                // reset selection background for whole text
                rtb.SelectAll();
                rtb.SelectionBackColor = rtb.BackColor;

                // iterate through the parsed schedule entries and color the matching line
                foreach (var entry in scheduleEntries)
                {
                    string line = $"{entry.start:hh\\:mm} - {entry.end:hh\\:mm} {entry.label}" + Environment.NewLine;
                    int len = line.Length;
                    rtb.Select(charIndex, len);
                    if (IsTimeInRange(t, entry.start, entry.end))
                    {
                        rtb.SelectionBackColor = Color.LightGreen;
                    }
                    else
                    {
                        rtb.SelectionBackColor = rtb.BackColor;
                    }

                    charIndex += len;
                }

                rtb.Select(0, 0);
            }
        }

        /// <summary>
        /// Returns true when 'now' falls within [start,end). Supports ranges that cross midnight.
        /// </summary>
        private bool IsTimeInRange(TimeSpan now, TimeSpan start, TimeSpan end)
        {
            if (end > start)
            {
                return now >= start && now < end;
            }
            else
            {
                // range crosses midnight
                return now >= start || now < end;
            }
        }

        /// <summary>
        /// Toggle visibility of developer audio test buttons. Thread-safe.
        /// </summary>
        public void SetDeveloperModeVisible(bool visible)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetDeveloperModeVisible(visible)));
                return;
            }

            var btns = new[] { "btnPlayEndOfWork", "btnPlayWorkEndingSoon", "btnPlayStartBreak", "btnPlayBreakEndingSoon", "btnPlayStartWork" };
            foreach (var name in btns)
            {
                var ctrls = this.Controls.Find(name, true);
                if (ctrls.Length > 0 && ctrls[0] is Button b)
                {
                    b.Visible = visible;
                }
            }
        }

        /// <summary>
        /// Update UI texts that depend on selected language.
        /// </summary>
        public void SetLanguageLabels()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => SetLanguageLabels()));
                return;
            }

            var statusCtrls = this.Controls.Find("lblStatus", true);
            if (statusCtrls.Length > 0 && statusCtrls[0] is Label lblStatus)
            {
                lblStatus.Text = $"{Localizer.Get("StatusPrefix")} -";
            }

            var remainingCtrls = this.Controls.Find("lblRemaining", true);
            if (remainingCtrls.Length > 0 && remainingCtrls[0] is Label lblRemaining)
            {
                lblRemaining.Text = $"{Localizer.Get("RemainingPrefix")} -";
            }

            // update developer buttons labels
            var map = new[] {
                ("btnPlayEndOfWork","Play EndOfWork"),
                ("btnPlayWorkEndingSoon","Play WorkEndingSoon"),
                ("btnPlayStartBreak","Play StartBreak"),
                ("btnPlayBreakEndingSoon","Play BreakEndingSoon"),
                ("btnPlayStartWork","Play StartWork") };

            foreach (var (name, def) in map)
            {
                var ctrls = this.Controls.Find(name, true);
                if (ctrls.Length > 0 && ctrls[0] is Button b)
                {
                    b.Text = def; // developer labels kept in English for now
                }
            }
        }

        // Click handlers for developer test buttons; they raise a static event consumed by the ApplicationContext.
        private void btnPlayEndOfWork_Click(object sender, EventArgs e) => DevBridge?.Invoke(DevAction.EndOfWork);
        private void btnPlayWorkEndingSoon_Click(object sender, EventArgs e) => DevBridge?.Invoke(DevAction.WorkEndingSoon);
        private void btnPlayStartBreak_Click(object sender, EventArgs e) => DevBridge?.Invoke(DevAction.StartBreak);
        private void btnPlayBreakEndingSoon_Click(object sender, EventArgs e) => DevBridge?.Invoke(DevAction.BreakEndingSoon);
        private void btnPlayStartWork_Click(object sender, EventArgs e) => DevBridge?.Invoke(DevAction.StartWork);

        /// <summary>
        /// Static event used as a small bridge to relay developer button clicks into the application context.
        /// </summary>
        public static event Action<DevAction>? DevBridge;

        public enum DevAction
        {
            EndOfWork,
            WorkEndingSoon,
            StartBreak,
            BreakEndingSoon,
            StartWork
        }
    }
}
