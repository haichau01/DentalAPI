using DentalAPI.Application.Patients;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DentalAPI.Application;

public static class DependencyInjection
{
    /// <summary>Registers use-case services and validators. Expects a <see cref="TimeProvider"/> to be registered by the host.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IPatientService, PatientService>();
        return services;
    }
}
