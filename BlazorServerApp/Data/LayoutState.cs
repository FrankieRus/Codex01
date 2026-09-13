namespace BlazorServerApp.Data;

/// <summary>
/// Per-user (scoped) UI state for the app shell, e.g. hiding the left nav
/// sidebar while a page (like the PDF viewer) wants full width.
/// </summary>
public class LayoutState
{
    public bool SidebarHidden { get; private set; }

    public event Action? OnChange;

    public void HideSidebar()
    {
        if (!SidebarHidden)
        {
            SidebarHidden = true;
            OnChange?.Invoke();
        }
    }

    public void ShowSidebar()
    {
        if (SidebarHidden)
        {
            SidebarHidden = false;
            OnChange?.Invoke();
        }
    }
}
