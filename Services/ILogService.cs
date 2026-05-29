namespace FestKasse.Services;

public interface ILogService
{
    // ── Simple log methods ──────────────────────────────────────────────────
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);

    /// <summary>
    /// Logs an exception together with all inner exceptions and the full
    /// stack trace. Optionally prefix with a human-readable context message.
    /// </summary>
    void Exception(Exception ex, string? context = null);

    // ── Log-file management ─────────────────────────────────────────────────
    string LogFilePath { get; }
    Task<string> ReadLogAsync();
    Task ClearLogAsync();

    // ── Runtime reconfiguration ─────────────────────────────────────────────
    void SetLogLevel(string level);
}
