namespace RpFlo.Application.DTOs;

public sealed record UserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string Department);
