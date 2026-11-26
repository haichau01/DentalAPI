namespace DentalAPI.Domain.Common;

/// <summary>
/// Base type for every persisted entity: identity, audit timestamps and soft delete.
/// Timestamps are set by the persistence layer when changes are saved.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void MarkCreated(DateTimeOffset now)
    {
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void MarkUpdated(DateTimeOffset now)
    {
        UpdatedAt = now;
    }

    public void MarkDeleted(DateTimeOffset now)
    {
        IsDeleted = true;
        DeletedAt = now;
        UpdatedAt = now;
    }
}
