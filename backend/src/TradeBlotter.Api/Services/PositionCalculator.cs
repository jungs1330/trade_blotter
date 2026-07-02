using TradeBlotter.Api.Domain;

namespace TradeBlotter.Api.Services;

/// <summary>
/// Derives current positions (net quantity + moving-average cost) from trade history.
/// </summary>
/// <remarks>
/// <para>This is the accounting heart of the app and the most heavily unit-tested code.
/// It is a pure function of its input — no I/O, no clock — so it can be exercised directly
/// with hand-built trade lists.</para>
///
/// <para><b>Ordering matters.</b> Average cost is order-dependent, so each symbol's trades
/// are processed in <c>(timestamp, id)</c> ascending order. The method sorts internally, so
/// it is correct regardless of the order the caller supplies.</para>
///
/// <para><b>Rules per trade</b> (signed qty: Buy = +q, Sell = −q):</para>
/// <list type="bullet">
///   <item>Flat (net 0): open a fresh position; avg cost = trade price.</item>
///   <item>Same direction as the open position: increase and update the weighted average
///         <c>(|net|·avg + |signed|·price) / |net+signed|</c>.</item>
///   <item>Opposite direction, smaller: partial reduction — net falls, <b>avg unchanged</b>.</item>
///   <item>Opposite direction, equal: position closes (net 0) and is omitted.</item>
///   <item>Opposite direction, larger: sign flip — close then open the opposite position at
///         this trade's price (new avg = that price).</item>
/// </list>
///
/// <para>All money math is in <see cref="decimal"/> at full precision; rounding happens only
/// at the API boundary. Short positions are allowed. Symbols that net to zero are omitted.</para>
/// </remarks>
public static class PositionCalculator
{
    public static IReadOnlyList<Position> Calculate(IEnumerable<Trade> trades)
    {
        var positions = new List<Position>();

        // Symbols are independent; order groups by symbol for a deterministic result.
        foreach (var group in trades
                     .GroupBy(trade => trade.Symbol)
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var orderedTrades = group
                .OrderBy(trade => trade.Timestamp, StringComparer.Ordinal)
                .ThenBy(trade => trade.Id);

            var netQuantity = 0;
            var averageCost = 0m;

            foreach (var trade in orderedTrades)
            {
                var signed = trade.SignedQuantity;

                if (netQuantity == 0)
                {
                    // Open a fresh position (long for a buy, short for a sell).
                    netQuantity = signed;
                    averageCost = trade.Price;
                }
                else if (Math.Sign(netQuantity) == Math.Sign(signed))
                {
                    // Same direction: grow the position and roll the weighted-average cost.
                    var increasedQuantity = netQuantity + signed;
                    averageCost =
                        (Math.Abs(netQuantity) * averageCost + Math.Abs(signed) * trade.Price)
                        / Math.Abs(increasedQuantity);
                    netQuantity = increasedQuantity;
                }
                else
                {
                    // Opposite direction: reduce, close, or flip.
                    var remainingQuantity = netQuantity + signed;

                    if (Math.Sign(remainingQuantity) == Math.Sign(netQuantity))
                    {
                        // Partial reduction — average cost of the surviving position is unchanged.
                        netQuantity = remainingQuantity;
                    }
                    else if (remainingQuantity == 0)
                    {
                        // Fully closed.
                        netQuantity = 0;
                        averageCost = 0m;
                    }
                    else
                    {
                        // Sign flip: the old position is closed and a new opposite one opens
                        // at this trade's price.
                        netQuantity = remainingQuantity;
                        averageCost = trade.Price;
                    }
                }
            }

            // A symbol that ends flat holds no position and is omitted.
            if (netQuantity != 0)
                positions.Add(new Position(group.Key, netQuantity, averageCost));
        }

        return positions;
    }
}
