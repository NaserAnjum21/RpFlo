using RpFlo.Domain.Common;
using RpFlo.Domain.Enums;

namespace RpFlo.Domain.Entities;

public sealed class User : Entity
{
    public string Name { get; private init; } = string.Empty;
    public string Email { get; private init; } = string.Empty;
    public UserRole Role { get; private init; }
    public Department Department { get; private init; }

    private User() { }

    public static User Create(string name, string email, UserRole role, Department department) =>
        new()
        {
            Name = name,
            Email = email,
            Role = role,
            Department = department
        };
}
