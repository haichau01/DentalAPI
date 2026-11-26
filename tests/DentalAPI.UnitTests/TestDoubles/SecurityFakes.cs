using DentalAPI.Application.Auth;
using DentalAPI.Application.Authorization;
using DentalAPI.Application.Common.Notifications;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;

namespace DentalAPI.UnitTests.TestDoubles;

/// <summary>Reversible "hash" so tests can create accounts with a known password.</summary>
public sealed class FakePasswordHasher : IPasswordHasher
{
    private const string Prefix = "hashed:";

    public int DummyVerificationCount { get; private set; }

    public string Hash(string password) => Prefix + password;

    public bool Verify(string passwordHash, string password) => passwordHash == Prefix + password;

    public void VerifyAgainstDummy(string password) => DummyVerificationCount++;
}

public sealed class FakeAccessTokenIssuer(TimeProvider timeProvider) : IAccessTokenIssuer
{
    public AccessTokenSubject? LastSubject { get; private set; }

    public AccessToken Issue(AccessTokenSubject subject)
    {
        LastSubject = subject;
        return new AccessToken($"access-{subject.AccountId}", timeProvider.GetUtcNow().AddMinutes(15));
    }
}

public sealed class FakeRefreshTokenFactory(TimeProvider timeProvider) : IRefreshTokenFactory
{
    private int _counter;

    public NewRefreshToken Create()
    {
        var plainText = $"refresh-{++_counter}";
        return new NewRefreshToken(plainText, Hash(plainText), timeProvider.GetUtcNow().AddDays(7));
    }

    public string Hash(string plainText) => "hash:" + plainText;
}

public sealed class FakeVerificationCodeGenerator : IVerificationCodeGenerator
{
    public const string Code = "123456";

    public string Generate() => Code;

    public string Hash(string code) => "hash:" + code;
}

public sealed class RecordingMessageSender : ISmsSender, IEmailSender
{
    public List<(string Destination, string Message)> Sent { get; } = [];

    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        Sent.Add((phoneNumber, message));
        return Task.CompletedTask;
    }

    public Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken)
    {
        Sent.Add((email, body));
        return Task.CompletedTask;
    }
}

public sealed class FakeCurrentUser : ICurrentUser
{
    public Guid? AccountId { get; set; }

    public AccountType? AccountType { get; set; }

    public Guid? PatientId { get; set; }
}

public sealed class FakeClientContext : IClientContext
{
    public string ClientAddress { get; set; } = "203.0.113.10";
}

/// <summary>Counts failures per key like the real throttle, without expiry.</summary>
public sealed class FakeLoginAttemptThrottle : ILoginAttemptThrottle
{
    private readonly Dictionary<string, int> _failures = [];

    public bool IsBlocked(string identifier, string clientAddress) =>
        _failures.GetValueOrDefault(Key(identifier, clientAddress)) >= ILoginAttemptThrottle.MaxFailuresPerClient;

    public void RecordFailure(string identifier, string clientAddress) =>
        _failures[Key(identifier, clientAddress)] = _failures.GetValueOrDefault(Key(identifier, clientAddress)) + 1;

    public void Reset(string identifier, string clientAddress) => _failures.Remove(Key(identifier, clientAddress));

    private static string Key(string identifier, string clientAddress) => $"{clientAddress}|{identifier}";
}

public sealed class CountingUnitOfWork : IUnitOfWork
{
    public int SaveChangesCallCount { get; private set; }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.CompletedTask;
    }
}

/// <summary>Uncached provider that records invalidations, so tests can check that changes clear the cache.</summary>
public sealed class RecordingPermissionProvider(EffectivePermissionLoader loader) : IEffectivePermissionProvider
{
    public List<Guid> InvalidatedAccounts { get; } = [];

    public int InvalidateAllCount { get; private set; }

    public Task<IReadOnlySet<Permission>> GetAsync(Guid staffAccountId, CancellationToken cancellationToken) =>
        loader.LoadAsync(staffAccountId, cancellationToken);

    public void InvalidateAccount(Guid staffAccountId) => InvalidatedAccounts.Add(staffAccountId);

    public void InvalidateAll() => InvalidateAllCount++;
}
