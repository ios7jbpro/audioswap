using System.Text.Json;
using AudioSwap.Models;

namespace AudioSwap.Services;

public sealed class SettingsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsPath;

    public SettingsService()
    {
        var appFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AudioSwap");

        Directory.CreateDirectory(appFolder);
        _settingsPath = Path.Combine(appFolder, "settings.json");
    }

    public async Task<AppSettings> LoadAsync()
    {
        if (!File.Exists(_settingsPath))
        {
            return AppSettings.CreateDefault();
        }

        await using var stream = File.OpenRead(_settingsPath);
        return await JsonSerializer.DeserializeAsync<AppSettings>(stream, SerializerOptions)
            ?? AppSettings.CreateDefault();
    }

    public async Task SaveAsync(AppSettings settings)
    {
        await using var stream = File.Create(_settingsPath);
        await JsonSerializer.SerializeAsync(stream, settings, SerializerOptions);
    }
}
