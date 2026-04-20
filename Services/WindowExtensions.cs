using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace AudioSwap.Services;

public static class WindowExtensions
{
    public static void BringToFront(this Window window)
    {
        var hwnd = WindowNative.GetWindowHandle(window);
        var appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.Restore();
        }

        appWindow.Show();
    }
}
