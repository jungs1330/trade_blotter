using System.Text.Json.Serialization;

namespace TradeBlotter.Api.Contracts;

/// <summary>
/// Consistent error body returned for every non-success status:
/// <c>{ "error": { "code": ..., "message": ..., "field": ... } }</c>.
/// </summary>
public sealed record ErrorEnvelope(ApiError Error)
{
    /// <summary>Convenience factory for validation failures (HTTP 400).</summary>
    public static ErrorEnvelope Validation(string message, string? field) =>
        new(new ApiError("VALIDATION_ERROR", message, field));

    /// <summary>Factory for a generic error with an explicit code (e.g. 404/500).</summary>
    public static ErrorEnvelope Of(string code, string message) =>
        new(new ApiError(code, message, null));
}

/// <summary>
/// The error detail. <c>field</c> is omitted from the JSON when it is null (i.e. for
/// non-field errors like 404/500), so validation errors carry a field and others don't.
/// </summary>
public sealed record ApiError(
    string Code,
    string Message,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Field);
