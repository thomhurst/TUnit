using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class DateTimeOffsetAssertionTests
{
    [Test]
    public async Task Test_DateTimeOffset_IsToday()
    {
        var value = DateTimeOffset.Now;
        await Assert.That(value).IsToday();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsToday_StartOfDay()
    {
        var value = new DateTimeOffset(DateTimeOffset.Now.Date);
        await Assert.That(value).IsToday();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsNotToday_Yesterday()
    {
        var value = DateTimeOffset.Now.AddDays(-1);
        await Assert.That(value).IsNotToday();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsNotToday_Tomorrow()
    {
        var value = DateTimeOffset.Now.AddDays(1);
        await Assert.That(value).IsNotToday();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsUtc()
    {
        var value = DateTimeOffset.UtcNow;
        await Assert.That(value).IsUtc();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsUtc_ZeroOffset()
    {
        var value = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        await Assert.That(value).IsUtc();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsNotUtc_LocalTime()
    {
        var value = DateTimeOffset.Now;

        // Only test if local offset is not UTC
        if (value.Offset != TimeSpan.Zero)
        {
            await Assert.That(value).IsNotUtc();
        }
    }

    [Test]
    public async Task Test_DateTimeOffset_IsNotUtc_CustomOffset()
    {
        var value = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.FromHours(5));
        await Assert.That(value).IsNotUtc();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsLeapYear()
    {
        var value = new DateTimeOffset(2024, 2, 29, 0, 0, 0, TimeSpan.Zero);
        await Assert.That(value).IsLeapYear();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsLeapYear_2020()
    {
        var value = new DateTimeOffset(2020, 6, 15, 12, 0, 0, TimeSpan.Zero);
        await Assert.That(value).IsLeapYear();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsNotLeapYear()
    {
        var value = new DateTimeOffset(2023, 3, 15, 0, 0, 0, TimeSpan.Zero);
        await Assert.That(value).IsNotLeapYear();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsNotLeapYear_2100()
    {
        var value = new DateTimeOffset(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await Assert.That(value).IsNotLeapYear();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsInFuture()
    {
        var value = DateTimeOffset.Now.AddDays(1);
        await Assert.That(value).IsInFuture();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsInFuture_Hours()
    {
        var value = DateTimeOffset.Now.AddHours(2);
        await Assert.That(value).IsInFuture();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsInPast()
    {
        var value = DateTimeOffset.Now.AddDays(-1);
        await Assert.That(value).IsInPast();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsInPast_Years()
    {
        var value = new DateTimeOffset(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
        await Assert.That(value).IsInPast();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsInFutureUtc()
    {
        var value = DateTimeOffset.UtcNow.AddHours(1);
        await Assert.That(value).IsInFutureUtc();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsInFutureUtc_Days()
    {
        var value = DateTimeOffset.UtcNow.AddDays(3);
        await Assert.That(value).IsInFutureUtc();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsInPastUtc()
    {
        var value = DateTimeOffset.UtcNow.AddHours(-1);
        await Assert.That(value).IsInPastUtc();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsInPastUtc_Days()
    {
        var value = DateTimeOffset.UtcNow.AddDays(-3);
        await Assert.That(value).IsInPastUtc();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsOnWeekend_Saturday()
    {
        // Find next Saturday
        var today = DateTimeOffset.Now.Date;
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        var saturday = new DateTimeOffset(daysUntilSaturday == 0 ? today : today.AddDays(daysUntilSaturday));
        await Assert.That(saturday).IsOnWeekend();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsOnWeekend_Sunday()
    {
        // Find next Sunday
        var today = DateTimeOffset.Now.Date;
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7;
        var sunday = new DateTimeOffset(daysUntilSunday == 0 ? today : today.AddDays(daysUntilSunday));
        await Assert.That(sunday).IsOnWeekend();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsOnWeekday_Monday()
    {
        // Find next Monday
        var today = DateTimeOffset.Now.Date;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        var monday = new DateTimeOffset(daysUntilMonday == 0 ? today : today.AddDays(daysUntilMonday));
        await Assert.That(monday).IsOnWeekday();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsOnWeekday_Wednesday()
    {
        // Find next Wednesday
        var today = DateTimeOffset.Now.Date;
        var daysUntilWednesday = ((int)DayOfWeek.Wednesday - (int)today.DayOfWeek + 7) % 7;
        var wednesday = new DateTimeOffset(daysUntilWednesday == 0 ? today : today.AddDays(daysUntilWednesday));
        await Assert.That(wednesday).IsOnWeekday();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsAfter()
    {
        var before = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var after = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);
        await Assert.That(after).IsAfter(before);
    }

    [Test]
    public async Task Test_DateTimeOffset_IsAfter_SameTime_Fails()
    {
        var dateTimeOffset = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        await Assert.That(async () => await Assert.That(dateTimeOffset).IsAfter(dateTimeOffset))
            .ThrowsException();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsBefore()
    {
        var before = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var after = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);
        await Assert.That(before).IsBefore(after);
    }

    [Test]
    public async Task Test_DateTimeOffset_IsBefore_SameTime_Fails()
    {
        var dateTimeOffset = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        await Assert.That(async () => await Assert.That(dateTimeOffset).IsBefore(dateTimeOffset))
            .ThrowsException();
    }

    [Test]
    public async Task Test_DateTimeOffset_IsAfterOrEqualTo()
    {
        var before = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var after = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);
        await Assert.That(after).IsAfterOrEqualTo(before);
    }

    [Test]
    public async Task Test_DateTimeOffset_IsAfterOrEqualTo_SameTime()
    {
        var dateTimeOffset = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        await Assert.That(dateTimeOffset).IsAfterOrEqualTo(dateTimeOffset);
    }

    [Test]
    public async Task Test_DateTimeOffset_IsBeforeOrEqualTo()
    {
        var before = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        var after = new DateTimeOffset(2024, 1, 2, 12, 0, 0, TimeSpan.Zero);
        await Assert.That(before).IsBeforeOrEqualTo(after);
    }

    [Test]
    public async Task Test_DateTimeOffset_IsBeforeOrEqualTo_SameTime()
    {
        var dateTimeOffset = new DateTimeOffset(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);
        await Assert.That(dateTimeOffset).IsBeforeOrEqualTo(dateTimeOffset);
    }
}
