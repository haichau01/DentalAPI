using DentalAPI.Domain.Common;

namespace DentalAPI.Domain.Accounts;

/// <summary>The editable profile of a staff member.</summary>
public sealed record StaffProfile(string FullName, string PhoneNumber, Address? Address);

/// <summary>An admin, dentist or receptionist. Created by an admin, never through public registration.</summary>
public sealed class StaffAccount : Entity, IPasswordCredential
{
    public const int EmailMaxLength = 256;
    public const int FullNameMaxLength = 200;
    public const int PhoneNumberMaxLength = 20;
    public const int PasswordHashMaxLength = 512;

    // Required by EF Core to materialize entities from the database.
    private StaffAccount()
    {
    }

    /// <param name="mustChangePassword">
    /// True when someone else chose the password (an admin creating the account), so only the staff member knows the final one.
    /// </param>
    public StaffAccount(string email, StaffRole role, StaffProfile profile, string passwordHash, bool mustChangePassword)
    {
        Email = NormalizeEmail(email);
        Role = role;
        PasswordHash = passwordHash;
        MustChangePassword = mustChangePassword;
        UpdateProfile(profile);
    }

    public string Email { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public string PhoneNumber { get; private set; } = string.Empty;

    public Address? Address { get; private set; }

    public StaffRole Role { get; private set; }

    public string PasswordHash { get; private set; } = string.Empty;

    public bool MustChangePassword { get; private set; }

    public LoginLockout Lockout { get; private set; } = new();

    public bool IsAdmin => Role == StaffRole.Admin;

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public void UpdateProfile(StaffProfile profile)
    {
        FullName = profile.FullName.Trim();
        PhoneNumber = Common.PhoneNumber.Normalize(profile.PhoneNumber);
        Address = profile.Address;
    }

    public void ChangePassword(string passwordHash)
    {
        PasswordHash = passwordHash;
        MustChangePassword = false;
    }
}
