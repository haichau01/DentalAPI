using DentalAPI.Domain.Common;
using FluentValidation;

namespace DentalAPI.Application.Common.Addresses;

/// <summary>A structured address: street line, ward/commune and province/city (two-level model, no district).</summary>
public sealed record AddressDto(string? StreetLine, string? Ward, string Province)
{
    public Address ToDomain() => new(StreetLine, Ward, Province);

    public static AddressDto? FromDomain(Address? address) =>
        address is null ? null : new AddressDto(address.StreetLine, address.Ward, address.Province);
}

public sealed class AddressValidator : AbstractValidator<AddressDto>
{
    public AddressValidator()
    {
        RuleFor(address => address.StreetLine).MaximumLength(Address.StreetLineMaxLength);
        RuleFor(address => address.Ward).MaximumLength(Address.WardMaxLength);

        // A ward or street alone is ambiguous across the country, so the province is always required.
        RuleFor(address => address.Province)
            .NotEmpty().WithMessage("Tỉnh/thành phố là bắt buộc khi nhập địa chỉ.")
            .MaximumLength(Address.ProvinceMaxLength);
    }
}
