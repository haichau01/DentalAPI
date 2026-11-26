using DentalAPI.Application.Common.Addresses;
using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.StaffAccounts;

public sealed record StaffAccountDto(
    Guid Id,
    string Email,
    StaffRole Role,
    string FullName,
    string PhoneNumber,
    AddressDto? Address,
    bool MustChangePassword,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt)
{
    public static StaffAccountDto FromEntity(StaffAccount account) => new(
        account.Id,
        account.Email,
        account.Role,
        account.FullName,
        account.PhoneNumber,
        AddressDto.FromDomain(account.Address),
        account.MustChangePassword,
        account.CreatedAt,
        account.UpdatedAt);
}
