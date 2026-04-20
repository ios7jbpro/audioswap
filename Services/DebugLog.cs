namespace AudioSwap.Services;

public static class DebugLog
{
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "AudioSwap",
        "app.log");

    public static string CurrentPath => LogPath;

    public static void Write(string message)
    {
        try
        {
            var directory = Path.GetDirectoryName(LogPath);
            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.AppendAllLines(LogPath, new[]
            {
                $"[{DateTimeOffset.Now:O}] {message}"
            });
        }
        catch
        {
            // Ignore logging failures.
        }
    }

    public static void WriteException(string source, Exception exception)
    {
        Write($"{source}: {exception}");
    }
}
