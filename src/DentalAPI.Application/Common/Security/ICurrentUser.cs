using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.Common.Security;

/// <summary>The signed-in caller of the current request, read from the access token. All values are null for anonymous calls.</summary>
public interface ICurrentUser
{
    Guid? AccountId { get; }

    AccountType? AccountType { get; }

    /// <summary>The patient record a patient account is linked to. Never taken from request data.</summary>
    Guid? PatientId { get; }
}
