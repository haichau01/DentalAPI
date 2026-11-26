namespace DentalAPI.Application.Common.Security;

/// <summary>Where the current request comes from, after trusted proxy headers are applied.</summary>
public interface IClientContext
{
    /// <summary>The client's IP address, or a fixed placeholder when it is unknown.</summary>
    string ClientAddress { get; }
}
