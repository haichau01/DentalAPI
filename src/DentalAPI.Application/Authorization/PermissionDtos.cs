using DentalAPI.Domain.Accounts;
using DentalAPI.Domain.Authorization;
using FluentValidation;

namespace DentalAPI.Application.Authorization;

/// <summary>A permission as shown to admins: its stable code and what it allows.</summary>
public sealed record PermissionDto(string Code, string Description);

public sealed record StaffRolePermissionsDto(StaffRole Role, IReadOnlyList<string> Permissions);

/// <summary>What a staff member may do, and which of it comes from individual overrides.</summary>
public sealed record StaffAccountPermissionsDto(
    Guid StaffAccountId,
    StaffRole Role,
    IReadOnlyList<string> EffectivePermissions,
    IReadOnlyList<string> Grants,
    IReadOnlyList<string> Revokes);

/// <summary>Replaces the whole permission set of a role.</summary>
public sealed record ReplaceRolePermissionsRequest(IReadOnlyList<string> Permissions);

/// <summary>Replaces all individual overrides of one staff member.</summary>
public sealed record ReplacePermissionOverridesRequest(IReadOnlyList<string> Grants, IReadOnlyList<string> Revokes);

public sealed class ReplaceRolePermissionsRequestValidator : AbstractValidator<ReplaceRolePermissionsRequest>
{
    public ReplaceRolePermissionsRequestValidator()
    {
        RuleFor(request => request.Permissions).NotNull();
        RuleForEach(request => request.Permissions).MustBeKnownPermissionCode();
    }
}

public sealed class ReplacePermissionOverridesRequestValidator : AbstractValidator<ReplacePermissionOverridesRequest>
{
    public ReplacePermissionOverridesRequestValidator()
    {
        RuleFor(request => request.Grants).NotNull();
        RuleFor(request => request.Revokes).NotNull();
        RuleForEach(request => request.Grants).MustBeKnownPermissionCode();
        RuleForEach(request => request.Revokes).MustBeKnownPermissionCode();

        RuleFor(request => request)
            .Must(request => !request.Grants.Intersect(request.Revokes, StringComparer.OrdinalIgnoreCase).Any())
            .WithName(nameof(ReplacePermissionOverridesRequest.Revokes))
            .WithMessage("Một quyền không thể vừa được cấp thêm vừa bị thu hồi.")
            .When(request => request.Grants is not null && request.Revokes is not null);
    }
}

internal static class PermissionCodeRules
{
    public static IRuleBuilderOptions<T, string> MustBeKnownPermissionCode<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(code => PermissionCodes.TryFromCode(code, out _))
            .WithMessage("Mã quyền '{PropertyValue}' không tồn tại.");
}
