namespace DentalAPI.Application.Common.Time;

/// <summary>
/// Converts UTC time to the clinic's local time zone. Data is stored in UTC,
/// but dates such as "today" and working hours are defined in Vietnam time.
/// </summary>
public static class ClinicClock
{
    public static readonly TimeZoneInfo TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public static DateOnly Today(TimeProvider timeProvider)
    {
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), TimeZone);
        return DateOnly.FromDateTime(localNow.DateTime);
    }
}
