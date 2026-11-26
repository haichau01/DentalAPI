namespace DentalAPI.Application.Common.Exceptions;

/// <summary>
/// Thrown when a request conflicts with the current state, such as an overlapping appointment.
/// Mapped to HTTP 409. Derive a specific exception per business rule.
/// </summary>
public abstract class ConflictException(string message) : Exception(message);
