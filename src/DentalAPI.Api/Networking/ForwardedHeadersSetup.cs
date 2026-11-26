using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace DentalAPI.Api.Networking;

/// <summary>Settings from "ForwardedHeaders": the reverse proxies whose X-Forwarded-For header is trusted.</summary>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>IP addresses of trusted proxies. Empty means only loopback is trusted (the ASP.NET Core default).</summary>
    public string[] KnownProxies { get; set; } = [];
}

public static class ForwardedHeadersSetup
{
    /// <summary>
    /// Behind a reverse proxy every request arrives from the proxy's address, which would put all clients
    /// into one rate-limit and login-throttle bucket. Only listed proxies are trusted, because otherwise
    /// any client could forge X-Forwarded-For and escape the limits.
    /// </summary>
    public static IServiceCollection AddTrustedForwardedHeaders(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = configuration.GetSection(ForwardedHeadersSettings.SectionName).Get<ForwardedHeadersSettings>()
            ?? new ForwardedHeadersSettings();

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            foreach (var proxy in settings.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }
        });

        return services;
    }
}
