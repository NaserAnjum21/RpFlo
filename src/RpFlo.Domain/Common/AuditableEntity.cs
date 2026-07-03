namespace RpFlo.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public Guid? LastModifiedBy { get; protected set; }

    protected new void Touch() => UpdatedAt = DateTimeOffset.UtcNow;

    protected void Touch(Guid modifiedBy)
    {
        UpdatedAt = DateTimeOffset.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
