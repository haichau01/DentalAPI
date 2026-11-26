namespace DentalAPI.Application.Common.Exceptions;

public sealed class PhoneNumberAlreadyRegisteredException()
    : ConflictException("Số điện thoại này đã được đăng ký.");

public sealed class EmailAlreadyInUseException()
    : ConflictException("Email này đã được sử dụng.");

/// <summary>Admin accounts always keep every permission and cannot be deleted or restricted through the API.</summary>
public sealed class AdminAccountProtectedException()
    : ConflictException("Không thể xóa hoặc giới hạn quyền của tài khoản Admin.");
