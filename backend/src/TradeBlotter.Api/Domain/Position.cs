namespace TradeBlotter.Api.Domain;

/// <summary>
/// A derived open position for one symbol. Never stored — always recomputed from the
/// trade history by <see cref="Services.PositionCalculator"/>.
/// </summary>
/// <remarks>
/// <see cref="NetQuantity"/> is signed (positive = long, negative = short) and is never
/// zero here (flat symbols are omitted). <see cref="AverageCost"/> is the moving-average
/// cost of the open position, carried at full precision; the API rounds it for display.
/// </remarks>
public sealed record Position(string Symbol, int NetQuantity, decimal AverageCost);
