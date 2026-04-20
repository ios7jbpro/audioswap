namespace AudioSwap.Models;

public sealed class AppSettings
{
    public string PrimaryDeviceId { get; set; } = string.Empty;

    public string SecondaryDeviceId { get; set; } = string.Empty;

    public static AppSettings CreateDefault() => new();
}
