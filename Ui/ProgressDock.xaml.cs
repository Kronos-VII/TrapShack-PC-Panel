using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Windows.Controls;

namespace TrapShack.Ui;

public enum StepResult { Done, Warn, Failed }

/// <summary>
/// The panel that slides up at the bottom of the window while changes are applied or undone: a live percentage and bar,
/// the step in progress, then a short result. It never blocks the page behind it. Every method must be called on the UI thread.
/// </summary>
public partial class ProgressDock : UserControl
{
    static readonly Brush Ok = new SolidColorBrush(Color.FromRgb(0x3D, 0xDC, 0x84));
    static readonly Brush Warn = new SolidColorBrush(Color.FromRgb(0xFF, 0xB0, 0x20));

    readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromMilliseconds(30) };
    readonly List<string> _names = new();
    TaskCompletionSource? _closed;
    double _shown, _target;
    int _count, _doneCount, _running = -1, _version;
    double _stepFrac;
    string _stepName = "";
    bool _finished, _autoClose;
    DateTime _finishedAt;

    public bool CancelRequested { get; private set; }

    public ProgressDock()
    {
        InitializeComponent();
        Visibility = Visibility.Collapsed;
        _timer.Tick += (_, _) => Tick();
    }

    // ---------------------------------------------------------------- lifecycle

    public void Begin(string heading, string title, IReadOnlyList<(string Name, string Detail)> steps, bool canCancel)
    {
        _version++;
        _closed = new TaskCompletionSource();
        _finished = false; _autoClose = false; CancelRequested = false;
        _count = steps.Count; _doneCount = 0; _running = -1; _shown = 0; _target = 0;
        _names.Clear();
        foreach (var s in steps) _names.Add(s.Name);

        TitleText.Text = title;
        SubText.Text = _count > 0 ? "Getting ready…" : "";
        PercentText.Text = "0%";
        Bar.Value = 0;
        Spin.Visibility = Visibility.Visible;
        Glyph.Visibility = Visibility.Collapsed;
        CloseBtn.Visibility = Visibility.Collapsed;
        CancelBtn.Visibility = canCancel ? Visibility.Visible : Visibility.Collapsed;
        CancelBtn.IsEnabled = true;

        Visibility = Visibility.Visible;
        SetBarColor((Brush)Application.Current.Resources["AccentBrush"]);
        PercentText.Foreground = (Brush)Application.Current.Resources["MutedBrush"];
        Card.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(220)));
        Slide.BeginAnimation(TranslateTransform.YProperty,
            new DoubleAnimation(0, TimeSpan.FromMilliseconds(260)) { EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut } });
        SpinRot.BeginAnimation(RotateTransform.AngleProperty,
            new DoubleAnimation(0, 360, TimeSpan.FromSeconds(1.1)) { RepeatBehavior = RepeatBehavior.Forever });
        _timer.Start();
    }

    public void StartStep(int i, string? what = null)
    {
        _running = i; _stepFrac = 0;
        var name = what ?? (i >= 0 && i < _names.Count ? _names[i] : "");
        _stepName = _count > 1 ? $"{Math.Min(i + 1, _count)} of {_count}  ·  {name}" : name;
        SubText.Text = _stepName;
        Retarget();
    }

    public void EndStep(int i, StepResult result, string? tag = null)
    {
        _doneCount = Math.Max(_doneCount, i + 1);
        if (_running == i) { _running = -1; _stepFrac = 0; }
        Retarget();
    }

    void Retarget() =>
        _target = _count == 0 ? 0 : Math.Min(99.0, (_doneCount + (_running >= 0 ? (_stepFrac > 0 ? _stepFrac : 0.05) : 0)) / _count * 100.0);

    /// <summary>Progress inside the step that is running (a download, say): fraction 0-1 and a short line such as "48 of 120 MB · 40%".</summary>
    public void SetStepProgress(double fraction, string? detail)
    {
        if (_running < 0 || _finished) return;
        _stepFrac = Math.Clamp(fraction, 0, 1);
        SubText.Text = string.IsNullOrEmpty(detail) ? _stepName : _stepName + "  ·  " + detail;
        Retarget();
    }

    public void Finish(string pill, string heading, string note, int verified, int needRestart, int failed, bool autoClose)
    {
        _finished = true;
        _running = -1;
        var bad = failed > 0;
        _target = bad && verified == 0 ? Math.Max(_target, 1) : 100;
        TitleText.Text = heading;
        SubText.Text = note.Replace('\n', ' ');
        SpinRot.BeginAnimation(RotateTransform.AngleProperty, null);
        Spin.Visibility = Visibility.Collapsed;
        Glyph.Text = bad ? "" : "";
        Glyph.Foreground = bad ? Warn : Ok;
        Glyph.Visibility = Visibility.Visible;
        CancelBtn.Visibility = Visibility.Collapsed;
        CloseBtn.Visibility = bad || !autoClose ? Visibility.Visible : Visibility.Collapsed;
        _autoClose = autoClose && !bad;
        _finishedAt = DateTime.Now;
        SetBarColor(bad ? Warn : Ok);
        _closed?.TrySetResult();   // the page is free again straight away; the panel just stays up for a moment
    }

    void SetBarColor(Brush brush)
    {
        Bar.ApplyTemplate();
        if (Bar.Template.FindName("PART_Indicator", Bar) is Border ind) ind.Background = brush;
    }

    void Tick()
    {
        _shown += (_target - _shown) * 0.18;
        if (Math.Abs(_target - _shown) < 0.2) _shown = _target;
        Bar.Value = _shown / 100.0;
        PercentText.Text = $"{(int)Math.Round(_shown)}%";
        if (_finished)
        {
            PercentText.Foreground = (Brush)Application.Current.Resources["MutedBrush"];
            if (_autoClose && DateTime.Now - _finishedAt > TimeSpan.FromSeconds(5)) Hide();
        }
    }

    public void Dismiss() => Hide();

    public Task WaitClosedAsync() => _closed?.Task ?? Task.CompletedTask;

    void Hide()
    {
        _timer.Stop();
        SpinRot.BeginAnimation(RotateTransform.AngleProperty, null);
        _closed?.TrySetResult();
        var v = ++_version;
        var fade = new DoubleAnimation(0, TimeSpan.FromMilliseconds(180));
        fade.Completed += (_, _) => { if (v == _version) Visibility = Visibility.Collapsed; };
        Card.BeginAnimation(OpacityProperty, fade);
        Slide.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(20, TimeSpan.FromMilliseconds(180)));
    }

    void OnClose(object sender, RoutedEventArgs e) => Hide();

    void OnCancel(object sender, RoutedEventArgs e)
    {
        CancelRequested = true;
        CancelBtn.IsEnabled = false;
        SubText.Text = "Stopping after the current step…";
    }
}
