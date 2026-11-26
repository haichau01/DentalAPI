using DentalAPI.Application.Auth;
using DentalAPI.Application.Authorization;
using DentalAPI.Application.Common.Exceptions;
using DentalAPI.Application.Common.Pagination;
using DentalAPI.Application.Common.Persistence;
using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;
using FluentValidation;

namespace DentalAPI.Application.StaffAccounts;

/// <summary>Admin use cases for managing dentist and receptionist accounts.</summary>
public interface IStaffAccountService
{
    Task<PagedResult<StaffAccountDto>> ListAsync(PageQuery query, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException">The account does not exist.</exception>
    Task<StaffAccountDto> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <exception cref="ValidationException">The request is invalid, including an Admin role.</exception>
    /// <exception cref="EmailAlreadyInUseException">Another staff account uses the email.</exception>
    Task<StaffAccountDto> CreateAsync(CreateStaffAccountRequest request, CancellationToken cancellationToken);

    /// <exception cref="NotFoundException">The account does not exist.</exception>
    Task<StaffAccountDto> UpdateProfileAsync(Guid id, UpdateStaffProfileRequest request, CancellationToken cancellationToken);

    /// <summary>Soft-deletes the account and signs it out everywhere.</summary>
    /// <exception cref="AdminAccountProtectedException">The account is an admin.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken);
}

public sealed class StaffAccountService(
    IStaffAccountRepository accountRepository,
    IPasswordHasher passwordHasher,
    IRefreshTokenService refreshTokenService,
    IEffectivePermissionProvider permissionProvider,
    IUnitOfWork unitOfWork,
    IValidator<CreateStaffAccountRequest> createValidator,
    IValidator<UpdateStaffProfileRequest> updateValidator,
    IValidator<PageQuery> pageQueryValidator) : IStaffAccountService
{
    public async Task<PagedResult<StaffAccountDto>> ListAsync(PageQuery query, CancellationToken cancellationToken)
    {
        await pageQueryValidator.ValidateAndThrowAsync(query, cancellationToken);

        var accounts = await accountRepository.ListAsync(query, cancellationToken);
        return accounts.Map(StaffAccountDto.FromEntity);
    }

    public async Task<StaffAccountDto> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await GetExistingAsync(id, cancellationToken);
        return StaffAccountDto.FromEntity(account);
    }

    public async Task<StaffAccountDto> CreateAsync(CreateStaffAccountRequest request, CancellationToken cancellationToken)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var email = StaffAccount.NormalizeEmail(request.Email);
        if (await accountRepository.EmailExistsAsync(email, cancellationToken))
        {
            throw new EmailAlreadyInUseException();
        }

        var account = new StaffAccount(
            email, request.Role, request.ToProfile(), passwordHasher.Hash(request.InitialPassword), mustChangePassword: true);
        accountRepository.Add(account);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return StaffAccountDto.FromEntity(account);
    }

    public async Task<StaffAccountDto> UpdateProfileAsync(Guid id, UpdateStaffProfileRequest request, CancellationToken cancellationToken)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var account = await GetExistingAsync(id, cancellationToken);
        account.UpdateProfile(request.ToProfile());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return StaffAccountDto.FromEntity(account);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var account = await GetExistingAsync(id, cancellationToken);
        if (account.IsAdmin)
        {
            throw new AdminAccountProtectedException();
        }

        accountRepository.Remove(account);
        await refreshTokenService.RevokeAllAsync(account.Id, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        permissionProvider.InvalidateAccount(account.Id);
    }

    private async Task<StaffAccount> GetExistingAsync(Guid id, CancellationToken cancellationToken) =>
        await accountRepository.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException(nameof(StaffAccount), id);
}
