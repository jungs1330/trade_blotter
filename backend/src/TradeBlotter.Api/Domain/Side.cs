namespace TradeBlotter.Api.Domain;

/// <summary>
/// Trade direction. A <see cref="Buy"/> contributes <c>+quantity</c> to a position,
/// a <see cref="Sell"/> contributes <c>-quantity</c>. These are the only two legal
/// values; the API validates the incoming string against them exactly.
/// </summary>
public enum Side
{
    Buy,
    Sell
}
