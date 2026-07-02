using System.Globalization;
using TradeBlotter.Api.Common;
using Xunit;

namespace TradeBlotter.Tests;

/// <summary>
/// Pins the monetary rounding policy: 2 decimal places, half-up (away from zero).
/// </summary>
public class MoneyTests
{
    [Theory]
    [InlineData("151.66666666", "151.67")] // the moving-average example
    [InlineData("2.125", "2.13")]           // half rounds up (away from zero)
    [InlineData("2.135", "2.14")]           // half rounds up (away from zero)
    [InlineData("-2.125", "-2.13")]         // away from zero for negatives too
    [InlineData("800", "800.00")]           // exact values keep 2dp scale
    public void Round_IsTwoDecimalsHalfUp(string input, string expected)
    {
        var value = decimal.Parse(input, CultureInfo.InvariantCulture);
        var want = decimal.Parse(expected, CultureInfo.InvariantCulture);
        Assert.Equal(want, Money.Round(value));
    }
}
