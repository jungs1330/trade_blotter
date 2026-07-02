namespace TradeBlotter.Api.Contracts;

/// <summary>
/// Raw inbound body for <c>POST /trades</c>. Every field is nullable on purpose so
/// that validation — not the JSON deserializer — decides what is missing or invalid,
/// and can report a precise field + message.
/// </summary>
/// <remarks>
/// <para><c>Quantity</c> is modelled as <see cref="decimal"/> (not <see cref="int"/>)
/// so a fractional value such as <c>100.5</c> deserializes successfully and is then
/// rejected by validation as "not a whole number", instead of surfacing an opaque
/// deserialization failure.</para>
/// <para>Any client-supplied <c>id</c>/<c>timestamp</c> is intentionally absent here
/// and thus ignored — both are server-generated.</para>
/// </remarks>
public sealed class CreateTradeRequest
{
    public string? Symbol { get; set; }
    public string? Side { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? Price { get; set; }
}
