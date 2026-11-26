namespace DentalAPI.Domain.Common;

/// <summary>
/// A postal address in Vietnam's two-level administrative model (ward/commune and province/city,
/// with no district level since the 2025 reorganisation). A patient's address is sensitive data and must never be logged.
/// </summary>
public sealed class Address
{
    public const int StreetLineMaxLength = 200;
    public const int WardMaxLength = 100;
    public const int ProvinceMaxLength = 100;

    // Required by EF Core to materialize owned types from the database.
    private Address()
    {
    }

    public Address(string? streetLine, string? ward, string province)
    {
        StreetLine = NullIfBlank(streetLine);
        Ward = NullIfBlank(ward);
        Province = province.Trim();
    }

    /// <summary>House number and street.</summary>
    public string? StreetLine { get; private set; }

    /// <summary>Ward or commune.</summary>
    public string? Ward { get; private set; }

    /// <summary>Province or centrally-run city.</summary>
    public string Province { get; private set; } = string.Empty;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
