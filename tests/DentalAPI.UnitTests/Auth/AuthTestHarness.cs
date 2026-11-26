using DentalAPI.Application.Auth;
using DentalAPI.Application.Authorization;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Application.StaffAccounts;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Common;
using DentalAPI.UnitTests.TestDoubles;

namespace DentalAPI.UnitTests.Auth;

/// <summary>Wires the auth services to in-memory fakes so each test only states what is different.</summary>
internal sealed class AuthTestHarness
{
    public const string Password = "Secret123";

    // 2026-06-15 10:00 in Vietnam (UTC+7).
    public static readonly DateTimeOffset UtcNow = new(2026, 6, 15, 3, 0, 0, TimeSpan.Zero);

    public FixedTimeProvider TimeProvider { get; } = new(UtcNow);

    public InMemoryPatientRepository Patients { get; } = new();

    public InMemoryPatientAccountRepository PatientAccounts { get; } = new();

    public InMemoryStaffAccountRepository StaffAccounts { get; } = new();

    public InMemoryRefreshTokenRepository RefreshTokens { get; } = new();

    public InMemoryVerificationCodeRepository VerificationCodes { get; } = new();

    public InMemoryPermissionRepository Permissions { get; } = new();

    public FakePasswordHasher PasswordHasher { get; } = new();

    public FakeVerificationCodeGenerator CodeGenerator { get; } = new();

    public RecordingMessageSender Messages { get; } = new();

    public FakeCurrentUser CurrentUser { get; } = new();

    public CountingUnitOfWork UnitOfWork { get; } = new();

    public FakeClientContext ClientContext { get; } = new();

    public FakeLoginAttemptThrottle LoginThrottle { get; } = new();

    public FakeRefreshTokenFactory RefreshTokenFactory => _refreshTokenFactory ??= new FakeRefreshTokenFactory(TimeProvider);

    public RecordingPermissionProvider PermissionProvider =>
        _permissionProvider ??= new RecordingPermissionProvider(new EffectivePermissionLoader(StaffAccounts, Permissions));

    private FakeRefreshTokenFactory? _refreshTokenFactory;
    private RecordingPermissionProvider? _permissionProvider;

    public TokenPairIssuer CreateTokenPairIssuer() =>
        new(new FakeAccessTokenIssuer(TimeProvider), RefreshTokenFactory, RefreshTokens);

    public PasswordCredentialChecker CreateCredentialChecker() =>
        new(PasswordHasher, LoginThrottle, ClientContext, UnitOfWork, TimeProvider);

    public PatientEmailVerificationService CreateEmailVerificationService() => new(
        CurrentUser, PatientAccounts, CreateVerificationCodeService(), UnitOfWork,
        new SendEmailVerificationCodeRequestValidator(), new VerifyEmailRequestValidator());

    public VerificationCodeService CreateVerificationCodeService() =>
        new(VerificationCodes, CodeGenerator, Messages, Messages, UnitOfWork, TimeProvider);

    public RefreshTokenService CreateRefreshTokenService() => new(
        RefreshTokens, RefreshTokenFactory, StaffAccounts, PatientAccounts, CreateTokenPairIssuer(),
        CurrentUser, UnitOfWork, new RefreshTokenRequestValidator(), TimeProvider);

    public PatientLoginService CreatePatientLoginService() =>
        new(PatientAccounts, CreateCredentialChecker(), CreateTokenPairIssuer(), UnitOfWork, new PatientLoginRequestValidator());

    public PatientRegistrationService CreatePatientRegistrationService() => new(
        PatientAccounts, PasswordHasher, CreateVerificationCodeService(), new PatientAccountLinker(Patients),
        CreateTokenPairIssuer(), UnitOfWork, new RegisterPatientAccountRequestValidator(TimeProvider),
        new VerifyPhoneRequestValidator(), new ResendVerificationCodeRequestValidator());

    public PermissionManagementService CreatePermissionManagementService() => new(
        Permissions, StaffAccounts, PermissionProvider, UnitOfWork,
        new ReplaceRolePermissionsRequestValidator(), new ReplacePermissionOverridesRequestValidator());

    public PatientAccount AddActivePatientAccount(string phoneNumber = "0900000001", string? email = null)
    {
        var account = new PatientAccount(phoneNumber, email, PasswordHasher.Hash(Password), PendingProfile());
        account.Activate(Guid.NewGuid());
        PatientAccounts.Add(account);
        return account;
    }

    public StaffAccount AddStaffAccount(StaffRole role, string email = "staff@example.com")
    {
        var profile = new StaffProfile("Le Thi Staff", "0910000000", Address: null);
        var account = new StaffAccount(email, role, profile, PasswordHasher.Hash(Password), mustChangePassword: false);
        StaffAccounts.Add(account);
        return account;
    }

    public static PendingPatientProfile PendingProfile() =>
        new("Nguyen Van A", new DateOnly(1990, 1, 15), new Address("1 Le Loi", "Phuong Ben Thanh", "TP Ho Chi Minh"));
}
