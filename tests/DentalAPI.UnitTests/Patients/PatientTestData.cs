using DentalAPI.Application.Patients;

namespace DentalAPI.UnitTests.Patients;

internal static class PatientTestData
{
    // 2026-06-15 10:00 in Vietnam (UTC+7).
    public static readonly DateTimeOffset UtcNow = new(2026, 6, 15, 3, 0, 0, TimeSpan.Zero);

    public static SavePatientRequest ValidRequest() =>
        new("Nguyen Van A", new DateOnly(1990, 1, 15), "0900000001", "a@example.com", null);
}
