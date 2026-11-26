using DentalAPI.Domain.Patients;
using Microsoft.EntityFrameworkCore;

namespace DentalAPI.Infrastructure.Persistence.Seeding;

/// <summary>Adds fake sample data for local development. Never put real patient data here.</summary>
public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(DentalDbContext dbContext, CancellationToken cancellationToken)
    {
        if (await dbContext.Patients.IgnoreQueryFilters().AnyAsync(cancellationToken))
        {
            return;
        }

        dbContext.Patients.AddRange(
            new Patient(new PatientDetails("Nguyen Van A", new DateOnly(1990, 1, 15), "0900000001", "a@example.com", null)),
            new Patient(new PatientDetails("Tran Thi B", new DateOnly(1985, 6, 30), "0900000002", null, "Allergic to penicillin")),
            new Patient(new PatientDetails("Le Van C", new DateOnly(2010, 11, 2), "0900000003", null, null)));

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
