using DentalAPI.Application.Common.Security;

namespace DentalAPI.Api.Networking;

/// <summary>Reads the client address set by the forwarded-headers middleware (only trusted proxies can change it).</summary>
public sealed class HttpContextClientContext(IHttpContextAccessor httpContextAccessor) : IClientContext
{
    public const string UnknownClient = "unknown";

    public string ClientAddress =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? UnknownClient;
}
