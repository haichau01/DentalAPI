using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Notifications;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;

namespace DentalAPI.Application.Auth;

/// <summary>Sends and checks one-time codes that prove a person owns a phone number or email.</summary>
public sealed class VerificationCodeService(
    IVerificationCodeRepository verificationCodeRepository,
    IVerificationCodeGenerator codeGenerator,
    ISmsSender smsSender,
    IEmailSender emailSender,
    IUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    private const string EmailSubject = "Mã xác minh DentalAPI";

    /// <summary>
    /// Stores a new code, saves all pending changes, then sends the code. Saving first means a code
    /// is never sent for changes that failed to save. Call it as the last step of a use case.
    /// </summary>
    /// <returns>When the code expires.</returns>
    /// <exception cref="VerificationCodeCooldownException">A code was sent to this destination too recently.</exception>
    public async Task<DateTimeOffset> SendAsync(VerificationChannel channel, string destination, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var latest = await verificationCodeRepository.GetLatestAsync(channel, destination, cancellationToken);
        if (latest is not null && !latest.CanResend(now))
        {
            throw new VerificationCodeCooldownException();
        }

        var code = codeGenerator.Generate();
        var verificationCode = new VerificationCode(channel, destination, codeGenerator.Hash(code), now);
        verificationCodeRepository.Add(verificationCode);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        await DeliverAsync(channel, destination, code, cancellationToken);
        return verificationCode.ExpiresAt;
    }

    /// <summary>Consumes the latest code for the destination. On success nothing is saved; the caller saves with its own changes.</summary>
    /// <exception cref="InvalidVerificationCodeException">The code is wrong, expired, already used or tried too often.</exception>
    public async Task VerifyAsync(VerificationChannel channel, string destination, string code, CancellationToken cancellationToken)
    {
        var latest = await verificationCodeRepository.GetLatestAsync(channel, destination, cancellationToken);
        if (latest is null)
        {
            throw new InvalidVerificationCodeException();
        }

        if (!latest.TryConsume(codeGenerator.Hash(code), timeProvider.GetUtcNow()))
        {
            // Persist the failed attempt so the attempt cap holds across requests.
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidVerificationCodeException();
        }
    }

    private Task DeliverAsync(VerificationChannel channel, string destination, string code, CancellationToken cancellationToken)
    {
        var message = $"Mã xác minh DentalAPI của bạn là {code}. Mã hết hạn sau {VerificationCode.Lifetime.TotalMinutes:0} phút.";
        return channel == VerificationChannel.Sms
            ? smsSender.SendAsync(destination, message, cancellationToken)
            : emailSender.SendAsync(destination, EmailSubject, message, cancellationToken);
    }
}
