namespace TUnit.Assertions.Extensions;

internal static class DateTimeExtensions
{
    private static readonly string? DateFormat = "yyyy/MM/dd";
    private static readonly string? LongTimeFormatWithMilliseconds = "hh:mm:ss.ffff tt";
    private static readonly string? LongDateTimeFormatWithMilliseconds = $"{DateFormat} {LongTimeFormatWithMilliseconds}";

    public static string ToLongStringWithMilliseconds(this DateTime dateTime)
    {
        return dateTime.ToString(LongDateTimeFormatWithMilliseconds);
    }
    
    public static string ToLongStringWithMilliseconds(this DateTimeOffset dateTimeOffset)
    {
        return dateTimeOffset.ToString(LongDateTimeFormatWithMilliseconds);
    }
    
    public static string ToLongStringWithMilliseconds(this TimeOnly timeOnly)
    {
        return timeOnly.ToString(LongTimeFormatWithMilliseconds);
    }
}