using System.Threading.RateLimiting;
using DentalAPI.Api.Networking;
using DentalAPI.Application.Common.Security;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace DentalAPI.Api.RateLimiting;

/// <summary>A fixed-window limit, bound from configuration.</summary>
public sealed class FixedWindowLimitOptions
{
    public int PermitLimit { get; set; } = 10;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}

public static class RateLimitingSetup
{
    /// <summary>Anonymous sign-in, registration and refresh endpoints, limited per client IP address.</summary>
    public const string PublicAuthPolicy = "PublicAuth";

    /// <summary>Signed-in endpoints that send verification codes, limited per account.</summary>
    public const string AccountVerificationPolicy = "AccountVerification";

    private const string PublicAuthSection = "RateLimiting:PublicAuth";
    private const string AccountVerificationSection = "RateLimiting:AccountVerification";

    /// <summary>
    /// Limits guessing passwords and verification codes, probing which phones and emails are registered,
    /// and flooding people with codes. Per-destination flooding is additionally limited by the resend cooldown.
    /// </summary>
    public static IServiceCollection AddDentalRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<FixedWindowLimitOptions>(PublicAuthPolicy).BindConfiguration(PublicAuthSection);
        services.AddOptions<FixedWindowLimitOptions>(AccountVerificationPolicy).BindConfiguration(AccountVerificationSection);

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(PublicAuthPolicy, httpContext =>
                FixedWindow(httpContext, PublicAuthPolicy, ClientAddress(httpContext)));
            limiter.AddPolicy(AccountVerificationPolicy, httpContext =>
                FixedWindow(httpContext, AccountVerificationPolicy, AccountKey(httpContext)));
        });

        return services;
    }

    private static RateLimitPartition<string> FixedWindow(HttpContext httpContext, string policy, string partitionKey)
    {
        var options = httpContext.RequestServices.GetRequiredService<IOptionsMonitor<FixedWindowLimitOptions>>().Get(policy);
        return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = options.PermitLimit,
            Window = options.Window,
            QueueLimit = 0,
        });
    }

    private static string ClientAddress(HttpContext httpContext) =>
        httpContext.Connection.RemoteIpAddress?.ToString() ?? HttpContextClientContext.UnknownClient;

    // These endpoints require sign-in, so the account is known; fall back to the address just in case.
    private static string AccountKey(HttpContext httpContext) =>
        httpContext.User.FindFirst(AccessTokenClaims.Subject)?.Value ?? ClientAddress(httpContext);
}
