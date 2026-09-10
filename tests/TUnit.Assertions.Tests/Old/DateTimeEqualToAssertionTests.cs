namespace TUnit.Assertions.Tests.Old;

public class DateTimeEqualToAssertionTests
{
    private static readonly DateTime TestDateTime = new(2020, 12, 31, 23, 59, 59);
    [Test]
    public async Task EqualsTo_Success()
    {
        var value1 = TestDateTime + TimeSpan.FromSeconds(1.1);
        var value2 = TestDateTime + TimeSpan.FromSeconds(1.1);

        await TUnitAssert.That(value1).IsEqualTo(value2);
    }

    [Test]
    public async Task EqualsTo_Failure()
    {
        var value1 = TestDateTime + TimeSpan.FromSeconds(1.1);
        var value2 = TestDateTime + TimeSpan.FromSeconds(1.2);

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2));
    }

    [Test]
    public async Task EqualsTo__With_Tolerance_Success()
    {
        var value1 = TestDateTime + TimeSpan.FromSeconds(1.1);
        var value2 = TestDateTime + TimeSpan.FromSeconds(1.2);

        await TUnitAssert.That(value1).IsEqualTo(value2).Within(TimeSpan.FromSeconds(0.1));
    }

    [Test]
    public async Task EqualsTo__With_Tolerance_Failure()
    {
        var value1 = TestDateTime + TimeSpan.FromSeconds(1.1);
        var value2 = TestDateTime + TimeSpan.FromSeconds(1.3);

        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).Within(TimeSpan.FromSeconds(0.1)));
    }
}
