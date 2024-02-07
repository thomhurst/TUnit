namespace TUnit.Assertions;

public static class DateTimeExtensions
{
    private static readonly string? DateFormat = "yyyy/MM/dd hh:mm:ss.fff tt";
    private static readonly string? LongTimeFormatWithMilliseconds = "hh:mm:ss.fff tt";
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