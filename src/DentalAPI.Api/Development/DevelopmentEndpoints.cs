using DentalAPI.Infrastructure.Notifications;

namespace DentalAPI.Api.Development;

public static class DevelopmentEndpoints
{
    /// <summary>
    /// Exposes the fake SMS/email outbox so verification codes can be read during manual testing.
    /// Mapped only in Development, which must never be reachable from outside the developer's machine.
    /// </summary>
    public static void MapDevelopmentOutbox(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/dev/outbox/{destination}", (string destination, InMemoryMessageOutbox outbox) =>
                outbox.GetLatest(destination) is { } message
                    ? Results.Ok(new { destination, message })
                    : Results.NotFound())
            .AllowAnonymous()
            .ExcludeFromDescription();
    }
}
