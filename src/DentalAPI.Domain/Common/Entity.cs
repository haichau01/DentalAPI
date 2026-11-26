namespace DentalAPI.Domain.Common;

/// <summary>
/// Base type for every persisted entity: identity, audit timestamps, audit actor and soft delete.
/// Audit fields are set by the persistence layer when changes are saved.
/// </summary>
public abstract class Entity
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>The account that created the record; null for anonymous actions such as self-registration.</summary>
    public Guid? CreatedBy { get; private set; }

    public Guid? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAt { get; private set; }

    public void MarkCreated(DateTimeOffset now, Guid? actorId)
    {
        CreatedAt = now;
        UpdatedAt = now;
        CreatedBy = actorId;
        UpdatedBy = actorId;
    }

    public void MarkUpdated(DateTimeOffset now, Guid? actorId)
    {
        UpdatedAt = now;
        UpdatedBy = actorId;
    }

    public void MarkDeleted(DateTimeOffset now, Guid? actorId)
    {
        IsDeleted = true;
        DeletedAt = now;
        MarkUpdated(now, actorId);
    }
}
