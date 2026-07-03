namespace RpFlo.Domain.Common;

public abstract class AuditableEntity : Entity
{
    public Guid? LastModifiedBy { get; protected set; }

    protected new void Touch() => UpdatedAt = DateTime.UtcNow;

    protected void Touch(Guid modifiedBy)
    {
        UpdatedAt = DateTime.UtcNow;
        LastModifiedBy = modifiedBy;
    }
}
