using System.Text;
using System.Windows;
using System.Windows.Controls;
using TrapShack.Core;

namespace TrapShack.Ui;

/// <summary>One utility as shown on the page.</summary>
public sealed class UtilVM : Notify
{
    public UtilVM(UtilTool t) { Tool = t; }
    public UtilTool Tool { get; }
    public string Title => Tool.Title;
    public string Desc => Tool.Desc;
    public string Glyph => Tool.Glyph;
    public bool OpensWindow => Tool.Steps.Length == 0;
    public string ButtonText => OpensWindow ? "Open" : "Run";
    public string ButtonGlyph => OpensWindow ? "" : "";

    bool _running, _blocked;
    public bool IsRunning { get => _running; set { if (Set(ref _running, value)) Raise(nameof(CanRun)); } }
    /// <summary>True while another utility is running (only one runs at a time).</summary>
    public bool Blocked { get => _blocked; set { if (Set(ref _blocked, value)) Raise(nameof(CanRun)); } }
    public bool CanRun => OpensWindow || !_blocked;
}

public sealed class UtilSectionVM : IPackCount
{
    public int PackCount => Tools.Count;
    public string Name { get; init; } = "";
    public string Glyph { get; init; } = "";
    public string Blurb { get; init; } = "";
    public List<UtilVM> Tools { get; init; } = new();
}

/// <summary>The Utilities page: Windows repair and maintenance tools, run one at a time in the built-in terminal.</summary>
public partial class UtilitiesPage : UserControl
{
    IPageHost? _host;
    readonly List<UtilVM> _all = new();
    CancellationTokenSource? _cts;
    bool _running;

    public UtilitiesPage()
    {
        InitializeComponent();
    }

    // the pop-up terminal, made the first time a tool runs
    TerminalWindow? _win;
    TerminalView Term => _win!.Term;

    void ShowTerminal(string title)
    {
        if (_win == null)
        {
            _win = new TerminalWindow { Owner = _host!.Win };
            _win.Term.StopRequested += () => _cts?.Cancel();
            _win.Closed += (_, _) => _win = null;
        }
        _win.Term.Clear();
        _win.Term.Title = title;
        if (!_win.IsVisible) _win.Show();
        _win.Activate();
    }

    public void Init(IPageHost host, bool isLaptop)
    {
        _host = host;
        var sections = new List<UtilSectionVM>();
        foreach (var (name, glyph, blurb) in UtilityCatalog.Sections)
        {
            var tools = UtilityCatalog.Tools.Where(t => t.Section == name && (!t.LaptopOnly || isLaptop)).Select(t => new UtilVM(t)).ToList();
            _all.AddRange(tools);
            if (tools.Count > 0) sections.Add(new UtilSectionVM { Name = name, Glyph = glyph, Blurb = blurb, Tools = tools });
        }
        Cards.ItemsSource = sections;
        WinTools.ItemsSource = UtilityCatalog.WindowsTools;
    }

    void OnOpenWinTool(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not WinTool t) return;
        if (!CommandRunner.Open(t.Target, t.Args) && _host != null)
            Dialogs.Info(_host.Win, t.Name, "Windows couldn't open " + t.Name + " on this PC.");
    }

    async void OnRunTool(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not UtilVM vm || _host == null) return;
        var tool = vm.Tool;

        if (vm.OpensWindow)
        {
            if (!CommandRunner.Open(tool.Open!, tool.OpenArgs))
                Dialogs.Info(_host.Win, tool.Title, "Windows couldn't open " + tool.Title + " on this PC.");
            return;
        }

        if (_running || _host.Busy) return;
        if (tool.Confirm != null && !Dialogs.Confirm(_host.Win, tool.Title, tool.Confirm, "Run", "Cancel")) return;

        _running = true; _host.Busy = true;
        vm.IsRunning = true;
        foreach (var other in _all) other.Blocked = true;
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        ShowTerminal(tool.Title);
        _win!.Running = true;
        Term.SetState(TermState.Running, tool.Title);
        var problems = 0;
        var stopped = false;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            for (var i = 0; i < tool.Steps.Length; i++)
            {
                var step = tool.Steps[i];
                var args = UtilityCatalog.Expand(step.Args);
                Term.Note($"Step {i + 1} of {tool.Steps.Length}: {step.Label}", TermKind.Info);
                Term.Command(step.File.Equals("powershell.exe", StringComparison.OrdinalIgnoreCase)
                    ? "powershell  (" + step.Label + ")"
                    : System.IO.Path.GetFileNameWithoutExtension(step.File) + (args.Length > 0 ? " " + args : ""));
                var code = await CommandRunner.RunAsync(step.File, args, step.Unicode ? Encoding.Unicode : null,
                    (text, live, kind) => Term.Line(text, live, kind), token);
                if (code == -2) { stopped = true; break; }
                if (code == 0 || step.AnyExitIsFine) Term.Note(code == 0 ? "Done" : $"Finished (code {code})", TermKind.Success);
                else { problems++; Term.Note($"Finished with code {code}. Windows reported a problem with this step (details above).", TermKind.Warning); }
            }
        }
        catch (Exception ex)
        {
            problems++;
            Log.Error($"Utility {tool.Id} failed: {ex}");
            Term.Note("Something went wrong: " + ex.Message, TermKind.Error);
        }

        var took = clock.Elapsed.TotalMinutes >= 1 ? $"{(int)clock.Elapsed.TotalMinutes} min {clock.Elapsed.Seconds} s" : $"{clock.Elapsed.Seconds} s";
        if (stopped) { Term.Note("Stopped.", TermKind.Warning); Term.SetState(TermState.Stopped, tool.Title + " was stopped"); }
        else if (problems == 0) { Term.Note($"{tool.Title} finished in {took}.", TermKind.Success); Term.SetState(TermState.Done, tool.Title + " finished"); }
        else { Term.Note($"{tool.Title} finished in {took}, with {problems} step{(problems == 1 ? "" : "s")} reporting a problem.", TermKind.Warning); Term.SetState(TermState.Failed, tool.Title + " needs a look"); }

        if (!stopped && tool.OpenAfter != null) CommandRunner.Open(UtilityCatalog.Expand(tool.OpenAfter));
        if (_win != null) _win.Running = false;
        vm.IsRunning = false;
        foreach (var other in _all) other.Blocked = false;
        _running = false; _host.Busy = false;
        if (!stopped && tool.Restart) _host.SuggestRestart(tool.Title + " is finished. Restart to complete it.");
    }
}
