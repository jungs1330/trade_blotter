namespace TradeBlotter.Api.Contracts;

/// <summary>
/// The wire shape returned for a trade by <c>POST /trades</c> and <c>GET /trades</c>.
/// </summary>
/// <remarks>
/// Serialized as camelCase JSON (ASP.NET Core web defaults). <c>side</c> is a plain
/// string (<c>"Buy"</c>/<c>"Sell"</c>). <c>price</c> is returned as entered (full
/// precision); <c>notional</c> is rounded to 2dp for display.
/// </remarks>
public sealed record TradeResponse(
    long Id,
    string Symbol,
    string Side,
    int Quantity,
    decimal Price,
    decimal Notional,
    string Timestamp);
