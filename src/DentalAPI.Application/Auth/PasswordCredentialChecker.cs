using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.Auth;

/// <summary>Checks a password against an account and applies the lockout rule; shared by patient and staff sign-in.</summary>
public sealed class PasswordCredentialChecker(
    IPasswordHasher passwordHasher,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Returns normally when the password is right and the account is not locked; resets the failure count.</summary>
    /// <exception cref="AuthenticationFailedException">Unknown account, locked account or wrong password.</exception>
    public async Task EnsureValidAsync(IPasswordCredential? account, string password, CancellationToken cancellationToken)
    {
        if (account is null)
        {
            passwordHasher.VerifyAgainstDummy(password);
            throw new AuthenticationFailedException();
        }

        var now = timeProvider.GetUtcNow();
        if (account.Lockout.IsLocked(now))
        {
            throw new AuthenticationFailedException();
        }

        if (!passwordHasher.Verify(account.PasswordHash, password))
        {
            account.Lockout.RecordFailure(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException();
        }

        account.Lockout.Reset();
    }
}
