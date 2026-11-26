using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace DentalAPI.Api.RateLimiting;

/// <summary>Settings from "RateLimiting:PublicAuth" for the anonymous sign-in, registration and refresh endpoints.</summary>
public sealed class PublicAuthRateLimitOptions
{
    public const string SectionName = "RateLimiting:PublicAuth";

    public int PermitLimit { get; set; } = 10;

    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}

public static class RateLimitingSetup
{
    /// <summary>Policy name for anonymous auth endpoints, limited per client IP address.</summary>
    public const string PublicAuthPolicy = "PublicAuth";

    private const string UnknownClient = "unknown";

    /// <summary>
    /// Limits guessing passwords and verification codes, and probing which phone numbers are registered.
    /// Per-phone flooding is additionally limited by the verification code resend cooldown.
    /// </summary>
    public static IServiceCollection AddDentalRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<PublicAuthRateLimitOptions>().BindConfiguration(PublicAuthRateLimitOptions.SectionName);

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy(PublicAuthPolicy, httpContext =>
            {
                var options = httpContext.RequestServices.GetRequiredService<IOptions<PublicAuthRateLimitOptions>>().Value;
                var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownClient;

                return RateLimitPartition.GetFixedWindowLimiter(clientAddress, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = options.Window,
                    QueueLimit = 0,
                });
            });
        });

        return services;
    }
}
