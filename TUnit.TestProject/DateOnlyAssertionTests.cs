#if NET6_0_OR_GREATER
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class DateOnlyAssertionTests
{
    [Test]
    public async Task Test_DateOnly_IsToday()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        await Assert.That(today).IsToday();
    }

    [Test]
    public async Task Test_DateOnly_IsNotToday()
    {
        var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        await Assert.That(yesterday).IsNotToday();
    }

    [Test]
    public async Task Test_DateOnly_IsLeapYear()
    {
        var leapYearDate = new DateOnly(2024, 2, 29);
        await Assert.That(leapYearDate).IsLeapYear();
    }

    [Test]
    public async Task Test_DateOnly_IsNotLeapYear()
    {
        var nonLeapYearDate = new DateOnly(2023, 3, 15);
        await Assert.That(nonLeapYearDate).IsNotLeapYear();
    }

    [Test]
    public async Task Test_DateOnly_IsOnWeekend()
    {
        // Find next Saturday
        var today = DateTime.Today;
        var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilSaturday == 0) daysUntilSaturday = 7; // If today is Saturday, get next Saturday
        var saturday = DateOnly.FromDateTime(today.AddDays(daysUntilSaturday));
        await Assert.That(saturday).IsOnWeekend();
    }

    [Test]
    public async Task Test_DateOnly_IsOnWeekday()
    {
        // Find next Monday
        var today = DateTime.Today;
        var daysUntilMonday = ((int)DayOfWeek.Monday - (int)today.DayOfWeek + 7) % 7;
        if (daysUntilMonday == 0) daysUntilMonday = 7; // If today is Monday, get next Monday
        var monday = DateOnly.FromDateTime(today.AddDays(daysUntilMonday));
        await Assert.That(monday).IsOnWeekday();
    }

    [Test]
    public async Task Test_DateOnly_IsInFuture()
    {
        var future = DateOnly.FromDateTime(DateTime.Today.AddDays(1));
        await Assert.That(future).IsInFuture();
    }

    [Test]
    public async Task Test_DateOnly_IsInPast()
    {
        var past = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
        await Assert.That(past).IsInPast();
    }

    [Test]
    public async Task Test_DateOnly_IsFirstDayOfMonth()
    {
        var firstDay = new DateOnly(2024, 1, 1);
        await Assert.That(firstDay).IsFirstDayOfMonth();
    }

    [Test]
    public async Task Test_DateOnly_IsLastDayOfMonth()
    {
        var lastDay = new DateOnly(2024, 2, 29); // Leap year
        await Assert.That(lastDay).IsLastDayOfMonth();
    }
}
#endif
