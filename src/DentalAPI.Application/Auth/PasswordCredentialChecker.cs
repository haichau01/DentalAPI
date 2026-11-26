using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.Auth;

/// <summary>
/// Checks a password against an account and applies both guessing limits: the per-client throttle and the
/// account-wide lockout. Shared by patient and staff sign-in.
/// </summary>
public sealed class PasswordCredentialChecker(
    IPasswordHasher passwordHasher,
    ILoginAttemptThrottle loginThrottle,
    IClientContext clientContext,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    /// <summary>Returns normally when the password is right and nothing blocks the attempt; resets both counters.</summary>
    /// <param name="identifier">The normalized phone or email the caller signed in with; part of the throttle key.</param>
    /// <exception cref="AuthenticationFailedException">Unknown account, blocked client, locked account or wrong password.</exception>
    public async Task EnsureValidAsync(string identifier, IPasswordCredential? account, string password, CancellationToken cancellationToken)
    {
        var clientAddress = clientContext.ClientAddress;
        var now = timeProvider.GetUtcNow();

        // Every rejected path still spends a hash, so response time does not reveal which check failed.
        if (account is null || loginThrottle.IsBlocked(identifier, clientAddress) || account.Lockout.IsLocked(now))
        {
            passwordHasher.VerifyAgainstDummy(password);
            RecordFailureIfUnknown(account, identifier, clientAddress);
            throw new AuthenticationFailedException();
        }

        if (!passwordHasher.Verify(account.PasswordHash, password))
        {
            loginThrottle.RecordFailure(identifier, clientAddress);
            account.Lockout.RecordFailure(now);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new AuthenticationFailedException();
        }

        loginThrottle.Reset(identifier, clientAddress);
        account.Lockout.Reset();
    }

    // Guessing identifiers that do not exist counts too, so the throttle also limits probing for registered phones.
    private void RecordFailureIfUnknown(IPasswordCredential? account, string identifier, string clientAddress)
    {
        if (account is null)
        {
            loginThrottle.RecordFailure(identifier, clientAddress);
        }
    }
}
