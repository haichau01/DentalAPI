using FluentValidation;

namespace DentalAPI.Application.Common.Validation;

/// <summary>Validation rules for people's contact details and passwords, shared by patient, account and staff requests.</summary>
public static class ContactRules
{
    public const int PasswordMinLength = 8;
    public const int PasswordMaxLength = 128;

    // Digits with an optional leading "+", e.g. 0901234567 or +84901234567.
    private const string PhoneNumberPattern = @"^\+?[0-9]{9,15}$";
    private const string LetterPattern = "[A-Za-z]";
    private const string DigitPattern = "[0-9]";

    public static IRuleBuilderOptions<T, string> ValidPhoneNumber<T>(this IRuleBuilder<T, string> rule, int maxLength) =>
        rule.NotEmpty().MaximumLength(maxLength).Matches(PhoneNumberPattern);

    public static IRuleBuilderOptions<T, string?> ValidEmail<T>(this IRuleBuilder<T, string?> rule, int maxLength) =>
        rule.EmailAddress().MaximumLength(maxLength);

    public static IRuleBuilderOptions<T, string> ValidPassword<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty()
            .MinimumLength(PasswordMinLength)
            .MaximumLength(PasswordMaxLength)
            .Matches(LetterPattern).WithMessage("Mật khẩu phải có ít nhất một chữ cái.")
            .Matches(DigitPattern).WithMessage("Mật khẩu phải có ít nhất một chữ số.");
}
