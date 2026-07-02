using System.Globalization;
using TradeBlotter.Api.Common;
using TradeBlotter.Api.Domain;
using TradeBlotter.Api.Services;
using Xunit;

namespace TradeBlotter.Tests;

/// <summary>
/// Tests for the accounting heart of the app. These cover every rule in the spec:
/// moving-average across buys, partial sell (avg unchanged), net-zero omission,
/// short positions, sign flips, and 2dp display rounding.
/// </summary>
public class PositionCalculatorTests
{
    // ---- helpers -----------------------------------------------------------

    /// <summary>
    /// Build a trade list from terse tuples, assigning ascending ids and timestamps in
    /// the order given (so list order == chronological order).
    /// </summary>
    private static List<Trade> Trades(params (string Symbol, Side Side, int Qty, decimal Price)[] items)
    {
        var baseTime = new DateTime(2026, 6, 29, 9, 0, 0, DateTimeKind.Utc);
        var list = new List<Trade>();
        for (var i = 0; i < items.Length; i++)
        {
            var ts = baseTime.AddMinutes(i).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
            var it = items[i];
            list.Add(new Trade(i + 1, it.Symbol, it.Side, it.Qty, it.Price, ts));
        }
        return list;
    }

    private static Position Only(IReadOnlyList<Position> positions)
    {
        Assert.Single(positions);
        return positions[0];
    }

    private static Position For(IReadOnlyList<Position> positions, string symbol) =>
        positions.Single(p => p.Symbol == symbol);

    // ---- tests -------------------------------------------------------------

    [Fact]
    public void SingleBuy_OpensPositionAtTradePrice()
    {
        var positions = PositionCalculator.Calculate(Trades(
            ("NVDA", Side.Buy, 10, 800m)));

        var nvda = Only(positions);
        Assert.Equal("NVDA", nvda.Symbol);
        Assert.Equal(10, nvda.NetQuantity);
        Assert.Equal(800.00m, Money.Round(nvda.AverageCost));
    }

    [Fact]
    public void TwoBuys_RollWeightedAverageCost()
    {
        var positions = PositionCalculator.Calculate(Trades(
            ("AAPL", Side.Buy, 100, 150m),
            ("AAPL", Side.Buy, 50, 155m)));

        var aapl = Only(positions);
        Assert.Equal(150, aapl.NetQuantity);
        // (100*150 + 50*155) / 150 = 22750 / 150 = 151.66666...
        Assert.Equal(151.6667m, Math.Round(aapl.AverageCost, 4));
        Assert.Equal(151.67m, Money.Round(aapl.AverageCost));
    }

    [Fact]
    public void PartialSell_ReducesQuantity_LeavesAverageUnchanged()
    {
        // The full AAPL worked example from the spec.
        var positions = PositionCalculator.Calculate(Trades(
            ("AAPL", Side.Buy, 100, 150m),
            ("AAPL", Side.Buy, 50, 155m),
            ("AAPL", Side.Sell, 60, 160m)));

        var aapl = Only(positions);
        Assert.Equal(90, aapl.NetQuantity);
        Assert.Equal(151.6667m, Math.Round(aapl.AverageCost, 4)); // unchanged by the sell
        Assert.Equal(151.67m, Money.Round(aapl.AverageCost));
    }

    [Fact]
    public void PositionClosedToZero_IsOmitted()
    {
        var positions = PositionCalculator.Calculate(Trades(
            ("MSFT", Side.Buy, 200, 300m),
            ("MSFT", Side.Sell, 200, 310m)));

        Assert.Empty(positions);
    }

    [Fact]
    public void ShortPosition_IsAllowed_WithAverageOfShortedFills()
    {
        // The TSLA seed scenario: open short, then partially cover.
        var positions = PositionCalculator.Calculate(Trades(
            ("TSLA", Side.Sell, 50, 250m),
            ("TSLA", Side.Buy, 20, 240m)));

        var tsla = Only(positions);
        Assert.Equal(-30, tsla.NetQuantity);
        Assert.Equal(250.00m, Money.Round(tsla.AverageCost)); // cover doesn't change avg
    }

    [Fact]
    public void GrowingShort_RollsWeightedAverage()
    {
        var positions = PositionCalculator.Calculate(Trades(
            ("TSLA", Side.Sell, 50, 250m),
            ("TSLA", Side.Sell, 30, 240m)));

        var tsla = Only(positions);
        Assert.Equal(-80, tsla.NetQuantity);
        // (50*250 + 30*240) / 80 = 19700 / 80 = 246.25
        Assert.Equal(246.25m, Money.Round(tsla.AverageCost));
    }

    [Fact]
    public void SignFlip_ClosesThenOpensOppositeAtFlipPrice()
    {
        // Long 100, then Sell 150 -> short 50 at the flipping trade's price.
        var positions = PositionCalculator.Calculate(Trades(
            ("AAPL", Side.Buy, 100, 150m),
            ("AAPL", Side.Sell, 150, 160m)));

        var aapl = Only(positions);
        Assert.Equal(-50, aapl.NetQuantity);
        Assert.Equal(160.00m, Money.Round(aapl.AverageCost));
    }

    [Fact]
    public void MultipleSymbols_MatchSeedExpectations()
    {
        var positions = PositionCalculator.Calculate(Trades(
            ("AAPL", Side.Buy, 100, 150m),
            ("AAPL", Side.Buy, 50, 155m),
            ("AAPL", Side.Sell, 60, 160m),
            ("MSFT", Side.Buy, 200, 300m),
            ("MSFT", Side.Sell, 200, 310m),
            ("TSLA", Side.Sell, 50, 250m),
            ("TSLA", Side.Buy, 20, 240m),
            ("NVDA", Side.Buy, 10, 800m)));

        Assert.Equal(3, positions.Count);           // MSFT nets to zero -> omitted
        Assert.DoesNotContain(positions, p => p.Symbol == "MSFT");

        Assert.Equal(90, For(positions, "AAPL").NetQuantity);
        Assert.Equal(151.67m, Money.Round(For(positions, "AAPL").AverageCost));

        Assert.Equal(-30, For(positions, "TSLA").NetQuantity);
        Assert.Equal(250.00m, Money.Round(For(positions, "TSLA").AverageCost));

        Assert.Equal(10, For(positions, "NVDA").NetQuantity);
        Assert.Equal(800.00m, Money.Round(For(positions, "NVDA").AverageCost));
    }

    [Fact]
    public void Calculation_IsOrderedByTimestamp_NotInputOrder()
    {
        // Feed the AAPL trades in a scrambled input order but with timestamps that encode
        // the true sequence; the calculator must sort them and still produce net 90 / 151.67.
        var t1 = new Trade(1, "AAPL", Side.Buy, 100, 150m, "2026-06-29T09:00:00.000Z");
        var t2 = new Trade(2, "AAPL", Side.Buy, 50, 155m, "2026-06-29T09:01:00.000Z");
        var t3 = new Trade(3, "AAPL", Side.Sell, 60, 160m, "2026-06-29T09:02:00.000Z");

        var positions = PositionCalculator.Calculate(new[] { t3, t1, t2 });

        var aapl = Only(positions);
        Assert.Equal(90, aapl.NetQuantity);
        Assert.Equal(151.67m, Money.Round(aapl.AverageCost));
    }
}
