namespace TradeBlotter.Api.Domain;

/// <summary>
/// A persisted trade — one immutable row of the append-only ledger.
/// </summary>
/// <remarks>
/// This is the in-application representation: <see cref="Price"/> is a real
/// <see cref="decimal"/> (the database stores it as a canonical decimal string).
/// <see cref="Timestamp"/> is the server-generated ISO-8601 UTC string; it is kept
/// as text because lexical order equals chronological order, which is exactly what
/// both the newest-first read and the oldest-first accounting fold rely on.
/// </remarks>
public sealed record Trade(
    long Id,
    string Symbol,
    Side Side,
    int Quantity,
    decimal Price,
    string Timestamp)
{
    /// <summary>
    /// Notional value of the trade = <see cref="Quantity"/> × <see cref="Price"/>.
    /// Kept at full precision here; the API rounds it to 2dp for display.
    /// </summary>
    public decimal Notional => Quantity * Price;

    /// <summary>Signed quantity: <c>+Quantity</c> for a Buy, <c>-Quantity</c> for a Sell.</summary>
    public int SignedQuantity => Side == Side.Buy ? Quantity : -Quantity;
}
