using DentalAPI.Application.Common.Addresses;
using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Domain.Patients;
using DentalAPI.UnitTests.TestDoubles;
using FluentAssertions;
using FluentValidation;

namespace DentalAPI.UnitTests.Auth;

public sealed class PatientRegistrationServiceTests
{
    private const string Phone = "0900000009";

    private readonly AuthTestHarness _harness = new();
    private readonly PatientRegistrationService _service;

    public PatientRegistrationServiceTests()
    {
        _service = _harness.CreatePatientRegistrationService();
    }

    [Fact]
    public async Task RegisterAsync_NewPhone_CreatesPendingAccountAndSendsSms()
    {
        var result = await _service.RegisterAsync(ValidRequest("+84900000009"), CancellationToken.None);

        _harness.PatientAccounts.Accounts.Should().ContainSingle(account => account.PhoneNumber == Phone && !account.IsActive);
        _harness.Messages.Sent.Should().ContainSingle(message => message.Destination == Phone);
        result.VerificationCodeExpiresAt.Should().BeAfter(AuthTestHarness.UtcNow);
    }

    [Fact]
    public async Task RegisterAsync_PhoneOfActiveAccount_ThrowsPhoneNumberAlreadyRegistered()
    {
        _harness.AddActivePatientAccount(Phone);

        var act = () => _service.RegisterAsync(ValidRequest(), CancellationToken.None);

        await act.Should().ThrowAsync<PhoneNumberAlreadyRegisteredException>();
        _harness.Messages.Sent.Should().BeEmpty();
    }

    [Theory]
    [InlineData("short1")]
    [InlineData("onlyletters")]
    [InlineData("12345678")]
    public async Task RegisterAsync_WeakPassword_ThrowsValidationException(string password)
    {
        var act = () => _service.RegisterAsync(ValidRequest() with { Password = password }, CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task VerifyPhoneAsync_CorrectCode_ActivatesAccountWithNewPatientRecord()
    {
        await _service.RegisterAsync(ValidRequest(), CancellationToken.None);

        var tokens = await _service.VerifyPhoneAsync(new(Phone, FakeVerificationCodeGenerator.Code), CancellationToken.None);

        var account = _harness.PatientAccounts.Accounts.Single();
        account.IsActive.Should().BeTrue();
        account.PendingProfile.Should().BeNull();
        var patient = _harness.Patients.Patients.Should().ContainSingle().Subject;
        account.PatientId.Should().Be(patient.Id);
        patient.Address!.Province.Should().Be("Ha Noi");
        tokens.AccessToken.Should().NotBeEmpty();
    }

    [Fact]
    public async Task VerifyPhoneAsync_WrongCode_ThrowsInvalidVerificationCodeAndKeepsAccountPending()
    {
        await _service.RegisterAsync(ValidRequest(), CancellationToken.None);

        var act = () => _service.VerifyPhoneAsync(new(Phone, "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidVerificationCodeException>();
        _harness.PatientAccounts.Accounts.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task ResendCodeAsync_WithinCooldown_ThrowsVerificationCodeCooldown()
    {
        await _service.RegisterAsync(ValidRequest(), CancellationToken.None);

        var act = () => _service.ResendCodeAsync(new(Phone), CancellationToken.None);

        await act.Should().ThrowAsync<VerificationCodeCooldownException>();
    }

    private static RegisterPatientAccountRequest ValidRequest(string phone = Phone) =>
        new("Tran Van E", new DateOnly(1992, 4, 1), phone, "e@example.com", AuthTestHarness.Password,
            new AddressDto("12 Hang Bai", "Phuong Hoan Kiem", "Ha Noi"));
}

public sealed class PatientAccountLinkerTests
{
    private const string Phone = "0900000009";

    private readonly AuthTestHarness _harness = new();
    private readonly PatientAccountLinker _linker;

    public PatientAccountLinkerTests()
    {
        _linker = new PatientAccountLinker(_harness.Patients);
    }

    [Fact]
    public async Task FindOrCreatePatientAsync_OneUnlinkedRecordWithSamePhone_ReturnsThatRecord()
    {
        var existing = AddPatient(Phone);

        var patientId = await _linker.FindOrCreatePatientAsync(PendingAccount(), CancellationToken.None);

        patientId.Should().Be(existing.Id);
        _harness.Patients.Patients.Should().ContainSingle();
        existing.FullName.Should().Be("Recorded By Reception");
    }

    [Fact]
    public async Task FindOrCreatePatientAsync_NoRecordWithPhone_CreatesRecordFromRegistration()
    {
        AddPatient("0911111111");

        var patientId = await _linker.FindOrCreatePatientAsync(PendingAccount(), CancellationToken.None);

        var created = _harness.Patients.Patients.Single(patient => patient.Id == patientId);
        created.FullName.Should().Be("Nguyen Van A");
        created.PhoneNumber.Should().Be(Phone);
    }

    [Fact]
    public async Task FindOrCreatePatientAsync_SeveralRecordsWithPhone_CreatesNewRecord()
    {
        var first = AddPatient(Phone);
        var second = AddPatient(Phone);

        var patientId = await _linker.FindOrCreatePatientAsync(PendingAccount(), CancellationToken.None);

        patientId.Should().NotBe(first.Id).And.NotBe(second.Id);
        _harness.Patients.Patients.Should().HaveCount(3);
    }

    [Fact]
    public async Task FindOrCreatePatientAsync_MatchingRecordAlreadyLinked_CreatesNewRecord()
    {
        var linked = AddPatient(Phone);
        _harness.Patients.LinkedPatientIds.Add(linked.Id);

        var patientId = await _linker.FindOrCreatePatientAsync(PendingAccount(), CancellationToken.None);

        patientId.Should().NotBe(linked.Id);
    }

    private Patient AddPatient(string phone)
    {
        var patient = new Patient(new PatientDetails("Recorded By Reception", new DateOnly(1980, 1, 1), phone, null, null));
        _harness.Patients.Add(patient);
        return patient;
    }

    private Domain.Accounts.PatientAccount PendingAccount() =>
        new(Phone, null, _harness.PasswordHasher.Hash(AuthTestHarness.Password), AuthTestHarness.PendingProfile());
}
