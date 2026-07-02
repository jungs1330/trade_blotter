namespace TradeBlotter.Api.Domain;

/// <summary>
/// A validated, well-typed request to record a trade. Producing one of these is the
/// only way past validation: symbol is already trimmed + uppercased, side is a real
/// <see cref="Side"/>, and quantity/price are guaranteed positive (quantity a whole
/// number). The repository can persist it without any further checks.
/// </summary>
public sealed record CreateTradeCommand(string Symbol, Side Side, int Quantity, decimal Price);
