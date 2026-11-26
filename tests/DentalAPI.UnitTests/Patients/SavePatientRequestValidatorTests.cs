using DentalAPI.Application.Patients;
using DentalAPI.UnitTests.TestDoubles;
using FluentAssertions;

namespace DentalAPI.UnitTests.Patients;

public sealed class SavePatientRequestValidatorTests
{
    private readonly SavePatientRequestValidator _validator = new(new FixedTimeProvider(PatientTestData.UtcNow));

    [Fact]
    public void Validate_ValidRequest_HasNoErrors()
    {
        var result = _validator.Validate(PatientTestData.ValidRequest());

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyFullName_HasFullNameError()
    {
        var request = PatientTestData.ValidRequest() with { FullName = " " };

        var result = _validator.Validate(request);

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(SavePatientRequest.FullName));
    }

    [Fact]
    public void Validate_DateOfBirthTomorrowInClinicTimeZone_HasDateOfBirthError()
    {
        var request = PatientTestData.ValidRequest() with { DateOfBirth = new DateOnly(2026, 6, 16) };

        var result = _validator.Validate(request);

        result.Errors.Should().ContainSingle(error => error.PropertyName == nameof(SavePatientRequest.DateOfBirth));
    }

    [Fact]
    public void Validate_DateOfBirthTodayInClinicTimeZoneButTomorrowInUtc_IsValid()
    {
        // 18:00 UTC on 15 June is already 01:00 on 16 June in Vietnam.
        var lateEveningUtc = new FixedTimeProvider(new DateTimeOffset(2026, 6, 15, 18, 0, 0, TimeSpan.Zero));
        var validator = new SavePatientRequestValidator(lateEveningUtc);
        var request = PatientTestData.ValidRequest() with { DateOfBirth = new DateOnly(2026, 6, 16) };

        var result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12345")]
    [InlineData("0900 000 001")]
    public void Validate_MalformedPhoneNumber_HasPhoneNumberError(string phoneNumber)
    {
        var request = PatientTestData.ValidRequest() with { PhoneNumber = phoneNumber };

        var result = _validator.Validate(request);

        result.Errors.Should().Contain(error => error.PropertyName == nameof(SavePatientRequest.PhoneNumber));
    }

    [Fact]
    public void Validate_MissingEmail_IsValid()
    {
        var request = PatientTestData.ValidRequest() with { Email = null };

        var result = _validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }
}
