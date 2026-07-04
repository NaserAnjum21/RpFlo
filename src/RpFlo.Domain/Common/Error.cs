namespace RpFlo.Domain.Common;

public sealed record Error(string Code, string Message)
{
    public static Error Validation(string code, string message) => new(Code: $"Validation.{code}", Message: message);
    public static Error NotFound(string code, string message) => new(Code: $"NotFound.{code}", Message: message);
    public static Error Unauthorized(string code, string message) => new(Code: $"Unauthorized.{code}", Message: message);
    public static Error Conflict(string code, string message) => new(Code: $"Conflict.{code}", Message: message);
    public static Error Domain(string code, string message) => new(Code: $"Domain.{code}", Message: message);
}
