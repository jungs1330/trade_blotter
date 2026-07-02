namespace TradeBlotter.Api.Common;

/// <summary>
/// Central place for the project's monetary rounding policy.
/// </summary>
/// <remarks>
/// All money math is done in <see cref="decimal"/> at full precision; rounding is
/// applied only at the API boundary for <c>notional</c> and <c>averageCost</c>
/// display values. The policy is "round half-up" (<see cref="MidpointRounding.AwayFromZero"/>),
/// applied consistently so results never depend on banker's rounding surprises.
/// </remarks>
public static class Money
{
    /// <summary>Round a monetary value to 2 decimal places, half-up.</summary>
    public static decimal Round(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
