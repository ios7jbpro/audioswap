using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AudioSwap.Services;

public sealed class TrayIconService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private Icon? _currentIcon;

    public void Initialize(Action onToggleRequested, Action onOpenSettingsRequested, Action onExitRequested)
    {
        if (_notifyIcon is not null)
        {
            return;
        }

        var contextMenu = new ContextMenuStrip();
        contextMenu.Items.Add("🔄 Toggle Output", null, (_, _) => onToggleRequested());
        contextMenu.Items.Add("⚙️ Settings", null, (_, _) => onOpenSettingsRequested());
        contextMenu.Items.Add(new ToolStripSeparator());
        contextMenu.Items.Add("❌ Exit", null, (_, _) => onExitRequested());

        _notifyIcon = new NotifyIcon
        {
            ContextMenuStrip = contextMenu,
            Icon = CreateDeviceIcon("A"),
            Text = "AudioSwap 🔈",
            Visible = true
        };

        _currentIcon = _notifyIcon.Icon;

        _notifyIcon.MouseClick += (_, args) =>
        {
            if (args.Button == MouseButtons.Left)
            {
                onToggleRequested();
            }
        };
    }

    public void UpdateStatus(string activeDeviceName)
    {
        if (_notifyIcon is null)
        {
            return;
        }

        ReplaceIcon(CreateDeviceIcon(activeDeviceName));
        _notifyIcon.Text = $"AudioSwap 🔈 {TrimTooltip(activeDeviceName)}";
    }

    public void ShowBalloonTip(string title, string message)
    {
        _notifyIcon?.ShowBalloonTip(1500, title, message, ToolTipIcon.Info);
    }

    public void Dispose()
    {
        if (_notifyIcon is null)
        {
            return;
        }

        _notifyIcon.Visible = false;
        _currentIcon?.Dispose();
        _currentIcon = null;
        _notifyIcon.Dispose();
        _notifyIcon = null;
    }

    private void ReplaceIcon(Icon icon)
    {
        if (_notifyIcon is null)
        {
            icon.Dispose();
            return;
        }

        var previousIcon = _currentIcon;
        _notifyIcon.Icon = icon;
        _currentIcon = icon;
        previousIcon?.Dispose();
    }

    private static Icon CreateDeviceIcon(string deviceName)
    {
        const int size = 16;

        using var bitmap = new Bitmap(size, size);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var letter = GetBadgeLetter(deviceName);
        var backgroundColor = GetDeviceColor(deviceName);

        using var backgroundBrush = new SolidBrush(backgroundColor);
        graphics.FillEllipse(backgroundBrush, 0, 0, size - 1, size - 1);

        using var borderPen = new Pen(Color.FromArgb(70, 0, 0, 0));
        graphics.DrawEllipse(borderPen, 0, 0, size - 1, size - 1);

        using var font = new Font("Segoe UI", 8.0f, FontStyle.Bold, GraphicsUnit.Pixel);
        using var textBrush = new SolidBrush(Color.White);
        var layout = new RectangleF(0, 0, size, size - 1);
        using var format = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        graphics.DrawString(letter.ToString(), font, textBrush, layout, format);

        var handle = bitmap.GetHicon();
        try
        {
            using var source = Icon.FromHandle(handle);
            return (Icon)source.Clone();
        }
        finally
        {
            DestroyIcon(handle);
        }
    }

    private static char GetBadgeLetter(string deviceName)
    {
        foreach (var character in deviceName)
        {
            if (char.IsLetterOrDigit(character))
            {
                return char.ToUpperInvariant(character);
            }
        }

        return 'A';
    }

    private static Color GetDeviceColor(string deviceName)
    {
        var palette = new[]
        {
            Color.FromArgb(0, 120, 215),
            Color.FromArgb(16, 124, 16),
            Color.FromArgb(196, 43, 28),
            Color.FromArgb(136, 23, 152),
            Color.FromArgb(3, 131, 135),
            Color.FromArgb(194, 57, 179)
        };

        var index = Math.Abs(deviceName.GetHashCode()) % palette.Length;
        return palette[index];
    }

    private static string TrimTooltip(string text)
    {
        return text.Length <= 40 ? text : $"{text[..37]}...";
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyIcon(IntPtr handle);
}
