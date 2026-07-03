namespace RpFlo.Domain.Common;

public sealed record Error(string Code, string Message)
{
    public static Error Validation(string code, string message) => new($"Validation.{code}", message);
    public static Error NotFound(string code, string message) => new($"NotFound.{code}", message);
    public static Error Unauthorized(string code, string message) => new($"Unauthorized.{code}", message);
    public static Error Conflict(string code, string message) => new($"Conflict.{code}", message);
    public static Error Domain(string code, string message) => new($"Domain.{code}", message);
}
