using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class DayOfWeekAssertionTests
{
    [Test]
    public async Task Test_DayOfWeek_IsWeekend_Saturday()
    {
        var value = DayOfWeek.Saturday;
        await Assert.That(value).IsWeekend();
    }

    [Test]
    public async Task Test_DayOfWeek_IsWeekend_Sunday()
    {
        var value = DayOfWeek.Sunday;
        await Assert.That(value).IsWeekend();
    }

    [Test]
    public async Task Test_DayOfWeek_IsWeekday_Monday()
    {
        var value = DayOfWeek.Monday;
        await Assert.That(value).IsWeekday();
    }

    [Test]
    public async Task Test_DayOfWeek_IsWeekday_Tuesday()
    {
        var value = DayOfWeek.Tuesday;
        await Assert.That(value).IsWeekday();
    }

    [Test]
    public async Task Test_DayOfWeek_IsWeekday_Wednesday()
    {
        var value = DayOfWeek.Wednesday;
        await Assert.That(value).IsWeekday();
    }

    [Test]
    public async Task Test_DayOfWeek_IsWeekday_Thursday()
    {
        var value = DayOfWeek.Thursday;
        await Assert.That(value).IsWeekday();
    }

    [Test]
    public async Task Test_DayOfWeek_IsWeekday_Friday()
    {
        var value = DayOfWeek.Friday;
        await Assert.That(value).IsWeekday();
    }

    [Test]
    public async Task Test_DayOfWeek_IsMonday()
    {
        var value = DayOfWeek.Monday;
        await Assert.That(value).IsMonday();
    }

    [Test]
    public async Task Test_DayOfWeek_IsFriday()
    {
        var value = DayOfWeek.Friday;
        await Assert.That(value).IsFriday();
    }
}
