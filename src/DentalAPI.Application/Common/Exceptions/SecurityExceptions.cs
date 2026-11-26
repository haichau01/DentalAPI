namespace DentalAPI.Application.Common.Exceptions;

/// <summary>
/// Thrown when sign-in or token refresh fails. Mapped to HTTP 401. The message is the same for every cause
/// (unknown account, wrong password, locked, unverified) so callers cannot probe which accounts exist.
/// </summary>
public sealed class AuthenticationFailedException()
    : Exception("Thông tin đăng nhập không đúng hoặc tài khoản tạm thời bị khóa.");

/// <summary>Thrown when the caller is signed in but may not perform the action. Mapped to HTTP 403.</summary>
public sealed class ForbiddenException()
    : Exception("Bạn không có quyền thực hiện thao tác này.");

/// <summary>Thrown when a verification code is wrong, expired, used or tried too often. Mapped to HTTP 400.</summary>
public sealed class InvalidVerificationCodeException()
    : Exception("Mã xác minh không đúng hoặc đã hết hạn.");

/// <summary>Thrown when a new code is requested before the resend cooldown ends. Mapped to HTTP 429.</summary>
public sealed class VerificationCodeCooldownException()
    : Exception("Vui lòng đợi trước khi yêu cầu mã xác minh mới.");
