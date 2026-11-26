namespace DentalAPI.Application.Common.Persistence;

/// <summary>Commits all changes tracked by repositories in the current request as one transaction.</summary>
public interface IUnitOfWork
{
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
