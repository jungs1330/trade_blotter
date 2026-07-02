namespace TradeBlotter.Api.Contracts;

/// <summary>
/// Wire shape for a derived position (<c>GET /positions</c>). <c>averageCost</c> is
/// rounded to 2dp for display; <c>netQuantity</c> is signed (negative = short).
/// </summary>
public sealed record PositionResponse(string Symbol, int NetQuantity, decimal AverageCost);
