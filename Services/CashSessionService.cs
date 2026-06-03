using System.Text.Json;
using FestKasse.Helpers;
using FestKasse.Models;

namespace FestKasse.Services;

public class CashSessionService : ICashSessionService
{
    private static readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly string _filePath = AppConstants.CashSessionFilePath;

    // ── ICashSessionService ───────────────────────────────────────────────

    public async Task<CashSession?> GetOpenSessionAsync()
    {
        var sessions = await LoadAsync();
        return sessions.LastOrDefault(s => s.IsOpen);
    }

    public async Task<CashSession> OpenSessionAsync(decimal openingCash)
    {
        var sessions = await LoadAsync();

        // Auto-close any lingering open session
        foreach (var s in sessions.Where(s => s.IsOpen))
            s.ClosedAt = DateTime.Now;

        var session = new CashSession
        {
            OpenedAt    = DateTime.Now,
            OpeningCash = openingCash
        };
        sessions.Add(session);
        await SaveAsync(sessions);
        return session;
    }

    public async Task<CashSession> CloseSessionAsync(decimal closingCash)
    {
        var sessions = await LoadAsync();
        var open = sessions.LastOrDefault(s => s.IsOpen)
                   ?? throw new InvalidOperationException("No open cash session.");

        open.ClosingCash = closingCash;
        open.ClosedAt    = DateTime.Now;
        await SaveAsync(sessions);
        return open;
    }

    public async Task RecordOrderAsync(decimal total)
    {
        var sessions = await LoadAsync();
        var open = sessions.LastOrDefault(s => s.IsOpen);
        if (open is null) return;

        open.Revenue     += total;
        open.OrderCount  += 1;
        await SaveAsync(sessions);
    }

    public async Task<IReadOnlyList<CashSession>> GetAllSessionsAsync()
        => (await LoadAsync()).AsReadOnly();

    // ── Private helpers ───────────────────────────────────────────────────

    private async Task<List<CashSession>> LoadAsync()
    {
        if (!File.Exists(_filePath)) return [];
        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<List<CashSession>>(stream, _jsonOptions)
                   ?? [];
        }
        catch { return []; }
    }

    private async Task SaveAsync(List<CashSession> sessions)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, sessions, _jsonOptions);
    }
}
