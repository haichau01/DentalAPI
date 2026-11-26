using DentalAPI.Application.Common.Addresses;
using DentalAPI.Application.Common.Validation;
using DentalAPI.Domain.Accounts;
using FluentValidation;

namespace DentalAPI.Application.StaffAccounts;

/// <summary>Created by an admin with an initial password the staff member must replace at first sign-in.</summary>
public sealed record CreateStaffAccountRequest(
    string Email,
    string InitialPassword,
    StaffRole Role,
    string FullName,
    string PhoneNumber,
    AddressDto? Address = null)
{
    public StaffProfile ToProfile() => new(FullName, PhoneNumber, Address?.ToDomain());
}

public sealed record UpdateStaffProfileRequest(string FullName, string PhoneNumber, AddressDto? Address = null)
{
    public StaffProfile ToProfile() => new(FullName, PhoneNumber, Address?.ToDomain());
}

public sealed record StaffLoginRequest(string Email, string Password);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

public sealed class CreateStaffAccountRequestValidator : AbstractValidator<CreateStaffAccountRequest>
{
    public CreateStaffAccountRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().ValidEmail(StaffAccount.EmailMaxLength);
        RuleFor(request => request.InitialPassword).ValidPassword();

        // Admins come only from configuration, so creating one through the API is not allowed.
        RuleFor(request => request.Role)
            .Must(role => role is StaffRole.Dentist or StaffRole.Receptionist)
            .WithMessage("Chỉ được tạo tài khoản Dentist hoặc Receptionist.");

        RuleFor(request => request.FullName).NotEmpty().MaximumLength(StaffAccount.FullNameMaxLength);
        RuleFor(request => request.PhoneNumber).ValidPhoneNumber(StaffAccount.PhoneNumberMaxLength);
        RuleFor(request => request.Address!)
            .SetValidator(new AddressValidator())
            .When(request => request.Address is not null);
    }
}

public sealed class UpdateStaffProfileRequestValidator : AbstractValidator<UpdateStaffProfileRequest>
{
    public UpdateStaffProfileRequestValidator()
    {
        RuleFor(request => request.FullName).NotEmpty().MaximumLength(StaffAccount.FullNameMaxLength);
        RuleFor(request => request.PhoneNumber).ValidPhoneNumber(StaffAccount.PhoneNumberMaxLength);
        RuleFor(request => request.Address!)
            .SetValidator(new AddressValidator())
            .When(request => request.Address is not null);
    }
}

public sealed class StaffLoginRequestValidator : AbstractValidator<StaffLoginRequest>
{
    public StaffLoginRequestValidator()
    {
        RuleFor(request => request.Email).NotEmpty().MaximumLength(StaffAccount.EmailMaxLength);
        RuleFor(request => request.Password).NotEmpty().MaximumLength(ContactRules.PasswordMaxLength);
    }
}

public sealed class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(request => request.CurrentPassword).NotEmpty().MaximumLength(ContactRules.PasswordMaxLength);
        RuleFor(request => request.NewPassword)
            .ValidPassword()
            .NotEqual(request => request.CurrentPassword).WithMessage("Mật khẩu mới phải khác mật khẩu hiện tại.");
    }
}
