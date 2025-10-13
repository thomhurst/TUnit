#if NET6_0_OR_GREATER
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class TimeOnlyAssertionTests
{
    [Test]
    public async Task Test_TimeOnly_IsMidnight()
    {
        var midnight = TimeOnly.MinValue;
        await Assert.That(midnight).IsMidnight();
    }

    [Test]
    public async Task Test_TimeOnly_IsNotMidnight()
    {
        var notMidnight = new TimeOnly(1, 0);
        await Assert.That(notMidnight).IsNotMidnight();
    }

    [Test]
    public async Task Test_TimeOnly_IsNoon()
    {
        var noon = new TimeOnly(12, 0, 0, 0);
        await Assert.That(noon).IsNoon();
    }

    [Test]
    public async Task Test_TimeOnly_IsAM()
    {
        var morning = new TimeOnly(9, 30);
        await Assert.That(morning).IsAM();
    }

    [Test]
    public async Task Test_TimeOnly_IsPM()
    {
        var afternoon = new TimeOnly(14, 30);
        await Assert.That(afternoon).IsPM();
    }

    [Test]
    public async Task Test_TimeOnly_IsStartOfHour()
    {
        var startOfHour = new TimeOnly(10, 0, 0, 0);
        await Assert.That(startOfHour).IsStartOfHour();
    }

    [Test]
    public async Task Test_TimeOnly_IsEndOfHour()
    {
        var endOfHour = new TimeOnly(10, 59, 59, 999);
        await Assert.That(endOfHour).IsEndOfHour();
    }
}
#endif
