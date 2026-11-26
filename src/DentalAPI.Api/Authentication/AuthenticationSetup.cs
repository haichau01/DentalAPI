using DentalAPI.Application.Common.Security;
using DentalAPI.Domain.Accounts;
using DentalAPI.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DentalAPI.Api.Authentication;

public static class AuthenticationSetup
{
    private static readonly TimeSpan _clockSkew = TimeSpan.FromSeconds(30);

    /// <summary>JWT bearer sign-in plus authorization where every endpoint requires a signed-in caller unless marked otherwise.</summary>
    public static IServiceCollection AddDentalAuthentication(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();

        // Configured from JwtOptions lazily, so tests and user-secrets can supply the signing key.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearerOptions, jwtOptions) => ConfigureBearer(bearerOptions, jwtOptions.Value));

        services.AddScoped<IAuthorizationHandler, PasswordChangeCompletedHandler>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

        services.AddAuthorizationBuilder()
            .SetFallbackPolicy(new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build())
            .AddPolicy(Policies.Admin, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(AccessTokenClaims.AccountType, nameof(AccountType.Staff))
                .RequireClaim(AccessTokenClaims.Role, nameof(StaffRole.Admin))
                .AddRequirements(new PasswordChangeCompletedRequirement()))
            .AddPolicy(Policies.PatientSelf, policy => policy
                .RequireAuthenticatedUser()
                .RequireClaim(AccessTokenClaims.AccountType, nameof(AccountType.Patient))
                .RequireClaim(AccessTokenClaims.PatientId));

        return services;
    }

    private static void ConfigureBearer(JwtBearerOptions bearerOptions, JwtOptions jwtOptions)
    {
        // Keep the short claim names ("sub", "role") instead of mapping them to long XML claim URIs.
        bearerOptions.MapInboundClaims = false;
        bearerOptions.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = jwtOptions.CreateSigningKey(),
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
            ClockSkew = _clockSkew,
            NameClaimType = AccessTokenClaims.Subject,
            RoleClaimType = AccessTokenClaims.Role,
        };
    }
}
