namespace FestKasse.Helpers;

/// <summary>
/// Central place for every file name / path that is reused across the app.
/// All full paths are resolved lazily from <see cref="FileSystem.AppDataDirectory"/>
/// so they are safe to access after the MAUI host has started.
/// </summary>
public static class AppConstants
{
    // ── File names ─────────────────────────────────────────────────────────

    public const string DataFileName     = "festkasse_data.json";
    public const string SettingsFileName = "festkasse_settings.json";
    public const string LogFolderName    = "logs";
    public const string LogBaseName      = "festkasse.log";

    // ── Full paths (computed once per process) ─────────────────────────────

    public static string DataFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, DataFileName);

    public static string SettingsFilePath =>
        Path.Combine(FileSystem.AppDataDirectory, SettingsFileName);

    public static string LogFolderPath =>
        Path.Combine(FileSystem.AppDataDirectory, LogFolderName);

    /// <summary>
    /// The base path handed to Serilog. Serilog appends a date suffix when
    /// rolling is enabled, so the actual on-disk file name differs.
    /// Use <see cref="ResolveCurrentLogFile"/> to obtain the real path.
    /// </summary>
    public static string LogBasePath =>
        Path.Combine(LogFolderPath, LogBaseName);

    /// <summary>
    /// Returns the path of the most recently written log file in
    /// <see cref="LogFolderPath"/>, or <c>null</c> if none exists.
    /// </summary>
    public static string? ResolveCurrentLogFile()
    {
        var dir = LogFolderPath;
        if (!Directory.Exists(dir))
            return null;

        // Serilog RollingInterval.Day produces names like festkasse20250601.log
        var stem = Path.GetFileNameWithoutExtension(LogBaseName); // "festkasse"
        var ext  = Path.GetExtension(LogBaseName);                // ".log"

        return Directory
            .EnumerateFiles(dir, $"{stem}*{ext}")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .FirstOrDefault();
    }
}
