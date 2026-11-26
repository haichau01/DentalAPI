using System.Collections.Concurrent;
using DentalAPI.Application.Common.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DentalAPI.Infrastructure.Notifications;

/// <summary>
/// Keeps the latest message per destination in memory instead of sending it, so developers and tests can read
/// verification codes. Messages are never logged because they contain codes and phone numbers.
/// </summary>
public sealed class InMemoryMessageOutbox
{
    private readonly ConcurrentDictionary<string, string> _latestMessages = new(StringComparer.OrdinalIgnoreCase);

    public void Store(string destination, string message) => _latestMessages[destination] = message;

    public string? GetLatest(string destination) => _latestMessages.GetValueOrDefault(destination);
}

public sealed class DevelopmentSmsSender(InMemoryMessageOutbox outbox, ILogger<DevelopmentSmsSender> logger) : ISmsSender
{
    public Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken)
    {
        outbox.Store(phoneNumber, message);
        logger.LogInformation("Development SMS stored in the in-memory outbox.");
        return Task.CompletedTask;
    }
}

public sealed class DevelopmentEmailSender(InMemoryMessageOutbox outbox, ILogger<DevelopmentEmailSender> logger) : IEmailSender
{
    public Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken)
    {
        outbox.Store(email, body);
        logger.LogInformation("Development email stored in the in-memory outbox.");
        return Task.CompletedTask;
    }
}

public static class DevelopmentNotificationsRegistration
{
    /// <summary>Registers fake senders. Development only: no real SMS or email provider is integrated yet.</summary>
    public static IServiceCollection AddDevelopmentNotifications(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryMessageOutbox>();
        services.AddScoped<ISmsSender, DevelopmentSmsSender>();
        services.AddScoped<IEmailSender, DevelopmentEmailSender>();
        return services;
    }
}
