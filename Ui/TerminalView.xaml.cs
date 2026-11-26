using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>One line in the terminal.</summary>
public sealed class TermLine : Notify
{
    string _text = "";
    bool _live;
    public TermKind Kind { get; init; }
    public string Text { get => _text; set => Set(ref _text, value); }
    public bool IsLive { get => _live; set => Set(ref _live, value); }
    public string Mark => Kind switch
    {
        TermKind.Command => "",   // chevron
        TermKind.Success => "",   // tick
        TermKind.Error => "",     // cross
        TermKind.Warning => "",   // warning
        TermKind.Info => "",      // info
        _ => "",
    };
}

public enum TermState { Idle, Running, Done, Failed, Stopped }

/// <summary>
/// The app's own terminal: shows commands and their live output in the app's colours instead of a plain console
/// window. Lines can be added from any thread.
/// </summary>
public partial class TerminalView : UserControl
{
    const int MaxLines = 4000;
    readonly ObservableCollection<TermLine> _lines = new();
    TermLine? _live;
    ScrollViewer? _scroller;

    public event Action? StopRequested;
    public event Action? CloseRequested;
    public event Action? DragRequested;

    /// <summary>Shows a close button (used when the terminal is its own pop-up window). Hidden while something runs.</summary>
    bool _closable;
    public bool Closable { get => _closable; set { _closable = value; CloseBtn.Visibility = value ? Visibility.Visible : Visibility.Collapsed; } }

    public TerminalView()
    {
        InitializeComponent();
        Lines.ItemsSource = _lines;
        Loaded += (_, _) =>
        {
            Lines.ApplyTemplate();
            _scroller = Lines.Template.FindName("Scroller", Lines) as ScrollViewer;
        };
        // the blinking caret only runs while the terminal can be seen (a hidden endless animation still costs frames)
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible)
                Caret.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(600))
                { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
            else Caret.BeginAnimation(OpacityProperty, null);
        };
    }

    public string Title { get => TitleText.Text; set => TitleText.Text = value; }

    /// <summary>A smaller version (used under the progress panel): no stop button.</summary>
    bool _compact;
    public bool Compact
    {
        get => _compact;
        set { _compact = value; if (value) { StopBtn.Visibility = Visibility.Collapsed; TitleBar.Padding = new Thickness(10, 6, 6, 6); } }
    }

    // ------------------------------------------------------------------------------------------ writing
    public void Command(string text) => Add(text, TermKind.Command);
    public void Note(string text, TermKind kind = TermKind.Info) => Add(text, kind);

    /// <summary>A line from a running program. Live lines (a progress bar redrawing itself) replace each other.</summary>
    public void Line(string text, bool live, TermKind kind)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(new Action(() => Line(text, live, kind))); return; }
        text = text.TrimEnd();
        if (_live != null)
        {
            if (live) { _live.Text = text; return; }
            if (text.Length > 0) _live.Text = text;
            _live.IsLive = false; _live = null;
            if (text.Length == 0) return;
            Scroll();
            return;
        }
        if (text.Length == 0 && !live) { if (_lines.Count > 0 && _lines[^1].Text.Length == 0) return; }
        var l = new TermLine { Kind = kind, Text = text, IsLive = live };
        Push(l);
        if (live) _live = l;
    }

    void Add(string text, TermKind kind)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(new Action(() => Add(text, kind))); return; }
        if (_live != null) { _live.IsLive = false; _live = null; }
        Push(new TermLine { Kind = kind, Text = text });
    }

    void Push(TermLine l)
    {
        _lines.Add(l);
        while (_lines.Count > MaxLines) _lines.RemoveAt(0);
        Scroll();
    }

    void Scroll()
    {
        // keep following the output unless the user scrolled up to read something
        if (_scroller == null) return;
        if (_scroller.VerticalOffset >= _scroller.ScrollableHeight - 2)
            Dispatcher.BeginInvoke(new Action(() => _scroller.ScrollToEnd()), System.Windows.Threading.DispatcherPriority.Background);
    }

    // ------------------------------------------------------------------------------------------ status
    public void SetState(TermState state, string? caption = null)
    {
        if (!Dispatcher.CheckAccess()) { Dispatcher.BeginInvoke(new Action(() => SetState(state, caption))); return; }
        if (caption != null) Caption.Text = caption;
        var (text, brushKey) = state switch
        {
            TermState.Running => ("Running", "TermAccentBrush"),
            TermState.Done => ("Done", "GreenBrush"),
            TermState.Failed => ("Problem", "AmberBrush"),
            TermState.Stopped => ("Stopped", "AmberBrush"),
            _ => ("Idle", "FaintBrush"),
        };
        PillText.Text = text;
        var brush = (Brush)FindResource(brushKey);
        PillDot.Fill = brush;
        PillText.Foreground = state == TermState.Idle ? (Brush)FindResource("MutedBrush") : brush;
        PromptText.Text = state == TermState.Running ? "working…" : "waiting for a task";

        var running = state == TermState.Running;
        if (StopRequested != null && !_compact) StopBtn.Visibility = running ? Visibility.Visible : Visibility.Collapsed;
        ClearBtn.IsEnabled = !running;
        if (_closable) CloseBtn.Visibility = running ? Visibility.Collapsed : Visibility.Visible;
        if (running)
        {
            RunBar.Opacity = 1;
            RunSlide.BeginAnimation(TranslateTransform.XProperty,
                new DoubleAnimation(-140, Math.Max(400, ActualWidth), TimeSpan.FromSeconds(1.4)) { RepeatBehavior = RepeatBehavior.Forever });
            PillDot.BeginAnimation(OpacityProperty, new DoubleAnimation(1, 0.3, TimeSpan.FromMilliseconds(700)) { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever });
        }
        else
        {
            RunSlide.BeginAnimation(TranslateTransform.XProperty, null);
            RunBar.Opacity = 0;
            PillDot.BeginAnimation(OpacityProperty, null);
        }
    }

    public void Clear() { _lines.Clear(); _live = null; }

    void OnStop(object sender, RoutedEventArgs e) => StopRequested?.Invoke();
    void OnClear(object sender, RoutedEventArgs e) => Clear();
    void OnCloseClick(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();
    void OnTitleDown(object sender, System.Windows.Input.MouseButtonEventArgs e) => DragRequested?.Invoke();

    void OnCopy(object sender, RoutedEventArgs e)
    {
        try { Clipboard.SetText(string.Join(Environment.NewLine, _lines.Select(l => l.Text))); } catch { }
    }
}
