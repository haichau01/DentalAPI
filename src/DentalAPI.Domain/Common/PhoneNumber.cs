namespace DentalAPI.Domain.Common;

/// <summary>
/// Canonical form of Vietnamese phone numbers. Patients type "+84901234567", "84901234567" or "0901234567"
/// for the same number, and accounts are matched to patient records by phone, so all of them are stored as "0901234567".
/// </summary>
public static class PhoneNumber
{
    private const string InternationalPrefix = "+84";
    private const string CountryCode = "84";
    private const string DomesticPrefix = "0";

    // Domestic numbers have 10 digits, so "84" + 9 digits is the country code form without "+".
    private const int CountryCodeFormLength = 11;

    public static string Normalize(string phoneNumber)
    {
        var trimmed = phoneNumber.Trim();

        if (trimmed.StartsWith(InternationalPrefix, StringComparison.Ordinal))
        {
            return DomesticPrefix + trimmed[InternationalPrefix.Length..];
        }

        if (trimmed.Length == CountryCodeFormLength && trimmed.StartsWith(CountryCode, StringComparison.Ordinal))
        {
            return DomesticPrefix + trimmed[CountryCode.Length..];
        }

        return trimmed;
    }
}
