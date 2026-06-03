using System.Text;
using FestKasse.Helpers;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace FestKasse.Services;

/// <summary>
/// Serilog-backed log service that writes to a rolling file in the app-data
/// directory.  The log level can be changed at runtime via SetLogLevel().
/// </summary>
public sealed class LogService : ILogService, IDisposable
{
    // Serilog level switch – mutated at runtime to change the minimum level.
    private readonly LoggingLevelSwitch _levelSwitch = new(LogEventLevel.Information);

    private Serilog.Core.Logger _logger;

    /// <summary>
    /// The base log path passed to Serilog. The actual on-disk file includes a
    /// date suffix due to rolling. Use <see cref="AppConstants.ResolveCurrentLogFile"/>
    /// to get the real readable path.
    /// </summary>
    public string LogFilePath => AppConstants.LogBasePath;

    public LogService(string initialLevel = "Information")
    {
        Directory.CreateDirectory(AppConstants.LogFolderPath);

        _levelSwitch.MinimumLevel = ParseLevel(initialLevel);

        _logger = BuildLogger();
    }

    // ── ILogService ────────────────────────────────────────────────────────

    public void Debug(string message)   => _logger.Debug(message);
    public void Info(string message)    => _logger.Information(message);
    public void Warning(string message) => _logger.Warning(message);
    public void Error(string message)   => _logger.Error(message);

    /// <summary>
    /// Logs the exception, every inner exception, and the full stack trace.
    /// </summary>
    public void Exception(Exception ex, string? context = null)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrWhiteSpace(context))
            sb.AppendLine(context);

        var current = ex;
        int depth = 0;
        while (current != null)
        {
            if (depth > 0)
                sb.AppendLine($"  ↳ InnerException [{depth}]: {current.GetType().FullName}");
            else
                sb.AppendLine($"Exception: {current.GetType().FullName}");

            sb.AppendLine($"  Message: {current.Message}");

            if (!string.IsNullOrWhiteSpace(current.StackTrace))
            {
                sb.AppendLine("  StackTrace:");
                foreach (var line in current.StackTrace.Split('\n'))
                    sb.AppendLine("    " + line.TrimEnd());
            }

            current = current.InnerException;
            depth++;
        }

        _logger.Error(ex, sb.ToString());
    }

    public IReadOnlyList<string> GetAllLogFiles()
    {
        var dir = AppConstants.LogFolderPath;
        if (!Directory.Exists(dir))
            return [];

        var stem = Path.GetFileNameWithoutExtension(AppConstants.LogBaseName);
        var ext  = Path.GetExtension(AppConstants.LogBaseName);

        return Directory
            .EnumerateFiles(dir, $"{stem}*{ext}")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToList();
    }

    public async Task<string> ReadLogFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return string.Empty;

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);
        return await reader.ReadToEndAsync();
    }

    public async Task DeleteLogFileAsync(string filePath)
    {
        // If it is the currently active log file, flush the logger first
        var current = AppConstants.ResolveCurrentLogFile();
        bool isActive = string.Equals(filePath, current, StringComparison.OrdinalIgnoreCase);

        if (isActive)
            await _logger.DisposeAsync();

        try
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        finally
        {
            if (isActive)
                _logger = BuildLogger();
        }
    }

    public async Task<string> ReadLogAsync()
    {
        // Flush buffered writes to disk first
        await _logger.DisposeAsync();
        try
        {
            var actualFile = AppConstants.ResolveCurrentLogFile();
            if (actualFile is null || !File.Exists(actualFile))
                return string.Empty;

            // Open with FileShare.ReadWrite so Serilog can keep the file open
            using var fs = new FileStream(actualFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var reader = new StreamReader(fs, System.Text.Encoding.UTF8);
            return await reader.ReadToEndAsync();
        }
        finally
        {
            _logger = BuildLogger();
        }
    }

    public async Task ClearLogAsync()
    {
        await _logger.DisposeAsync();
        try
        {
            var actualFile = AppConstants.ResolveCurrentLogFile();
            if (actualFile != null && File.Exists(actualFile))
                File.Delete(actualFile);
        }
        finally
        {
            _logger = BuildLogger();
        }
    }

    public void SetLogLevel(string level)
    {
        _levelSwitch.MinimumLevel = ParseLevel(level);
        _logger.Information("Log level changed to {Level}.", level);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private Serilog.Core.Logger BuildLogger() =>
        new LoggerConfiguration()
            .MinimumLevel.ControlledBy(_levelSwitch)
            .WriteTo.File(
                path: AppConstants.LogBasePath,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: false,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
            .CreateLogger();

    private static LogEventLevel ParseLevel(string level) =>
        level?.ToLowerInvariant() switch
        {
            "verbose" or "trace" => LogEventLevel.Verbose,
            "debug"              => LogEventLevel.Debug,
            "information" or "info" => LogEventLevel.Information,
            "warning" or "warn"  => LogEventLevel.Warning,
            "error"              => LogEventLevel.Error,
            "fatal"              => LogEventLevel.Fatal,
            _                    => LogEventLevel.Information
        };

    public void Dispose() => _logger.Dispose();
}
