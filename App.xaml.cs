using AudioSwap.Models;
using AudioSwap.Services;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using WinUIApplication = Microsoft.UI.Xaml.Application;

namespace AudioSwap;

public partial class App : WinUIApplication
{
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly AudioDeviceService _audioDeviceService;
    private readonly SettingsService _settingsService;
    private readonly TrayIconService _trayIconService;
    private HostWindow? _hostWindow;
    private SettingsWindow? _settingsWindow;
    private AppSettings _settings = AppSettings.CreateDefault();

    public App()
    {
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _audioDeviceService = new AudioDeviceService();
        _settingsService = new SettingsService();
        _trayIconService = new TrayIconService();

        DebugLog.Write("App constructed");
        UnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        DebugLog.Write("OnLaunched start");
        _settings = await _settingsService.LoadAsync();
        DebugLog.Write("Settings loaded");
        EnsureHostWindow();
        DebugLog.Write("Host window ensured");

        _trayIconService.Initialize(
            onToggleRequested: ToggleAudio,
            onOpenSettingsRequested: ShowSettingsWindow,
            onExitRequested: ExitApplication);
        DebugLog.Write("Tray icon initialized");

        if (!HasValidSelection(_settings))
        {
            DebugLog.Write("No valid selection, opening settings");
            ShowSettingsWindow();
        }
        else
        {
            DebugLog.Write("Valid selection found");
            UpdateTrayForCurrentDevice();
        }
    }

    private void EnsureHostWindow()
    {
        if (_hostWindow is not null)
        {
            return;
        }

        DebugLog.Write("Creating host window");
        _hostWindow = new HostWindow();
        _hostWindow.Activate();
    }

    private void ToggleAudio()
    {
        try
        {
            var devices = _audioDeviceService.GetPlaybackDevices();
            var primary = devices.FirstOrDefault(device => device.Id == _settings.PrimaryDeviceId);
            var secondary = devices.FirstOrDefault(device => device.Id == _settings.SecondaryDeviceId);

            if (primary is null || secondary is null)
            {
                _trayIconService.ShowBalloonTip("AudioSwap", "Pick two playback devices in settings first.");
                ShowSettingsWindow();
                return;
            }

            var currentDefaultId = _audioDeviceService.GetDefaultPlaybackDeviceId();
            var target = currentDefaultId == primary.Id ? secondary : primary;

            _audioDeviceService.SetDefaultPlaybackDevice(target.Id);
            _trayIconService.UpdateStatus(target.Name);
            _trayIconService.ShowBalloonTip("AudioSwap", $"Switched to {target.Name}");
        }
        catch (Exception ex)
        {
            _trayIconService.ShowBalloonTip("AudioSwap", $"Switch failed: {ex.Message}");
        }
    }

    private void ShowSettingsWindow()
    {
        DebugLog.Write("ShowSettingsWindow requested");

        void ShowWindow()
        {
            DebugLog.Write("ShowSettingsWindow executing");
            if (_settingsWindow is null)
            {
                _settingsWindow = new SettingsWindow(_audioDeviceService, _settings);
                _settingsWindow.Closed += SettingsWindowOnClosed;
                _settingsWindow.SettingsSaved += OnSettingsSaved;
                DebugLog.Write("SettingsWindow created");
            }

            _settingsWindow.Activate();
            DebugLog.Write("SettingsWindow activated");
        }

        if (_dispatcherQueue.HasThreadAccess)
        {
            ShowWindow();
            return;
        }

        _dispatcherQueue.TryEnqueue(ShowWindow);
    }

    private void OnSettingsSaved(object? sender, AppSettings settings)
    {
        _settings = settings;
        _ = _settingsService.SaveAsync(settings);
        DebugLog.Write("Settings saved event received");
        UpdateTrayForCurrentDevice();
        _trayIconService.ShowBalloonTip("AudioSwap", "Saved preferred devices.");
    }

    private static bool HasValidSelection(AppSettings settings)
    {
        return !string.IsNullOrWhiteSpace(settings.PrimaryDeviceId)
            && !string.IsNullOrWhiteSpace(settings.SecondaryDeviceId)
            && settings.PrimaryDeviceId != settings.SecondaryDeviceId;
    }

    private void SettingsWindowOnClosed(object sender, WindowEventArgs args)
    {
        DebugLog.Write("SettingsWindow closed");
        if (_settingsWindow is null)
        {
            return;
        }

        _settingsWindow.SettingsSaved -= OnSettingsSaved;
        _settingsWindow.Closed -= SettingsWindowOnClosed;
        _settingsWindow = null;
    }

    private void ExitApplication()
    {
        DebugLog.Write("ExitApplication invoked");
        _settingsWindow?.Close();
        _hostWindow?.Close();
        _trayIconService.Dispose();
        _audioDeviceService.Dispose();
        Exit();
    }

    private void UpdateTrayForCurrentDevice()
    {
        try
        {
            var currentDefaultId = _audioDeviceService.GetDefaultPlaybackDeviceId();
            var activeDevice = _audioDeviceService
                .GetPlaybackDevices()
                .FirstOrDefault(device => device.Id == currentDefaultId);

            if (activeDevice is not null)
            {
                _trayIconService.UpdateStatus(activeDevice.Name);
            }
        }
        catch (Exception ex)
        {
            DebugLog.WriteException("UpdateTrayForCurrentDevice", ex);
        }
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        DebugLog.WriteException("WinUI", e.Exception);
        _trayIconService.ShowBalloonTip("AudioSwap", e.Exception.Message);
    }

    private void OnCurrentDomainUnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            DebugLog.WriteException("AppDomain", exception);
        }
    }
}
