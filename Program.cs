using System.Runtime.InteropServices;
using AudioSwap.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinRT;
using WinUIApplication = Microsoft.UI.Xaml.Application;

namespace AudioSwap;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        DebugLog.Write("Program.Main starting");

        try
        {
            XamlCheckProcessRequirements();
            DebugLog.Write("XamlCheckProcessRequirements passed");

            ComWrappersSupport.InitializeComWrappers();
            DebugLog.Write("ComWrappers initialized");

            WinUIApplication.Start(_ =>
            {
                DebugLog.Write("Application.Start entered");
                var queue = DispatcherQueue.GetForCurrentThread();
                SynchronizationContext.SetSynchronizationContext(new DispatcherQueueSynchronizationContext(queue));
                var app = new App();
                DebugLog.Write("App instance created");
            });

            DebugLog.Write("Application.Start returned");
        }
        catch (Exception ex)
        {
            DebugLog.WriteException("Program.Main", ex);
            throw;
        }
    }

    [DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();
}
