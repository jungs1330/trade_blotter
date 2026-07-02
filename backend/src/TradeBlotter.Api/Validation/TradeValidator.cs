using TradeBlotter.Api.Contracts;
using TradeBlotter.Api.Domain;

namespace TradeBlotter.Api.Validation;

/// <summary>
/// Server-side validation for <c>POST /trades</c>. The client is never trusted:
/// every rule here is authoritative and mirrors what the frontend also checks.
/// </summary>
/// <remarks>
/// Returns a discriminated result as a tuple: exactly one of <c>command</c> (valid)
/// or <c>error</c> (the 400 envelope) is non-null. Rules, in field order, match the
/// API contract: non-empty symbol; side exactly <c>Buy</c>/<c>Sell</c>; quantity a
/// positive whole number; price positive.
/// </remarks>
public static class TradeValidator
{
    public static (CreateTradeCommand? command, ErrorEnvelope? error) Validate(CreateTradeRequest? request)
    {
        if (request is null)
            return (null, ErrorEnvelope.Validation("Request body is required.", null));

        // symbol — trimmed + uppercased server-side (e.g. "aapl " -> "AAPL").
        var symbol = request.Symbol?.Trim().ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(symbol))
            return (null, ErrorEnvelope.Validation("Symbol is required.", "symbol"));

        // side — must be exactly "Buy" or "Sell" (case-sensitive; no numeric/aliased values).
        Side side;
        if (request.Side == "Buy") side = Side.Buy;
        else if (request.Side == "Sell") side = Side.Sell;
        else return (null, ErrorEnvelope.Validation("Side must be exactly 'Buy' or 'Sell'.", "side"));

        // quantity — required, whole number, > 0, and within int range.
        if (request.Quantity is not { } quantity)
            return (null, ErrorEnvelope.Validation("Quantity is required.", "quantity"));
        if (quantity != Math.Truncate(quantity))
            return (null, ErrorEnvelope.Validation("Quantity must be a whole number of shares.", "quantity"));
        if (quantity <= 0)
            return (null, ErrorEnvelope.Validation("Quantity must be greater than zero.", "quantity"));
        if (quantity > int.MaxValue)
            return (null, ErrorEnvelope.Validation("Quantity is too large.", "quantity"));

        // price — required, > 0. Full decimal precision is preserved.
        if (request.Price is not { } price)
            return (null, ErrorEnvelope.Validation("Price is required.", "price"));
        if (price <= 0)
            return (null, ErrorEnvelope.Validation("Price must be greater than zero.", "price"));

        return (new CreateTradeCommand(symbol!, side, (int)quantity, price), null);
    }
}
