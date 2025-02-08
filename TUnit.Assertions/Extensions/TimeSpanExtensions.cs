namespace TUnit.Assertions.Extensions;

public static class TimeSpanExtensions
{
    public static TimeSpan Milliseconds(this int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
    public static TimeSpan Seconds(this int seconds) => TimeSpan.FromSeconds(seconds);
    public static TimeSpan Minutes(this int minutes) => TimeSpan.FromMinutes(minutes);
    public static TimeSpan Hours(this int hours) => TimeSpan.FromHours(hours);
    public static TimeSpan Days(this int days) => TimeSpan.FromDays(days);
}