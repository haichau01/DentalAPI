using DentalAPI.Application.Common.Addresses;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Common;
using FluentAssertions;

namespace DentalAPI.UnitTests.Validation;

public sealed class AddressValidatorTests
{
    private readonly AddressValidator _validator = new();

    [Fact]
    public void Validate_ProvinceOnly_IsValid()
    {
        _validator.Validate(new AddressDto(null, null, "Da Nang")).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WardWithoutProvince_HasProvinceError()
    {
        var result = _validator.Validate(new AddressDto("1 Tran Phu", "Phuong Hai Chau", " "));

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(AddressDto.Province));
    }

    [Fact]
    public void Validate_StreetLineOverMaxLength_HasStreetLineError()
    {
        var address = new AddressDto(new string('a', Address.StreetLineMaxLength + 1), null, "Da Nang");

        var result = _validator.Validate(address);

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(AddressDto.StreetLine));
    }
}

public sealed class CreateStaffAccountRequestValidatorTests
{
    private readonly CreateStaffAccountRequestValidator _validator = new();

    [Theory]
    [InlineData(StaffRole.Dentist)]
    [InlineData(StaffRole.Receptionist)]
    public void Validate_StaffRole_IsValid(StaffRole role)
    {
        _validator.Validate(ValidRequest() with { Role = role }).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_AdminRole_HasRoleError()
    {
        var result = _validator.Validate(ValidRequest() with { Role = StaffRole.Admin });

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(CreateStaffAccountRequest.Role));
    }

    [Fact]
    public void Validate_InvalidAddress_HasNestedAddressError()
    {
        var result = _validator.Validate(ValidRequest() with { Address = new AddressDto("1 Le Loi", null, string.Empty) });

        result.Errors.Should().Contain(error => error.PropertyName == "Address.Province");
    }

    private static CreateStaffAccountRequest ValidRequest() =>
        new("dentist@example.com", "Initial123", StaffRole.Dentist, "Pham Nha Si", "0912345678",
            new AddressDto("1 Le Loi", "Phuong Ben Nghe", "TP Ho Chi Minh"));
}
