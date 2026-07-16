using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class TimeSpanAssertionTests
{
    [Test]
    public async Task Test_TimeSpan_IsZero()
    {
        var timeSpan = TimeSpan.Zero;
        await Assert.That(timeSpan).IsZero();
    }

    [Test]
    public async Task Test_TimeSpan_IsZero_FromConstructor()
    {
        var timeSpan = new TimeSpan(0);
        await Assert.That(timeSpan).IsZero();
    }

    [Test]
    public async Task Test_TimeSpan_IsNotZero()
    {
        var timeSpan = TimeSpan.FromSeconds(1);
        await Assert.That(timeSpan).IsNotZero();
    }

    [Test]
    public async Task Test_TimeSpan_IsNotZero_Negative()
    {
        var timeSpan = TimeSpan.FromSeconds(-1);
        await Assert.That(timeSpan).IsNotZero();
    }

    [Test]
    public async Task Test_TimeSpan_IsPositive()
    {
        var timeSpan = TimeSpan.FromMinutes(5);
        await Assert.That(timeSpan).IsPositive();
    }

    [Test]
    public async Task Test_TimeSpan_IsPositive_Small()
    {
        var timeSpan = TimeSpan.FromTicks(1);
        await Assert.That(timeSpan).IsPositive();
    }

    [Test]
    public async Task Test_TimeSpan_IsNegative()
    {
        var timeSpan = TimeSpan.FromHours(-2);
        await Assert.That(timeSpan).IsNegative();
    }

    [Test]
    public async Task Test_TimeSpan_IsNegative_Small()
    {
        var timeSpan = TimeSpan.FromTicks(-1);
        await Assert.That(timeSpan).IsNegative();
    }

    [Test]
    public async Task Test_TimeSpan_IsNonNegative_Zero()
    {
        var timeSpan = TimeSpan.Zero;
        await Assert.That(timeSpan).IsNonNegative();
    }

    [Test]
    public async Task Test_TimeSpan_IsNonNegative_Positive()
    {
        var timeSpan = TimeSpan.FromDays(1);
        await Assert.That(timeSpan).IsNonNegative();
    }

    [Test]
    public async Task Test_TimeSpan_IsNonPositive_Zero()
    {
        var timeSpan = TimeSpan.Zero;
        await Assert.That(timeSpan).IsNonPositive();
    }

    [Test]
    public async Task Test_TimeSpan_IsNonPositive_Negative()
    {
        var timeSpan = TimeSpan.FromMilliseconds(-100);
        await Assert.That(timeSpan).IsNonPositive();
    }
}
