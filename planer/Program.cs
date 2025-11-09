using System.Text.Json;
using System.Media;
using System.Diagnostics;
using NAudio.Wave;
using Microsoft.Toolkit.Uwp.Notifications; // ToastContentBuilder
using planer;

/// <summary>
/// ApplicationContext that manages the time-cycling logic, notifications and tray UI.
/// The context runs the main timer, computes work/break periods, plays configured sounds
/// and shows system notifications. Configuration is stored in config.json in the
/// application folder and supports custom durations and sound file paths.
/// </summary>
public class TimeCyclerApplicationContext : ApplicationContext
{
    // --- Cycle duration values (configured via config.json) ---
    private int _workDurationSeconds;  // work period length in seconds
    private int _breakDurationSeconds; // break period length in seconds
    private int _cycleDurationSeconds; // total cycle length in seconds (work + break)

    // --- UI and system components ---
    private NotifyIcon trayIcon; // system tray icon
    private System.Windows.Forms.Timer updateTimer;   // main timer running every second
    private ContextMenuStrip contextMenu; // tray context menu
    private ToolStripMenuItem exitMenuItem; // exit menu item
    private ToolStripMenuItem openMenuItem; // open main window menu item
    private ToolStripMenuItem editConfigMenuItem; // edit config menu item
    private ToolStripMenuItem reloadConfigMenuItem; // reload config menu item

    // --- Runtime state ---
    private string previousStatus = "";
    private string currentStatus = "";
    private int secondsRemaining = 0;

    // Main window instance (created on demand)
    private planer.Form1 mainForm;

    // Configuration and paths
    private AppConfig config;
    private string baseDir;
    private string configPath;

    /// <summary>
    /// Initializes the application context: loads configuration, creates tray icon and menu,
    /// starts the timer and initializes the main window instance (hidden by default).
    /// </summary>
    public TimeCyclerApplicationContext()
    {
        baseDir = AppDomain.CurrentDomain.BaseDirectory;
        LoadOrCreateConfig();

        // Create tray context menu
        contextMenu = new ContextMenuStrip();
        openMenuItem = new ToolStripMenuItem(Localizer.Get("OpenMenu"));
        openMenuItem.Click += OnOpenRequested;
        editConfigMenuItem = new ToolStripMenuItem(Localizer.Get("EditConfig"));
        editConfigMenuItem.Click += OnEditConfig;
        reloadConfigMenuItem = new ToolStripMenuItem(Localizer.Get("ReloadConfig"));
        reloadConfigMenuItem.Click += OnReloadConfig;
        exitMenuItem = new ToolStripMenuItem(Localizer.Get("Exit"));
        exitMenuItem.Click += OnExit;
        contextMenu.Items.Add(openMenuItem);
        contextMenu.Items.Add(editConfigMenuItem);
        contextMenu.Items.Add(reloadConfigMenuItem);
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add(exitMenuItem);

        // Create tray icon
        trayIcon = new NotifyIcon();
        trayIcon.Text = Localizer.Get("TrayLoading");
        trayIcon.ContextMenuStrip = contextMenu;
        trayIcon.Visible = true;

        // Show main window on left-click
        trayIcon.MouseClick += TrayIcon_MouseClick;

        // Create a simple 16x16 blue icon so we don't require an external .ico file
        using (Bitmap bmp = new Bitmap(16, 16))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Blue);
            trayIcon.Icon = Icon.FromHandle(bmp.GetHicon());
        }

        // Initialize main form (kept hidden until user opens it)
        mainForm = new planer.Form1();
        planer.Form1.DevBridge += OnDevActionRequested; // subscribe developer button bridge
        mainForm.SetDeveloperModeVisible(config.DevMode);
        mainForm.SetLanguageLabels();

        // Create and start the main timer
        updateTimer = new System.Windows.Forms.Timer();
        updateTimer.Interval = 1000; // 1 second
        updateTimer.Tick += OnTimerTick;
        updateTimer.Start();

        // Run one immediate tick to initialize state
        OnTimerTick(null, null);
    }

    /// <summary>
    /// Handler for developer UI actions; plays configured sounds for the selected action.
    /// </summary>
    private void OnDevActionRequested(planer.Form1.DevAction action)
    {
        switch (action)
        {
            case planer.Form1.DevAction.EndOfWork:
                PlaySoundIfExists(config.Sounds.EndOfWork);
                break;
            case planer.Form1.DevAction.WorkEndingSoon:
                PlaySoundIfExists(config.Sounds.WorkEndingSoon);
                break;
            case planer.Form1.DevAction.StartBreak:
                PlaySoundIfExists(config.Sounds.StartBreak);
                break;
            case planer.Form1.DevAction.BreakEndingSoon:
                PlaySoundIfExists(config.Sounds.BreakEndingSoon);
                break;
            case planer.Form1.DevAction.StartWork:
                PlaySoundIfExists(config.Sounds.StartWork);
                break;
        }
    }

    /// <summary>
    /// Opens the configuration file (config.json) in the system default editor.
    /// If the config file doesn't exist, opens the application folder instead.
    /// </summary>
    private void OnEditConfig(object? sender, EventArgs e)
    {
        try
        {
            if (File.Exists(configPath))
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = configPath,
                    UseShellExecute = true
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo()
                {
                    FileName = baseDir,
                    UseShellExecute = true
                });
            }
        }
        catch
        {
            // Show a simple balloon tip on error (toast fallback may not be available yet)
            trayIcon.ShowBalloonTip(2000, "Error", "Unable to open configuration file.", ToolTipIcon.Error);
        }
    }

    /// <summary>
    /// Reloads the configuration from disk and updates runtime state and UI accordingly.
    /// </summary>
    private void OnReloadConfig(object? sender, EventArgs e)
    {
        try
        {
            LoadOrCreateConfig();
            // Update dev buttons visibility
            if (mainForm != null && !mainForm.IsDisposed)
            {
                mainForm.SetDeveloperModeVisible(config.DevMode);
                mainForm.SetLanguageLabels();
            }

            // Update menu and tray labels to possibly changed language
            if (openMenuItem != null) openMenuItem.Text = Localizer.Get("OpenMenu");
            if (editConfigMenuItem != null) editConfigMenuItem.Text = Localizer.Get("EditConfig");
            if (reloadConfigMenuItem != null) reloadConfigMenuItem.Text = Localizer.Get("ReloadConfig");
            if (exitMenuItem != null) exitMenuItem.Text = Localizer.Get("Exit");
            if (trayIcon != null) trayIcon.Text = Localizer.Get("TrayLoading");

            trayIcon.ShowBalloonTip(2000, Localizer.Get("ConfigurationReloaded"), $"{config.WorkMinutes}m work, {config.BreakMinutes}m break. DevMode={config.DevMode}", ToolTipIcon.Info);
        }
        catch
        {
            trayIcon.ShowBalloonTip(2000, "Error", "Failed to reload configuration.", ToolTipIcon.Error);
        }
    }

    /// <summary>
    /// Loads config.json from the application directory or creates a default configuration
    /// and sample sound files if none exist.
    /// </summary>
    private void LoadOrCreateConfig()
    {
        configPath = Path.Combine(baseDir, "config.json");
        string soundsDir = Path.Combine(baseDir, "sounds");

        if (!Directory.Exists(soundsDir)) Directory.CreateDirectory(soundsDir);

        if (!File.Exists(configPath))
        {
            // create default configuration
            config = new AppConfig
            {
                WorkMinutes = 90,
                BreakMinutes = 30,
                DevMode = false,
                Sounds = new SoundConfig
                {
                    EndOfWork = Path.Combine("sounds", "end_of_work.wav"),
                    WorkEndingSoon = Path.Combine("sounds", "work_ending_soon.wav"),
                    StartBreak = Path.Combine("sounds", "start_break.wav"),
                    BreakEndingSoon = Path.Combine("sounds", "break_ending_soon.wav"),
                    StartWork = Path.Combine("sounds", "start_work.wav")
                },
                Notifications = new NotificationConfig
                {
                    StartBreakTitle = "Now break",
                    StartBreakMessage = "Enjoy your break",
                    StartWorkTitle = "Now work",
                    StartWorkMessage = "Back to work"
                }
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configPath, json);

            // generate simple WAVs for defaults
            GenerateSineWav(Path.Combine(baseDir, config.Sounds.EndOfWork), 523, 1.0);
            GenerateSineWav(Path.Combine(baseDir, config.Sounds.WorkEndingSoon), 784, 0.8);
            GenerateSineWav(Path.Combine(baseDir, config.Sounds.StartBreak), 659, 1.0);
            GenerateSineWav(Path.Combine(baseDir, config.Sounds.BreakEndingSoon), 880, 0.8);
            GenerateSineWav(Path.Combine(baseDir, config.Sounds.StartWork), 440, 1.0);
        }
        else
        {
            try
            {
                var json = File.ReadAllText(configPath);
                config = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig { WorkMinutes = 90, BreakMinutes = 30, DevMode = false, Sounds = new SoundConfig(), Notifications = new NotificationConfig() };
            }
            catch
            {
                // fall back to defaults on error
                config = new AppConfig { WorkMinutes = 90, BreakMinutes = 30, DevMode = false, Sounds = new SoundConfig(), Notifications = new NotificationConfig() };
            }
        }

        // Set language in localizer
        Localizer.Language = config.Language ?? "en";

        // convert minutes to seconds and update derived values
        _workDurationSeconds = Math.Max(1, config.WorkMinutes) * 60;
        _breakDurationSeconds = Math.Max(1, config.BreakMinutes) * 60;
        _cycleDurationSeconds = _workDurationSeconds + _breakDurationSeconds;

        // ensure default sound files exist
        EnsureDefaultSoundExists(config.Sounds.EndOfWork, 523);
        EnsureDefaultSoundExists(config.Sounds.WorkEndingSoon, 784);
        EnsureDefaultSoundExists(config.Sounds.StartBreak, 659);
        EnsureDefaultSoundExists(config.Sounds.BreakEndingSoon, 880);
        EnsureDefaultSoundExists(config.Sounds.StartWork, 440);

        // refresh the schedule shown in the UI
        UpdateScheduleUI();
    }

    /// <summary>
    /// Ensure a default WAV exists at the specified relative path; if missing generate a simple tone.
    /// </summary>
    private void EnsureDefaultSoundExists(string relativePath, int freq)
    {
        string full = Path.Combine(baseDir, relativePath);
        if (!File.Exists(full))
        {
            try
            {
                GenerateSineWav(full, freq, 0.9);
            }
            catch
            {
                // ignore generation errors
            }
        }
    }

    /// <summary>
    /// Generate a simple mono 16-bit PCM WAV file containing a sine tone.
    /// This is used only as a default sound when no sound file is provided by the user.
    /// </summary>
    private void GenerateSineWav(string path, int freq, double seconds)
    {
        int sampleRate = 44100;
        short bitsPerSample = 16;
        int channels = 1;
        int samples = (int)(sampleRate * seconds);
        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            int blockAlign = channels * bitsPerSample / 8;

            // RIFF header
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + samples * blockAlign);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // fmt chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1); // PCM
            bw.Write((short)channels);
            bw.Write(sampleRate);
            bw.Write(byteRate);
            bw.Write((short)blockAlign);
            bw.Write((short)bitsPerSample);

            // data chunk
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(samples * blockAlign);

            double amplitude = 0.25 * short.MaxValue;
            double twoPiF = 2 * Math.PI * freq;
            for (int n = 0; n < samples; n++)
            {
                double t = (double)n / sampleRate;
                short val = (short)(amplitude * Math.Sin(twoPiF * t));
                bw.Write(val);
            }
        }
    }

    /// <summary>
    /// Tray icon left-click handler - show the main window.
    /// </summary>
    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ShowMainWindow();
        }
    }

    /// <summary>
    /// Open or focus the main window.
    /// </summary>
    private void OnOpenRequested(object sender, EventArgs e)
    {
        ShowMainWindow();
    }

    /// <summary>
    /// Ensure main window instance exists and bring it to foreground.
    /// </summary>
    private void ShowMainWindow()
    {
        if (mainForm == null || mainForm.IsDisposed)
        {
            mainForm = new planer.Form1();
        }

        if (!mainForm.Visible)
        {
            mainForm.Show();
        }

        mainForm.WindowState = FormWindowState.Normal;
        mainForm.BringToFront();
        mainForm.Activate();
    }

    /// <summary>
    /// Main timer tick fired every second. Computes the current phase (work/break), remaining time,
    /// updates the tray text and UI, triggers sounds and notifications at configured thresholds.
    /// </summary>
    private void OnTimerTick(object sender, EventArgs e)
    {
        DateTime now = DateTime.Now;
        int totalSecondsInDay = (now.Hour * 3600) + (now.Minute * 60) + now.Second;
        int secondsIntoCycle = totalSecondsInDay % _cycleDurationSeconds;

        if (secondsIntoCycle < _workDurationSeconds)
        {
            currentStatus = "Praca";
            secondsRemaining = _workDurationSeconds - secondsIntoCycle;
        }
        else
        {
            currentStatus = "Przerwa";
            secondsRemaining = _cycleDurationSeconds - secondsIntoCycle;
        }

        TimeSpan remainingTimeSpan = TimeSpan.FromSeconds(secondsRemaining);
        string timeString = $"{remainingTimeSpan.Minutes:D2}:{remainingTimeSpan.Seconds:D2}";

        trayIcon.Text = $"Status: {currentStatus} (Pozosta³o: {timeString})";

        try
        {
            if (mainForm != null && !mainForm.IsDisposed)
            {
                mainForm.UpdateStatus(currentStatus, timeString);
                mainForm.HighlightCurrentPeriod(now);
            }
        }
        catch
        {
            // ignore UI update errors
        }

        const int fiveMinutes = 5 * 60;

        if (currentStatus == "Praca" && secondsRemaining == fiveMinutes)
        {
            PlaySoundIfExists(config.Sounds.WorkEndingSoon);
        }

        if (currentStatus == "Przerwa" && previousStatus == "Praca")
        {
            ShowNotification(config.Notifications.StartBreakTitle, config.Notifications.StartBreakMessage);
            PlaySoundIfExists(config.Sounds.EndOfWork);
            PlaySoundIfExists(config.Sounds.StartBreak);
        }

        if (currentStatus == "Przerwa" && secondsRemaining == fiveMinutes)
        {
            PlaySoundIfExists(config.Sounds.BreakEndingSoon);
        }

        if (currentStatus == "Praca" && previousStatus == "Przerwa")
        {
            ShowNotification(config.Notifications.StartWorkTitle, config.Notifications.StartWorkMessage);
            PlaySoundIfExists(config.Sounds.StartWork);
        }

        previousStatus = currentStatus;
    }

    /// <summary>
    /// Plays the specified audio file if it exists. Supports WAV and MP3 via NAudio and falls back to SoundPlayer for WAV.
    /// Playback is executed on a background task to avoid blocking the timer thread.
    /// </summary>
    private void PlaySoundIfExists(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) return;
        string full = Path.Combine(baseDir, relativePath);
        if (!File.Exists(full))
        {
            trayIcon.ShowBalloonTip(2000, Localizer.Get("MissingSoundFileTitle"), $"Missing file: {relativePath}", ToolTipIcon.Warning);
            return;
        }

        Task.Run(() => {
            try
            {
                using (var reader = new AudioFileReader(full))
                using (var output = new WaveOutEvent())
                {
                    var tcs = new TaskCompletionSource<bool>();
                    output.Init(reader);
                    output.PlaybackStopped += (s, e) => tcs.TrySetResult(true);
                    output.Play();
                    tcs.Task.Wait();
                }
            }
            catch
            {
                try
                {
                    using (var sp = new SoundPlayer(full))
                    {
                        sp.PlaySync();
                    }
                }
                catch
                {
                    // ignore playback errors
                }
            }
        });
    }

    /// <summary>
    /// Displays a Windows toast notification using the Microsoft.Toolkit.Uwp.Notifications
    /// library. Falls back to a tray balloon tip if toast cannot be shown.
    /// </summary>
    private void ShowNotification(string title, string message)
    {
        try
        {
            var builder = new ToastContentBuilder()
                .AddText(title)
                .AddText(message);

            dynamic dyn = builder;
            try
            {
                dyn.Show();
                return;
            }
            catch
            {
                // fall back
            }
        }
        catch
        {
            // ignored
        }

        try
        {
            trayIcon.ShowBalloonTip(3000, title, message, ToolTipIcon.Info);
        }
        catch
        {
            // ignore
        }
    }

    /// <summary>
    /// Cleanly exit the application and dispose resources.
    /// </summary>
    private void OnExit(object sender, EventArgs e)
    {
        updateTimer.Stop();
        trayIcon.Visible = false;

        if (mainForm != null && !mainForm.IsDisposed)
        {
            mainForm.FormClosing -= mainForm_FormClosing;
            mainForm.Close();
            mainForm.Dispose();
        }

        trayIcon.Dispose();
        Application.Exit();
    }

    private void mainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        // reserved for future shutdown handling
    }

    /// <summary>
    /// Builds a textual schedule that enumerates work and break periods starting at 00:00
    /// and repeating until the end of the day. The returned string is suitable for displaying
    /// in the main window schedule box.
    /// </summary>
    private string BuildScheduleText()
    {
        var lines = new System.Text.StringBuilder();

        TimeSpan start = TimeSpan.Zero;
        int cycleSec = _cycleDurationSeconds;
        int workSec = _workDurationSeconds;
        int breakSec = _breakDurationSeconds;

        int maxCycles = (24 * 3600) / cycleSec + 1;
        TimeSpan cur = start;

        for (int i = 0; i < maxCycles; i++)
        {
            TimeSpan workEnd = cur.Add(TimeSpan.FromSeconds(workSec));
            lines.AppendLine($"{cur:hh\\:mm} - {workEnd:hh\\:mm} {Localizer.Get("WorkLabel")}");

            TimeSpan breakEnd = workEnd.Add(TimeSpan.FromSeconds(breakSec));
            lines.AppendLine($"{workEnd:hh\\:mm} - {breakEnd:hh\\:mm} {Localizer.Get("BreakLabel")}");

            cur = breakEnd;
            if (cur.TotalSeconds >= 24 * 3600) break;
        }

        return lines.ToString();
    }

    /// <summary>
    /// Pushes the current schedule to the main form.
    /// </summary>
    private void UpdateScheduleUI()
    {
        try
        {
            if (mainForm != null && !mainForm.IsDisposed)
            {
                var scheduleText = BuildScheduleText();
                mainForm.SetSchedule(scheduleText);
            }
        }
        catch
        {
            // ignore UI errors
        }
    }

    // Configuration types
    private class AppConfig
    {
        public int WorkMinutes { get; set; }
        public int BreakMinutes { get; set; }
        public bool DevMode { get; set; } = false;
        public string? Language { get; set; } = "en";
        public SoundConfig Sounds { get; set; } = new SoundConfig();
        public NotificationConfig Notifications { get; set; } = new NotificationConfig();
    }

    private class SoundConfig
    {
        public string EndOfWork { get; set; } = string.Empty;
        public string WorkEndingSoon { get; set; } = string.Empty;
        public string StartBreak { get; set; } = string.Empty;
        public string BreakEndingSoon { get; set; } = string.Empty;
        public string StartWork { get; set; } = string.Empty;
    }

    private class NotificationConfig
    {
        public string StartBreakTitle { get; set; } = "Now break";
        public string StartBreakMessage { get; set; } = "Enjoy your break";
        public string StartWorkTitle { get; set; } = "Now work";
        public string StartWorkMessage { get; set; } = "Back to work";
    }
}

/// <summary>
/// Program entry point for the WinForms application. Initializes application-wide visual
/// styles and runs the custom ApplicationContext so the program can operate without
/// a visible main window initially (tray-based).
/// </summary>
public class Program
{
    [STAThread]
    public static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        Application.Run(new TimeCyclerApplicationContext());
    }
}