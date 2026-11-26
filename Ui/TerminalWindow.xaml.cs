using System.ComponentModel;
using System.Windows;

namespace TrapShack.Ui;

/// <summary>
/// The pop-up terminal used by the Utilities page. It can't be closed while something is running (use Stop),
/// and closing it afterwards just hides it until the next tool runs.
/// </summary>
public partial class TerminalWindow : Window
{
    public bool Running { get; set; }

    public TerminalWindow()
    {
        InitializeComponent();
        Term.Closable = true;
        Term.CloseRequested += Hide;
        Term.DragRequested += () => { try { DragMove(); } catch { } };
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // never lose a running repair by closing its window (use Stop first)
        if (Running) e.Cancel = true;
    }
}
