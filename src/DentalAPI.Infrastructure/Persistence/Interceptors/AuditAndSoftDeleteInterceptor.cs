using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DentalAPI.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps audit timestamps and the acting account on save and turns deletes into soft deletes,
/// so patient, treatment and invoice history is never physically removed.
/// </summary>
public sealed class AuditAndSoftDeleteInterceptor(TimeProvider timeProvider, ICurrentUser currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAuditRules(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ApplyAuditRules(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAuditRules(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var actorId = currentUser.AccountId;
        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            ApplyAuditRule(entry, now, actorId);
        }
    }

    private static void ApplyAuditRule(EntityEntry<Entity> entry, DateTimeOffset now, Guid? actorId)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.MarkCreated(now, actorId);
                break;
            case EntityState.Modified:
                entry.Entity.MarkUpdated(now, actorId);
                break;
            case EntityState.Deleted:
                entry.State = EntityState.Modified;
                RestoreOwnedValues(entry);
                entry.Entity.MarkDeleted(now, actorId);
                break;
        }
    }

    // Deleting an owner also marks its owned values (address, lockout) as deleted, which would
    // null their columns, so they are put back when the delete becomes a soft delete.
    private static void RestoreOwnedValues(EntityEntry entry)
    {
        foreach (var reference in entry.References)
        {
            if (reference.TargetEntry is not { } ownedEntry || !ownedEntry.Metadata.IsOwned())
            {
                continue;
            }

            if (ownedEntry.State == EntityState.Deleted)
            {
                ownedEntry.State = EntityState.Unchanged;
            }

            RestoreOwnedValues(ownedEntry);
        }
    }
}
