using DentalAPI.Application.Auth;
using DentalAPI.Application.Authorization;
using DentalAPI.Application.PatientAccounts;
using DentalAPI.Application.Patients;
using DentalAPI.Application.StaffAccounts;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DentalAPI.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers use-case services and validators. Expects the host to register <see cref="TimeProvider"/>,
    /// <see cref="Common.Security.ICurrentUser"/> and the message senders.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        services.AddScoped<IPatientService, PatientService>();
        services.AddScoped<ICurrentPatientService, CurrentPatientService>();

        services.AddScoped<TokenPairIssuer>();
        services.AddScoped<PasswordCredentialChecker>();
        services.AddScoped<VerificationCodeService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        services.AddScoped<PatientAccountLinker>();
        services.AddScoped<IPatientRegistrationService, PatientRegistrationService>();
        services.AddScoped<IPatientLoginService, PatientLoginService>();
        services.AddScoped<IPatientEmailVerificationService, PatientEmailVerificationService>();
        services.AddScoped<ExpiredRegistrationCleaner>();

        services.AddScoped<IStaffLoginService, StaffLoginService>();
        services.AddScoped<IStaffAccountService, StaffAccountService>();

        services.AddScoped<EffectivePermissionLoader>();
        services.AddScoped<IPermissionManagementService, PermissionManagementService>();

        return services;
    }
}
