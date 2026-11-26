namespace DentalAPI.Application.Common.Notifications;

/// <summary>Sends text messages. Implementations must not log the phone number or message body.</summary>
public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken);
}

/// <summary>Sends emails. Implementations must not log the address or message body.</summary>
public interface IEmailSender
{
    Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken);
}
