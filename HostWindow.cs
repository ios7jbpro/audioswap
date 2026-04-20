using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using AudioSwap.Services;
using WinRT.Interop;

namespace AudioSwap;

public sealed class HostWindow : Window
{
    private bool _configured;

    public HostWindow()
    {
        DebugLog.Write("HostWindow constructed");
        Title = "AudioSwap Host";
        Content = new Grid();
        Activated += OnActivated;
        Closed += OnClosed;
    }

    private void OnActivated(object sender, WindowActivatedEventArgs args)
    {
        if (_configured)
        {
            return;
        }

        _configured = true;
        DebugLog.Write("HostWindow activated; minimizing window");

        var hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new Windows.Graphics.SizeInt32(320, 120));
        appWindow.Move(new Windows.Graphics.PointInt32(-10000, -10000));
        appWindow.IsShownInSwitchers = false;

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
            presenter.Minimize();
        }
    }

    private void OnClosed(object sender, WindowEventArgs args)
    {
        DebugLog.Write("HostWindow closed");
    }
}
