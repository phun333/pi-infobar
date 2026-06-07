using System.Windows;
using PiStats.Interop;

namespace PiStats.Views;

public partial class PopoverWindow : Window
{
    public PopoverWindow()
    {
        InitializeComponent();
        // Hide instead of close when it loses focus or the user clicks away.
        Deactivated += (_, _) => HidePopover();
        SourceInitialized += (_, _) => WindowEffects.ApplyAcrylic(this);
    }

    /// Position bottom-right above the taskbar (near the tray) and show.
    public void ShowPopover()
    {
        var wa = SystemParameters.WorkArea;
        const double gap = 8;
        Left = wa.Right - Width - gap;
        Top = wa.Bottom - Height - gap;

        Show();
        Activate();
        Topmost = true;
    }

    public void HidePopover()
    {
        Hide();
    }
}
