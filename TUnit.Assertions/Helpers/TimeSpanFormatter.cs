namespace TUnit.Assertions.Helpers;

public static class TimeSpanFormatter
{
    public static string PrettyPrint(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 1)
        {
            return $"{timeSpan.TotalMilliseconds} milliseconds";
        }

        if (timeSpan.TotalMinutes < 1)
        {
            return $"{timeSpan.Seconds} seconds and {timeSpan.Milliseconds} milliseconds";
        }

        if (timeSpan.TotalHours < 1)
        {
            return $"{timeSpan.Minutes} minutes and {timeSpan.Seconds} seconds";
        }

        if (timeSpan.TotalDays < 1)
        {
            return $"{timeSpan.Hours} hours, {timeSpan.Minutes} minutes and {timeSpan.Seconds} seconds";
        }

        return $"{timeSpan.Days} days, {timeSpan.Hours} hours, {timeSpan.Minutes} minutes and {timeSpan.Seconds} seconds";
    }
}
