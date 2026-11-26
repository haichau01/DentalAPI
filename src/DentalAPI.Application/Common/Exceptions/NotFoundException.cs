namespace DentalAPI.Application.Common.Exceptions;

/// <summary>Thrown when a requested resource does not exist. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string resourceName, Guid id)
    : Exception($"{resourceName} '{id}' was not found.")
{
    public string ResourceName { get; } = resourceName;

    public Guid ResourceId { get; } = id;
}
