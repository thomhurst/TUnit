---
sidebar_position: 7.5
---

# DateTime and Time Assertions

TUnit provides comprehensive assertions for date and time types, including `DateTime`, `DateTimeOffset`, `DateOnly`, `TimeOnly`, and `TimeSpan`, with support for tolerance-based comparisons and specialized checks.

## DateTime Equality with Tolerance

DateTime comparisons often need tolerance to account for timing variations:

```csharp
[Test]
public async Task DateTime_With_Tolerance()
{
    var now = DateTime.Now;
    var almostNow = now.AddMilliseconds(50);

    // Without tolerance - might fail
    // await Assert.That(almostNow).IsEqualTo(now);

    // With tolerance - passes
    await Assert.That(almostNow).IsEqualTo(now, tolerance: TimeSpan.FromSeconds(1));
}
```

### Tolerance Examples

```csharp
[Test]
public async Task Various_Tolerance_Values()
{
    var baseTime = new DateTime(2024, 1, 15, 10, 30, 0);

    // Millisecond tolerance
    var time1 = baseTime.AddMilliseconds(100);
    await Assert.That(time1).IsEqualTo(baseTime, tolerance: TimeSpan.FromMilliseconds(500));

    // Second tolerance
    var time2 = baseTime.AddSeconds(5);
    await Assert.That(time2).IsEqualTo(baseTime, tolerance: TimeSpan.FromSeconds(10));

    // Minute tolerance
    var time3 = baseTime.AddMinutes(2);
    await Assert.That(time3).IsEqualTo(baseTime, tolerance: TimeSpan.FromMinutes(5));
}
```

## DateTime Comparison

Standard comparison operators work with DateTime:

```csharp
[Test]
public async Task DateTime_Comparison()
{
    var past = DateTime.Now.AddDays(-1);
    var now = DateTime.Now;
    var future = DateTime.Now.AddDays(1);

    await Assert.That(now).IsGreaterThan(past);
    await Assert.That(now).IsLessThan(future);
    await Assert.That(past).IsLessThan(future);
}
```

## DateTime-Specific Assertions

### IsToday / IsNotToday

```csharp
[Test]
public async Task DateTime_Is_Today()
{
    var today = DateTime.Now;
    await Assert.That(today).IsToday();

    var yesterday = DateTime.Now.AddDays(-1);
    await Assert.That(yesterday).IsNotToday();

    var tomorrow = DateTime.Now.AddDays(1);
    await Assert.That(tomorrow).IsNotToday();
}
```

### IsUtc / IsNotUtc

```csharp
[Test]
public async Task DateTime_Kind()
{
    var utc = DateTime.UtcNow;
    await Assert.That(utc).IsUtc();

    var local = DateTime.Now;
    await Assert.That(local).IsNotUtc();

    var unspecified = new DateTime(2024, 1, 15);
    await Assert.That(unspecified).IsNotUtc();
}
```

### IsLeapYear / IsNotLeapYear

```csharp
[Test]
public async Task Leap_Year_Check()
{
    var leapYear = new DateTime(2024, 1, 1);
    await Assert.That(leapYear).IsLeapYear();

    var nonLeapYear = new DateTime(2023, 1, 1);
    await Assert.That(nonLeapYear).IsNotLeapYear();
}
```

### IsInFuture / IsInPast

Compares against local time:

```csharp
[Test]
public async Task Future_and_Past()
{
    var future = DateTime.Now.AddHours(1);
    await Assert.That(future).IsInFuture();

    var past = DateTime.Now.AddHours(-1);
    await Assert.That(past).IsInPast();
}
```

### IsInFutureUtc / IsInPastUtc

Compares against UTC time:

```csharp
[Test]
public async Task Future_and_Past_UTC()
{
    var futureUtc = DateTime.UtcNow.AddHours(1);
    await Assert.That(futureUtc).IsInFutureUtc();

    var pastUtc = DateTime.UtcNow.AddHours(-1);
    await Assert.That(pastUtc).IsInPastUtc();
}
```

### IsOnWeekend / IsOnWeekday

```csharp
[Test]
public async Task Weekend_Check()
{
    var saturday = new DateTime(2024, 1, 6); // Saturday
    await Assert.That(saturday).IsOnWeekend();

    var monday = new DateTime(2024, 1, 8); // Monday
    await Assert.That(monday).IsOnWeekday();
    await Assert.That(monday).IsNotOnWeekend();
}
```

### IsDaylightSavingTime / IsNotDaylightSavingTime

```csharp
[Test]
public async Task Daylight_Saving_Time()
{
    var summer = new DateTime(2024, 7, 1); // Summer in Northern Hemisphere
    var winter = new DateTime(2024, 1, 1); // Winter

    // Results depend on timezone
    if (TimeZoneInfo.Local.IsDaylightSavingTime(summer))
    {
        await Assert.That(summer).IsDaylightSavingTime();
    }
}
```

## DateTimeOffset

DateTimeOffset includes timezone information:

```csharp
[Test]
public async Task DateTimeOffset_With_Tolerance()
{
    var now = DateTimeOffset.Now;
    var almostNow = now.AddSeconds(1);

    await Assert.That(almostNow).IsEqualTo(now, tolerance: TimeSpan.FromSeconds(5));
}
```

```csharp
[Test]
public async Task DateTimeOffset_Comparison()
{
    var earlier = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(-8));
    var later = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(0));

    // Same local time, but different UTC times
    await Assert.That(later).IsGreaterThan(earlier);
}
```

## DateOnly (.NET 6+)

DateOnly represents just a date without time:

```csharp
[Test]
public async Task DateOnly_Assertions()
{
    var date1 = new DateOnly(2024, 1, 15);
    var date2 = new DateOnly(2024, 1, 15);

    await Assert.That(date1).IsEqualTo(date2);
}
```

### DateOnly with Days Tolerance

```csharp
[Test]
public async Task DateOnly_With_Tolerance()
{
    var date1 = new DateOnly(2024, 1, 15);
    var date2 = new DateOnly(2024, 1, 17);

    await Assert.That(date2).IsEqualTo(date1, daysTolerance: 5);
}
```

### DateOnly Comparison

```csharp
[Test]
public async Task DateOnly_Comparison()
{
    var earlier = new DateOnly(2024, 1, 1);
    var later = new DateOnly(2024, 12, 31);

    await Assert.That(later).IsGreaterThan(earlier);
    await Assert.That(earlier).IsLessThan(later);
}
```

## TimeOnly (.NET 6+)

TimeOnly represents just time without a date:

```csharp
[Test]
public async Task TimeOnly_Assertions()
{
    var morning = new TimeOnly(9, 30, 0);
    var evening = new TimeOnly(17, 45, 0);

    await Assert.That(evening).IsGreaterThan(morning);
}
```

### TimeOnly with Tolerance

```csharp
[Test]
public async Task TimeOnly_With_Tolerance()
{
    var time1 = new TimeOnly(10, 30, 0);
    var time2 = new TimeOnly(10, 30, 5);

    await Assert.That(time2).IsEqualTo(time1, tolerance: TimeSpan.FromSeconds(10));
}
```

## TimeSpan

TimeSpan represents a duration:

```csharp
[Test]
public async Task TimeSpan_Assertions()
{
    var duration1 = TimeSpan.FromMinutes(30);
    var duration2 = TimeSpan.FromMinutes(30);

    await Assert.That(duration1).IsEqualTo(duration2);
}
```

### TimeSpan Comparison

```csharp
[Test]
public async Task TimeSpan_Comparison()
{
    var short_duration = TimeSpan.FromMinutes(5);
    var long_duration = TimeSpan.FromHours(1);

    await Assert.That(long_duration).IsGreaterThan(short_duration);
    await Assert.That(short_duration).IsLessThan(long_duration);
}
```

### TimeSpan Sign Checks

```csharp
[Test]
public async Task TimeSpan_Sign()
{
    var positive = TimeSpan.FromHours(1);
    await Assert.That(positive).IsPositive();

    var negative = TimeSpan.FromHours(-1);
    await Assert.That(negative).IsNegative();
}
```

## Practical Examples

### Expiration Checks

```csharp
[Test]
public async Task Check_Token_Expiration()
{
    var token = CreateToken();
    var expiresAt = token.ExpiresAt;

    await Assert.That(expiresAt).IsInFuture();

    // Or check if expired
    var expiredToken = CreateExpiredToken();
    await Assert.That(expiredToken.ExpiresAt).IsInPast();
}
```

### Age Calculation

```csharp
[Test]
public async Task Calculate_Age()
{
    var birthDate = new DateTime(1990, 1, 1);
    var age = DateTime.Now.Year - birthDate.Year;

    if (DateTime.Now.DayOfYear < birthDate.DayOfYear)
    {
        age--;
    }

    await Assert.That(age).IsGreaterThanOrEqualTo(0);
    await Assert.That(age).IsLessThan(150); // Reasonable max age
}
```

### Business Days

```csharp
[Test]
public async Task Is_Business_Day()
{
    var monday = new DateTime(2024, 1, 8);

    await Assert.That(monday).IsOnWeekday();
    await Assert.That(monday.DayOfWeek).IsNotEqualTo(DayOfWeek.Saturday);
    await Assert.That(monday.DayOfWeek).IsNotEqualTo(DayOfWeek.Sunday);
}
```

### Scheduling

```csharp
[Test]
public async Task Scheduled_Time()
{
    var scheduledTime = new DateTime(2024, 12, 25, 9, 0, 0);

    await Assert.That(scheduledTime.Month).IsEqualTo(12);
    await Assert.That(scheduledTime.Day).IsEqualTo(25);
    await Assert.That(scheduledTime.Hour).IsEqualTo(9);
}
```

### Performance Timing

```csharp
[Test]
public async Task Operation_Duration()
{
    var start = DateTime.Now;
    await PerformOperationAsync();
    var end = DateTime.Now;

    var duration = end - start;

    await Assert.That(duration).IsLessThan(TimeSpan.FromSeconds(5));
    await Assert.That(duration).IsPositive();
}
```

### Date Range Validation

```csharp
[Test]
public async Task Date_Within_Range()
{
    var startDate = new DateTime(2024, 1, 1);
    var endDate = new DateTime(2024, 12, 31);
    var checkDate = new DateTime(2024, 6, 15);

    await Assert.That(checkDate).IsGreaterThan(startDate);
    await Assert.That(checkDate).IsLessThan(endDate);
}
```

### Timestamp Validation

```csharp
[Test]
public async Task Record_Created_Recently()
{
    var record = await CreateRecordAsync();
    var createdAt = record.CreatedAt;
    var now = DateTime.UtcNow;

    // Created within last minute
    await Assert.That(createdAt).IsEqualTo(now, tolerance: TimeSpan.FromMinutes(1));
    await Assert.That(createdAt).IsInPastUtc();
}
```

### Time Zone Conversions

```csharp
[Test]
public async Task Time_Zone_Conversion()
{
    var utcTime = DateTime.UtcNow;
    var localTime = utcTime.ToLocalTime();

    await Assert.That(utcTime).IsUtc();
    await Assert.That(localTime).IsNotUtc();

    var offset = localTime - utcTime;
    await Assert.That(Math.Abs(offset.TotalHours)).IsLessThan(24);
}
```

## Working with Date Components

```csharp
[Test]
public async Task Date_Components()
{
    var date = new DateTime(2024, 7, 15, 14, 30, 45);

    await Assert.That(date.Year).IsEqualTo(2024);
    await Assert.That(date.Month).IsEqualTo(7);
    await Assert.That(date.Day).IsEqualTo(15);
    await Assert.That(date.Hour).IsEqualTo(14);
    await Assert.That(date.Minute).IsEqualTo(30);
    await Assert.That(date.Second).IsEqualTo(45);
}
```

## First and Last Day of Month

```csharp
[Test]
public async Task First_Day_Of_Month()
{
    var date = new DateTime(2024, 3, 15);
    var firstDay = new DateTime(date.Year, date.Month, 1);

    await Assert.That(firstDay.Day).IsEqualTo(1);
}

[Test]
public async Task Last_Day_Of_Month()
{
    var date = new DateTime(2024, 2, 15);
    var daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
    var lastDay = new DateTime(date.Year, date.Month, daysInMonth);

    await Assert.That(lastDay.Day).IsEqualTo(29); // 2024 is a leap year
}
```

## Quarter Calculation

```csharp
[Test]
public async Task Date_Quarter()
{
    var q1 = new DateTime(2024, 2, 1);
    var quarter1 = (q1.Month - 1) / 3 + 1;
    await Assert.That(quarter1).IsEqualTo(1);

    var q3 = new DateTime(2024, 8, 1);
    var quarter3 = (q3.Month - 1) / 3 + 1;
    await Assert.That(quarter3).IsEqualTo(3);
}
```

## DayOfWeek Assertions

DayOfWeek has its own assertions:

```csharp
[Test]
public async Task Day_Of_Week_Checks()
{
    var dayOfWeek = DateTime.Now.DayOfWeek;

    if (dayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
    {
        await Assert.That(dayOfWeek).IsWeekend();
    }
    else
    {
        await Assert.That(dayOfWeek).IsWeekday();
    }
}
```

## Chaining DateTime Assertions

```csharp
[Test]
public async Task Chained_DateTime_Assertions()
{
    var date = DateTime.Now;

    await Assert.That(date)
        .IsToday()
        .And.IsGreaterThan(DateTime.MinValue)
        .And.IsLessThan(DateTime.MaxValue);
}
```

## Common Patterns

### Birthday Validation

```csharp
[Test]
public async Task Validate_Birthday()
{
    var birthday = new DateTime(1990, 5, 15);

    await Assert.That(birthday).IsInPast();
    await Assert.That(birthday).IsGreaterThan(new DateTime(1900, 1, 1));
}
```

### Meeting Scheduler

```csharp
[Test]
public async Task Schedule_Meeting()
{
    var meetingTime = new DateTime(2024, 1, 15, 14, 0, 0);

    await Assert.That(meetingTime).IsInFuture();
    await Assert.That(meetingTime).IsOnWeekday();
    await Assert.That(meetingTime.Hour).IsBetween(9, 17); // Business hours
}
```

### Relative Time Checks

```csharp
[Test]
public async Task Within_Last_Hour()
{
    var timestamp = DateTime.Now.AddMinutes(-30);
    var hourAgo = DateTime.Now.AddHours(-1);

    await Assert.That(timestamp).IsGreaterThan(hourAgo);
}
```

## See Also

- [Equality & Comparison](equality-and-comparison.md) - General comparison with tolerance
- [Numeric Assertions](numeric.md) - Numeric components of dates
- [Specialized Types](specialized-types.md) - Other time-related types
