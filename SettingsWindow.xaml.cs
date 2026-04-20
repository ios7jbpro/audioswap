using AudioSwap.Models;
using AudioSwap.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Composition.SystemBackdrops;
using Windows.UI.ViewManagement;
using WinRT.Interop;
using Button = Microsoft.UI.Xaml.Controls.Button;
using ComboBox = Microsoft.UI.Xaml.Controls.ComboBox;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using SolidColorBrush = Microsoft.UI.Xaml.Media.SolidColorBrush;

namespace AudioSwap;

public sealed class SettingsWindow : Window
{
    private readonly AudioDeviceService _audioDeviceService;
    private readonly DispatcherQueue _dispatcherQueue;
    private readonly UISettings _uiSettings;
    private AppSettings _settings;
    private readonly Grid _rootGrid;
    private readonly Border _titleBarRoot;
    private readonly TextBlock _titleBarText;
    private readonly TextBlock _statusText;
    private readonly ComboBox _primaryDeviceComboBox;
    private readonly ComboBox _secondaryDeviceComboBox;

    public event EventHandler<AppSettings>? SettingsSaved;

    public SettingsWindow(AudioDeviceService audioDeviceService, AppSettings settings)
    {
        _audioDeviceService = audioDeviceService;
        _dispatcherQueue = DispatcherQueue.GetForCurrentThread();
        _uiSettings = new UISettings();
        _settings = new AppSettings
        {
            PrimaryDeviceId = settings.PrimaryDeviceId,
            SecondaryDeviceId = settings.SecondaryDeviceId
        };

        Title = "AudioSwap Settings";

        _titleBarRoot = new Border
        {
            Height = 32,
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 243, 243, 243)),
            BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 218, 218, 218)),
            BorderThickness = new Thickness(0, 0, 0, 1)
        };

        _titleBarText = new TextBlock
        {
            Text = "AudioSwap",
            Margin = new Thickness(10, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 12
        };

        _statusText = new TextBlock
        {
            Text = "Choose two devices and save your quick-switch pair.",
            TextWrapping = TextWrapping.WrapWholeWords
        };

        _primaryDeviceComboBox = new ComboBox
        {
            DisplayMemberPath = nameof(AudioDevice.Name),
            PlaceholderText = "Select a playback device"
        };

        _secondaryDeviceComboBox = new ComboBox
        {
            DisplayMemberPath = nameof(AudioDevice.Name),
            PlaceholderText = "Select a second playback device"
        };

        _rootGrid = BuildContent();
        Content = _rootGrid;
        ConfigureWindow();
        HookThemeTracking();
        ApplyTheme(GetPreferredTheme());
        LoadDevices();
    }

    private void LoadDevices()
    {
        var devices = _audioDeviceService.GetPlaybackDevices();

        _primaryDeviceComboBox.ItemsSource = devices;
        _secondaryDeviceComboBox.ItemsSource = devices;

        _primaryDeviceComboBox.SelectedItem = devices.FirstOrDefault(device => device.Id == _settings.PrimaryDeviceId);
        _secondaryDeviceComboBox.SelectedItem = devices.FirstOrDefault(device => device.Id == _settings.SecondaryDeviceId);

        if (devices.Count == 0)
        {
            ShowStatus("No active playback devices were found.", InfoBarSeverity.Warning);
            return;
        }

        ShowStatus("Choose two devices and save your quick-switch pair.", InfoBarSeverity.Informational);
    }

    private void RefreshDevices_Click(object sender, RoutedEventArgs e)
    {
        LoadDevices();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_primaryDeviceComboBox.SelectedItem is not AudioDevice primary
            || _secondaryDeviceComboBox.SelectedItem is not AudioDevice secondary)
        {
            ShowStatus("Select both devices before saving.", InfoBarSeverity.Warning);
            return;
        }

        if (primary.Id == secondary.Id)
        {
            ShowStatus("Pick two different devices.", InfoBarSeverity.Warning);
            return;
        }

        _settings = new AppSettings
        {
            PrimaryDeviceId = primary.Id,
            SecondaryDeviceId = secondary.Id
        };

        SettingsSaved?.Invoke(this, _settings);
        ShowStatus("Preferences saved. You can close this window and use the tray icon.", InfoBarSeverity.Success);
    }

    private void ShowStatus(string message, InfoBarSeverity severity)
    {
        _statusText.Text = message;
    }

    private Grid BuildContent()
    {
        var root = new Grid
        {
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 250, 250, 250))
        };

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        _titleBarRoot.Child = BuildTitleBar();

        var contentPanel = new StackPanel
        {
            Spacing = 16,
            Padding = new Thickness(24)
        };

        contentPanel.Children.Add(new TextBlock
        {
            Text = "AudioSwap",
            FontSize = 28
        });

        contentPanel.Children.Add(new TextBlock
        {
            Text = "Pick the two playback devices you want to jump between instantly.",
            TextWrapping = TextWrapping.WrapWholeWords
        });

        contentPanel.Children.Add(_statusText);
        contentPanel.Children.Add(BuildField("Primary device", _primaryDeviceComboBox));
        contentPanel.Children.Add(BuildField("Secondary device", _secondaryDeviceComboBox));
        contentPanel.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            Children =
            {
                new Button
                {
                    Content = "Refresh devices"
                }.Also(button => button.Click += RefreshDevices_Click),
                new Button
                {
                    Content = "Save"
                }.Also(button => button.Click += Save_Click)
            }
        });

        Grid.SetRow(_titleBarRoot, 0);
        Grid.SetRow(contentPanel, 1);
        root.Children.Add(_titleBarRoot);
        root.Children.Add(contentPanel);

        return root;
    }

    private void ConfigureWindow()
    {
        var hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
        appWindow.Resize(new Windows.Graphics.SizeInt32(560, 420));

        if (appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = true;
            presenter.SetBorderAndTitleBar(true, true);
        }

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(_titleBarRoot);
        TryApplyBackdrop();

        if (appWindow.TitleBar is { } titleBar)
        {
            UpdateTitleBarColors(titleBar, GetPreferredTheme());
        }
    }

    private UIElement BuildTitleBar()
    {
        var layout = new Grid
        {
            Padding = new Thickness(12, 0, 0, 0)
        };

        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        layout.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var glyph = new Border
        {
            Width = 16,
            Height = 16,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 120, 215)),
            CornerRadius = new CornerRadius(3),
            Child = new TextBlock
            {
                Text = "♪",
                FontSize = 10,
                Foreground = new SolidColorBrush(Colors.White),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        Grid.SetColumn(_titleBarText, 1);
        layout.Children.Add(glyph);
        layout.Children.Add(_titleBarText);
        return layout;
    }

    private static UIElement BuildField(string label, ComboBox comboBox)
    {
        return new StackPanel
        {
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = label
                },
                comboBox
            }
        };
    }

    private void HookThemeTracking()
    {
        _uiSettings.ColorValuesChanged += (_, _) =>
        {
            _dispatcherQueue.TryEnqueue(() =>
            {
                var theme = GetPreferredTheme();
                _rootGrid.RequestedTheme = theme;
                ApplyTheme(theme);
            });
        };
    }

    private ElementTheme GetPreferredTheme()
    {
        var background = _uiSettings.GetColorValue(UIColorType.Background);
        var isDark = background.R + background.G + background.B < (255 * 3 / 2);
        return isDark ? ElementTheme.Dark : ElementTheme.Light;
    }

    private void ApplyTheme(ElementTheme theme)
    {
        _rootGrid.RequestedTheme = theme;

        if (theme == ElementTheme.Dark)
        {
            _rootGrid.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 32, 32, 32));
            _titleBarRoot.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 43, 43, 43));
            _titleBarRoot.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 62, 62, 62));
            _titleBarText.Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 242, 242, 242));
        }
        else
        {
            _rootGrid.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 250, 250, 250));
            _titleBarRoot.Background = new SolidColorBrush(ColorHelper.FromArgb(255, 243, 243, 243));
            _titleBarRoot.BorderBrush = new SolidColorBrush(ColorHelper.FromArgb(255, 218, 218, 218));
            _titleBarText.Foreground = new SolidColorBrush(ColorHelper.FromArgb(255, 24, 24, 24));
        }

        var hwnd = WindowNative.GetWindowHandle(this);
        var appWindow = AppWindow.GetFromWindowId(Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd));
        if (appWindow.TitleBar is { } titleBar)
        {
            UpdateTitleBarColors(titleBar, theme);
        }
    }

    private static void UpdateTitleBarColors(AppWindowTitleBar titleBar, ElementTheme theme)
    {
        if (theme == ElementTheme.Dark)
        {
            titleBar.ButtonBackgroundColor = ColorHelper.FromArgb(255, 43, 43, 43);
            titleBar.ButtonInactiveBackgroundColor = ColorHelper.FromArgb(255, 43, 43, 43);
            titleBar.ButtonForegroundColor = ColorHelper.FromArgb(255, 242, 242, 242);
            titleBar.ButtonInactiveForegroundColor = ColorHelper.FromArgb(255, 150, 150, 150);
            titleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(255, 65, 65, 65);
            titleBar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(255, 82, 82, 82);
            titleBar.ButtonHoverForegroundColor = ColorHelper.FromArgb(255, 255, 255, 255);
            titleBar.ButtonPressedForegroundColor = ColorHelper.FromArgb(255, 255, 255, 255);
        }
        else
        {
            titleBar.ButtonBackgroundColor = ColorHelper.FromArgb(255, 243, 243, 243);
            titleBar.ButtonInactiveBackgroundColor = ColorHelper.FromArgb(255, 243, 243, 243);
            titleBar.ButtonForegroundColor = ColorHelper.FromArgb(255, 32, 32, 32);
            titleBar.ButtonInactiveForegroundColor = ColorHelper.FromArgb(255, 110, 110, 110);
            titleBar.ButtonHoverBackgroundColor = ColorHelper.FromArgb(255, 229, 229, 229);
            titleBar.ButtonPressedBackgroundColor = ColorHelper.FromArgb(255, 214, 214, 214);
            titleBar.ButtonHoverForegroundColor = ColorHelper.FromArgb(255, 16, 16, 16);
            titleBar.ButtonPressedForegroundColor = ColorHelper.FromArgb(255, 16, 16, 16);
        }
    }

    private void TryApplyBackdrop()
    {
        try
        {
            SystemBackdrop = GetPreferredTheme() == ElementTheme.Dark
                ? new DesktopAcrylicBackdrop()
                : new MicaBackdrop();
        }
        catch
        {
            // Keep the stable solid background if system backdrops are unavailable.
        }
    }
}
