using TradeBlotter.Api.Common;
using TradeBlotter.Api.Domain;

namespace TradeBlotter.Api.Contracts;

/// <summary>
/// Maps domain types to their API wire shapes, applying display rounding at the boundary.
/// </summary>
public static class ApiMappings
{
    public static TradeResponse ToResponse(this Trade trade) => new(
        trade.Id,
        trade.Symbol,
        trade.Side.ToString(),
        trade.Quantity,
        trade.Price,                     // price is returned as entered (not rounded)
        Money.Round(trade.Notional),     // notional rounded to 2dp for display
        trade.Timestamp);

    public static PositionResponse ToResponse(this Position position) => new(
        position.Symbol,
        position.NetQuantity,
        Money.Round(position.AverageCost));  // average cost rounded to 2dp for display
}
