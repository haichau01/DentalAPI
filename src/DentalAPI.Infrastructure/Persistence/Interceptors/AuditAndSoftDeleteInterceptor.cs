using DentalAPI.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DentalAPI.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps audit timestamps on save and turns deletes into soft deletes,
/// so patient, treatment and invoice history is never physically removed.
/// </summary>
public sealed class AuditAndSoftDeleteInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
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
        foreach (var entry in context.ChangeTracker.Entries<Entity>())
        {
            ApplyAuditRule(entry, now);
        }
    }

    private static void ApplyAuditRule(EntityEntry<Entity> entry, DateTimeOffset now)
    {
        switch (entry.State)
        {
            case EntityState.Added:
                entry.Entity.MarkCreated(now);
                break;
            case EntityState.Modified:
                entry.Entity.MarkUpdated(now);
                break;
            case EntityState.Deleted:
                entry.State = EntityState.Modified;
                entry.Entity.MarkDeleted(now);
                break;
        }
    }
}
