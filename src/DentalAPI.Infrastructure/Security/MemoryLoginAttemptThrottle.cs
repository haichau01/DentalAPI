using DentalAPI.Application.Auth;
using Microsoft.Extensions.Caching.Memory;

namespace DentalAPI.Infrastructure.Security;

/// <summary>
/// In-process failure counters that expire <see cref="ILoginAttemptThrottle.BlockDuration"/> after the first failure.
/// Like the permission cache, this must move to a distributed cache once the API runs as several instances.
/// </summary>
public sealed class MemoryLoginAttemptThrottle(IMemoryCache memoryCache) : ILoginAttemptThrottle
{
    public bool IsBlocked(string identifier, string clientAddress) =>
        memoryCache.TryGetValue<FailureCounter>(Key(identifier, clientAddress), out var counter)
        && counter!.Count >= ILoginAttemptThrottle.MaxFailuresPerClient;

    public void RecordFailure(string identifier, string clientAddress)
    {
        var counter = memoryCache.GetOrCreate(Key(identifier, clientAddress), entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = ILoginAttemptThrottle.BlockDuration;
            return new FailureCounter();
        })!;
        counter.Increment();
    }

    public void Reset(string identifier, string clientAddress) => memoryCache.Remove(Key(identifier, clientAddress));

    private static string Key(string identifier, string clientAddress) => $"login-throttle:{clientAddress}:{identifier}";

    private sealed class FailureCounter
    {
        private int _count;

        public int Count => Volatile.Read(ref _count);

        public void Increment() => Interlocked.Increment(ref _count);
    }
}
