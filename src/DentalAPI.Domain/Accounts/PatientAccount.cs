using DentalAPI.Domain.Common;

namespace DentalAPI.Domain.Accounts;

/// <summary>
/// A patient's self-registered sign-in. It becomes usable only after the phone number is verified,
/// and then points at exactly one patient record. Phone and email are sensitive data and must never be logged.
/// </summary>
public sealed class PatientAccount : Entity, IPasswordCredential
{
    public const int PhoneNumberMaxLength = 20;
    public const int EmailMaxLength = 256;
    public const int PasswordHashMaxLength = 512;

    /// <summary>
    /// Unverified registrations hold personal details of someone who never proved they own the phone,
    /// so they are deleted after this time.
    /// </summary>
    public static readonly TimeSpan PendingRegistrationLifetime = TimeSpan.FromHours(24);

    // Required by EF Core to materialize entities from the database.
    private PatientAccount()
    {
    }

    public PatientAccount(string phoneNumber, string? email, string passwordHash, PendingPatientProfile pendingProfile)
    {
        PhoneNumber = Common.PhoneNumber.Normalize(phoneNumber);
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        PendingProfile = pendingProfile;
        Status = PatientAccountStatus.PendingVerification;
    }

    public string PhoneNumber { get; private set; } = string.Empty;

    /// <summary>Optional. Usable for sign-in only once <see cref="IsEmailVerified"/> is true.</summary>
    public string? Email { get; private set; }

    public bool IsEmailVerified { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public PatientAccountStatus Status { get; private set; }

    /// <summary>Set when the account is activated; null while verification is pending.</summary>
    public Guid? PatientId { get; private set; }

    /// <summary>Registration details; cleared on activation because the patient record then holds them.</summary>
    public PendingPatientProfile? PendingProfile { get; private set; }

    public LoginLockout Lockout { get; private set; } = new();

    public bool IsActive => Status == PatientAccountStatus.Active;

    public static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    /// <summary>Replaces the details of a pending registration when the same phone registers again.</summary>
    public void RestartRegistration(string? email, string passwordHash, PendingPatientProfile pendingProfile)
    {
        EnsurePending();
        Email = NormalizeEmail(email);
        PasswordHash = passwordHash;
        PendingProfile = pendingProfile;
    }

    public void Activate(Guid patientId)
    {
        EnsurePending();

        PatientId = patientId;
        Status = PatientAccountStatus.Active;
        PendingProfile = null;
    }

    /// <summary>Replaces the email; it stays unusable for sign-in until verified again.</summary>
    public void ChangeEmail(string email)
    {
        Email = NormalizeEmail(email);
        IsEmailVerified = false;
    }

    public void MarkEmailVerified()
    {
        IsEmailVerified = Email is not null;
    }

    private void EnsurePending()
    {
        if (IsActive)
        {
            throw new InvalidOperationException("The account is already active.");
        }
    }
}
