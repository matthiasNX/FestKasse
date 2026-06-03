namespace FestKasse.Services;

public interface ICashSessionService
{
    /// <summary>Returns the current open session or null if the drawer is closed.</summary>
    Task<Models.CashSession?> GetOpenSessionAsync();

    /// <summary>Opens a new cash session with the given opening cash amount.</summary>
    Task<Models.CashSession> OpenSessionAsync(decimal openingCash);

    /// <summary>Closes the current session and records the closing cash count.</summary>
    Task<Models.CashSession> CloseSessionAsync(decimal closingCash);

    /// <summary>Records a completed order against the current open session.</summary>
    Task RecordOrderAsync(decimal total);

    /// <summary>Returns all historical sessions.</summary>
    Task<IReadOnlyList<Models.CashSession>> GetAllSessionsAsync();
}
