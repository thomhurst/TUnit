using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class DateOnlyEqualToAssertionTests
{
    private static readonly DateOnly TestDate = new DateOnly(2020, 12, 31);
    
    [Test]
    public async Task EqualsTo_Success()
    {
        var value1 = TestDate.AddDays(1);
        var value2 = TestDate.AddDays(1);
        
        await TUnitAssert.That(value1).IsEqualTo(value2);
    }
    
    [Test]
    public void EqualsTo_Failure()
    {
        var value1 = TestDate.AddDays(1);
        var value2 = TestDate.AddDays(2);
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2));
    }
    
    [Test]
    public async Task EqualsTo__With_Tolerance_Success()
    {
        var value1 = TestDate.AddDays(1);
        var value2 = TestDate.AddDays(2);
        
        await TUnitAssert.That(value1).IsEqualTo(value2).WithinDays(1);
    }
    
    [Test]
    public void EqualsTo__With_Tolerance_Failure()
    {
        var value1 = TestDate.AddDays(1);
        var value2 = TestDate.AddDays(3);
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).WithinDays(1));
    }
}