using System.Globalization;
using Dapper;
using TradeBlotter.Api.Domain;

namespace TradeBlotter.Api.Data;

/// <summary>
/// Read/write access to the <c>trades</c> ledger.
/// </summary>
/// <remarks>
/// The repository is the single boundary where the TEXT-decimal storage convention
/// is translated to/from C# <see cref="decimal"/> (see <see cref="TradeRow"/>). No
/// monetary arithmetic happens in SQL — reads return rows and the app does the math.
/// </remarks>
public sealed class TradeRepository
{
    private readonly Database _db;

    public TradeRepository(Database db) => _db = db;

    /// <summary>
    /// Persist a validated trade and return the resulting ledger row, including the
    /// server-assigned <c>id</c> and <c>timestamp</c>.
    /// </summary>
    /// <remarks>
    /// The timestamp is generated here (UTC, millisecond precision, ISO-8601 with a
    /// trailing <c>Z</c>) so it is authoritative and any client value is irrelevant.
    /// Price is written as a canonical invariant-culture decimal string. The row id is
    /// obtained with SQLite's <c>RETURNING</c> clause in the same statement.
    /// </remarks>
    public Trade InsertTrade(CreateTradeCommand command)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        var priceText = command.Price.ToString(CultureInfo.InvariantCulture);

        using var connection = _db.OpenConnection();
        var id = connection.ExecuteScalar<long>(
            "INSERT INTO trades (symbol, side, quantity, price, timestamp) " +
            "VALUES (@Symbol, @Side, @Quantity, @Price, @Timestamp) " +
            "RETURNING id;",
            new
            {
                command.Symbol,
                Side = command.Side.ToString(),
                command.Quantity,
                Price = priceText,
                Timestamp = timestamp
            });

        return new Trade(id, command.Symbol, command.Side, command.Quantity, command.Price, timestamp);
    }

    /// <summary>
    /// All trades ordered newest-first for <c>GET /trades</c>. The <c>id</c> tiebreaker
    /// makes ordering deterministic when two trades share a millisecond-precision timestamp.
    /// </summary>
    public IReadOnlyList<Trade> GetAllNewestFirst()
    {
        using var connection = _db.OpenConnection();
        return connection
            .Query<TradeRow>(
                "SELECT id, symbol, side, quantity, price, timestamp " +
                "FROM trades ORDER BY timestamp DESC, id DESC")
            .Select(row => row.ToTrade())
            .ToList();
    }
}

/// <summary>
/// Wire type that mirrors the raw <c>trades</c> row shape (Dapper maps by column name).
/// Kept separate from <see cref="Trade"/> so the TEXT→decimal / TEXT→enum parsing lives
/// in exactly one place.
/// </summary>
internal sealed class TradeRow
{
    public long Id { get; set; }
    public string Symbol { get; set; } = "";
    public string Side { get; set; } = "";
    public long Quantity { get; set; }
    public string Price { get; set; } = "";
    public string Timestamp { get; set; } = "";

    public Trade ToTrade() => new(
        Id,
        Symbol,
        Enum.Parse<Side>(Side),
        (int)Quantity,
        // Invariant culture is enforced process-wide (InvariantGlobalization), but we
        // pass it explicitly so the parse contract is obvious and locale-independent.
        decimal.Parse(Price, CultureInfo.InvariantCulture),
        Timestamp);
}
