using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ZipZip.ShellExtension;

internal sealed class ShellExtractProgressForm : Form
{
    private readonly Label _overallPercentLabel;
    private readonly Label _overallTimeLabel;
    private readonly ProgressBar _overallProgressBar;
    private readonly Label _currentPercentLabel;
    private readonly Label _currentTimeLabel;
    private readonly ProgressBar _currentProgressBar;
    private readonly Label _archiveLabel;
    private readonly Label _currentFileLabel;
    private readonly Label _statusLabel;
    private readonly Button _openFolderButton;
    private readonly Button _closeButton;
    private readonly System.Windows.Forms.Timer _timer;
    private readonly Stopwatch _stopwatch = new();
    private readonly string _archiveName;
    private bool _isCompleted;
    private bool _isCancellationRequested;
    private int _lastPercent;
    private string? _completedFolderPath;

    public ShellExtractProgressForm(string archivePath)
    {
        _archiveName = Path.GetFileName(archivePath);

        Text = $"\uC555\uCD95 \uD480\uAE30 {_archiveName} - ZipZip";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = true;
        AutoScaleMode = AutoScaleMode.Dpi;
        ClientSize = new Size(820, 350);
        MinimumSize = new Size(836, 389);
        Font = SystemFonts.MessageBoxFont;
        BackColor = SystemColors.Window;

        _overallPercentLabel = new Label
        {
            AutoSize = false,
            Font = new Font(Font.FontFamily, 17f, FontStyle.Bold),
            Text = "0%",
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(16, 18),
            Size = new Size(160, 32),
        };
        _overallTimeLabel = new Label
        {
            AutoSize = false,
            Text = "00:00:00 / 00:00:00",
            TextAlign = ContentAlignment.MiddleRight,
            Location = new Point(588, 24),
            Size = new Size(216, 20),
        };
        _overallProgressBar = new ProgressBar
        {
            Location = new Point(16, 56),
            Size = new Size(788, 28),
            Style = ProgressBarStyle.Continuous,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
        };
        _currentPercentLabel = new Label
        {
            AutoSize = false,
            Font = new Font(Font.FontFamily, 12f, FontStyle.Bold),
            Text = "0%",
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(16, 102),
            Size = new Size(160, 24),
        };
        _currentTimeLabel = new Label
        {
            AutoSize = false,
            Text = "00:00:00 / 00:00:00",
            TextAlign = ContentAlignment.MiddleRight,
            Location = new Point(588, 104),
            Size = new Size(216, 20),
        };
        _currentProgressBar = new ProgressBar
        {
            Location = new Point(16, 132),
            Size = new Size(788, 28),
            Style = ProgressBarStyle.Continuous,
            Minimum = 0,
            Maximum = 100,
            Value = 0,
        };
        _archiveLabel = new Label
        {
            AutoSize = false,
            Font = new Font(Font, FontStyle.Bold),
            Text = _archiveName,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(16, 178),
            Size = new Size(788, 22),
        };
        _currentFileLabel = new Label
        {
            AutoSize = false,
            Text = "\uC900\uBE44 \uC911...",
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Location = new Point(16, 204),
            Size = new Size(788, 24),
        };
        _statusLabel = new Label
        {
            AutoSize = false,
            BorderStyle = BorderStyle.FixedSingle,
            Text = "\uC555\uCD95 \uD480\uAE30\uB97C \uC2DC\uC791\uD569\uB2C8\uB2E4.",
            TextAlign = ContentAlignment.MiddleCenter,
            BackColor = SystemColors.Window,
            Location = new Point(16, 236),
            Size = new Size(788, 66),
        };
        _openFolderButton = new Button
        {
            Enabled = false,
            Text = "\uD3F4\uB354 \uC5F4\uAE30",
            Size = new Size(112, 34),
            Location = new Point(576, 312),
        };
        _closeButton = new Button
        {
            Text = "\uCDE8\uC18C",
            Size = new Size(112, 34),
            Location = new Point(692, 312),
        };

        _openFolderButton.Click += OnOpenFolderClick;
        _closeButton.Click += OnCloseClick;

        Controls.AddRange(
        [
            _overallPercentLabel,
            _overallTimeLabel,
            _overallProgressBar,
            _currentPercentLabel,
            _currentTimeLabel,
            _currentProgressBar,
            _archiveLabel,
            _currentFileLabel,
            _statusLabel,
            _openFolderButton,
            _closeButton,
        ]);

        _timer = new System.Windows.Forms.Timer
        {
            Interval = 200,
        };
        _timer.Tick += (_, _) => UpdateTimeLabels();

        Load += (_, _) =>
        {
            _stopwatch.Start();
            _timer.Start();
            UpdateCaption(0);
            UpdateTimeLabels();
        };
    }

    public event EventHandler? CancelRequested;

    public int ExitCode { get; private set; } = 1;

    public void ApplyProgress(ShellExtractProgressUpdate update)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => ApplyProgress(update)));
            }
            catch
            {
            }

            return;
        }

        if (update.Percent is int percent)
        {
            _lastPercent = Math.Clamp(percent, 0, 100);
            _overallProgressBar.Value = _lastPercent;
            _currentProgressBar.Value = _lastPercent;
            _overallPercentLabel.Text = $"{_lastPercent}%";
            _currentPercentLabel.Text = $"{_lastPercent}%";
            UpdateCaption(_lastPercent);
            UpdateTimeLabels();
        }

        if (!string.IsNullOrWhiteSpace(update.CurrentFile))
        {
            _currentFileLabel.Text = update.CurrentFile;
        }

        if (!string.IsNullOrWhiteSpace(update.StatusText))
        {
            _statusLabel.Text = update.StatusText;
        }
    }

    public void CompleteSuccess(string completedFolderPath)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => CompleteSuccess(completedFolderPath)));
            }
            catch
            {
            }

            return;
        }

        _isCompleted = true;
        _lastPercent = 100;
        ExitCode = 0;
        _completedFolderPath = completedFolderPath;
        _timer.Stop();
        _stopwatch.Stop();
        _overallProgressBar.Value = 100;
        _currentProgressBar.Value = 100;
        _overallPercentLabel.Text = "100%";
        _currentPercentLabel.Text = "100%";
        _statusLabel.ForeColor = SystemColors.ControlText;
        _statusLabel.Text = "\uC555\uCD95 \uD480\uAE30\uC5D0 \uC131\uACF5\uD558\uC600\uC2B5\uB2C8\uB2E4.";
        _openFolderButton.Enabled = Directory.Exists(completedFolderPath);
        _closeButton.Text = "\uB2EB\uAE30";
        UpdateCaption(100);
        UpdateTimeLabels();
    }

    public void CompleteFailure(string message)
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => CompleteFailure(message)));
            }
            catch
            {
            }

            return;
        }

        _isCompleted = true;
        ExitCode = 1;
        _timer.Stop();
        _stopwatch.Stop();
        _statusLabel.ForeColor = Color.DarkRed;
        _statusLabel.Text = string.IsNullOrWhiteSpace(message)
            ? "\uC555\uCD95 \uD480\uAE30 \uC911 \uC624\uB958\uAC00 \uBC1C\uC0DD\uD588\uC2B5\uB2C8\uB2E4."
            : message;
        _closeButton.Text = "\uB2EB\uAE30";
        UpdateTimeLabels();
    }

    public void CompleteCanceled()
    {
        if (IsDisposed || Disposing)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(CompleteCanceled));
            }
            catch
            {
            }

            return;
        }

        _isCompleted = true;
        ExitCode = 1;
        _timer.Stop();
        _stopwatch.Stop();
        _statusLabel.ForeColor = SystemColors.ControlText;
        _statusLabel.Text = "\uC791\uC5C5\uC744 \uCDE8\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
        _closeButton.Text = "\uB2EB\uAE30";
        BeginInvoke(new Action(Close));
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_isCompleted && !_isCancellationRequested)
        {
            e.Cancel = true;
            RequestCancel();
            return;
        }

        base.OnFormClosing(e);
    }

    private void UpdateCaption(int percent)
    {
        Text = $"{percent}% \uC555\uCD95 \uD480\uAE30 {_archiveName} - ZipZip";
    }

    private void UpdateTimeLabels()
    {
        var elapsed = _stopwatch.Elapsed;
        var estimatedTotal = elapsed;

        if (_lastPercent > 0 && _lastPercent < 100)
        {
            estimatedTotal = TimeSpan.FromTicks(elapsed.Ticks * 100 / _lastPercent);
        }

        var timeText = $"{elapsed:hh\\:mm\\:ss} / {estimatedTotal:hh\\:mm\\:ss}";
        _overallTimeLabel.Text = timeText;
        _currentTimeLabel.Text = timeText;
    }

    private void OnOpenFolderClick(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_completedFolderPath) || !Directory.Exists(_completedFolderPath))
        {
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = _completedFolderPath,
            UseShellExecute = true,
        });
    }

    private void OnCloseClick(object? sender, EventArgs e)
    {
        if (_isCompleted)
        {
            Close();
            return;
        }

        RequestCancel();
    }

    private void RequestCancel()
    {
        if (_isCancellationRequested)
        {
            return;
        }

        _isCancellationRequested = true;
        _closeButton.Enabled = false;
        _statusLabel.ForeColor = SystemColors.ControlText;
        _statusLabel.Text = "\uCDE8\uC18C\uD558\uB294 \uC911...";
        CancelRequested?.Invoke(this, EventArgs.Empty);
    }
}
