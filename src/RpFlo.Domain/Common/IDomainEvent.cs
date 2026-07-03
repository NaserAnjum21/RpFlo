namespace RpFlo.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredAt { get; }
}
