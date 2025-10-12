using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class DateTimeAssertionTests
{
    [Test]
    public async Task Test_DateTime_IsToday()
    {
        var value = DateTime.Today;
        await Assert.That(value).IsToday();
    }

    [Test]
    public async Task Test_DateTime_IsToday_Now()
    {
        var value = DateTime.Now;
        await Assert.That(value).IsToday();
    }

    [Test]
    public async Task Test_DateTime_IsNotToday()
    {
        var value = DateTime.Today.AddDays(-1);
        await Assert.That(value).IsNotToday();
    }

    [Test]
    public async Task Test_DateTime_IsNotToday_Tomorrow()
    {
        var value = DateTime.Today.AddDays(1);
        await Assert.That(value).IsNotToday();
    }

    [Test]
    public async Task Test_DateTime_IsUtc()
    {
        var value = DateTime.UtcNow;
        await Assert.That(value).IsUtc();
    }

    [Test]
    public async Task Test_DateTime_IsUtc_Specified()
    {
        var value = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await Assert.That(value).IsUtc();
    }

    [Test]
    public async Task Test_DateTime_IsNotUtc_Local()
    {
        var value = DateTime.Now;
        await Assert.That(value).IsNotUtc();
    }

    [Test]
    public async Task Test_DateTime_IsNotUtc_Unspecified()
    {
        var value = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        await Assert.That(value).IsNotUtc();
    }

    [Test]
    public async Task Test_DateTime_IsLeapYear()
    {
        var value = new DateTime(2024, 2, 29); // 2024 is a leap year
        await Assert.That(value).IsLeapYear();
    }

    [Test]
    public async Task Test_DateTime_IsLeapYear_2020()
    {
        var value = new DateTime(2020, 6, 15);
        await Assert.That(value).IsLeapYear();
    }

    [Test]
    public async Task Test_DateTime_IsNotLeapYear()
    {
        var value = new DateTime(2023, 3, 15); // 2023 is not a leap year
        await Assert.That(value).IsNotLeapYear();
    }

    [Test]
    public async Task Test_DateTime_IsNotLeapYear_1900()
    {
        var value = new DateTime(1900, 1, 1); // 1900 is not a leap year (century rule)
        await Assert.That(value).IsNotLeapYear();
    }

    [Test]
    public async Task Test_DateTime_IsInFuture()
    {
        var value = DateTime.Now.AddDays(1);
        await Assert.That(value).IsInFuture();
    }

    [Test]
    public async Task Test_DateTime_IsInFuture_FarFuture()
    {
        var value = DateTime.Now.AddYears(10);
        await Assert.That(value).IsInFuture();
    }

    [Test]
    public async Task Test_DateTime_IsInPast()
    {
        var value = DateTime.Now.AddDays(-1);
        await Assert.That(value).IsInPast();
    }

    [Test]
    public async Task Test_DateTime_IsInPast_FarPast()
    {
        var value = new DateTime(2000, 1, 1);
        await Assert.That(value).IsInPast();
    }

    [Test]
    public async Task Test_DateTime_IsInFutureUtc()
    {
        var value = DateTime.UtcNow.AddHours(1);
        await Assert.That(value).IsInFutureUtc();
    }

    [Test]
    public async Task Test_DateTime_IsInFutureUtc_Days()
    {
        var value = DateTime.UtcNow.AddDays(5);
        await Assert.That(value).IsInFutureUtc();
    }

    [Test]
    public async Task Test_DateTime_IsInPastUtc()
    {
        var value = DateTime.UtcNow.AddHours(-1);
        await Assert.That(value).IsInPastUtc();
    }

    [Test]
    public async Task Test_DateTime_IsInPastUtc_Days()
    {
        var value = DateTime.UtcNow.AddDays(-5);
        await Assert.That(value).IsInPastUtc();
    }

    [Test]
    public async Task Test_DateTime_IsOnWeekend_Saturday()
    {
        // Find next Saturday
        var today = DateTime.Today;
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        var saturday = daysUntilSaturday == 0 ? today : today.AddDays(daysUntilSaturday);
        await Assert.That(saturday).IsOnWeekend();
    }

    [Test]
    public async Task Test_DateTime_IsOnWeekend_Sunday()
    {
        // Find next Sunday
        var today = DateTime.Today;
        var daysUntilSunday = ((int)DayOfWeek.Sunday - (int)today.DayOfWeek + 7) % 7;
        var sunday = daysUntilSunday == 0 ? today : today.AddDays(daysUntilSunday);
        await Assert.That(sunday).IsOnWeekend();
    }

    [Test]
    public async Task Test_DateTime_IsOnWeekday_Monday()
    {
        // Find next Monday
        var today = DateTime.Today;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        var monday = daysUntilMonday == 0 ? today : today.AddDays(daysUntilMonday);
        await Assert.That(monday).IsOnWeekday();
    }

    [Test]
    public async Task Test_DateTime_IsOnWeekday_Friday()
    {
        // Find next Friday
        var today = DateTime.Today;
        var daysUntilFriday = ((int)DayOfWeek.Friday - (int)today.DayOfWeek + 7) % 7;
        var friday = daysUntilFriday == 0 ? today : today.AddDays(daysUntilFriday);
        await Assert.That(friday).IsOnWeekday();
    }

    [Test]
    public async Task Test_DateTime_IsDaylightSavingTime()
    {
        // Create a date that would be in DST in most northern hemisphere locations
        var summerDate = new DateTime(2024, 7, 15, 12, 0, 0, DateTimeKind.Local);

        // Only test if the system actually observes DST
        if (summerDate.IsDaylightSavingTime())
        {
            await Assert.That(summerDate).IsDaylightSavingTime();
        }
    }

    [Test]
    public async Task Test_DateTime_IsNotDaylightSavingTime()
    {
        // Create a date that would not be in DST in most northern hemisphere locations
        var winterDate = new DateTime(2024, 1, 15, 12, 0, 0, DateTimeKind.Local);

        // Only test if the system actually observes DST and this date is not in DST
        if (!winterDate.IsDaylightSavingTime())
        {
            await Assert.That(winterDate).IsNotDaylightSavingTime();
        }
    }
}
