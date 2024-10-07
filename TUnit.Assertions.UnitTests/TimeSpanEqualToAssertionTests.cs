using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class TimeSpanEqualToAssertionTests
{
    [Test]
    public async Task EqualsTo_Success()
    {
        var value1 = TimeSpan.FromSeconds(1.1);
        var value2 = TimeSpan.FromSeconds(1.1);
        
        await TUnitAssert.That(value1).IsEqualTo(value2);
    }
    
    [Test]
    public void EqualsTo_Failure()
    {
        var value1 = TimeSpan.FromSeconds(1.1);
        var value2 = TimeSpan.FromSeconds(1.2);
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2));
    }
    
    [Test]
    public async Task EqualsTo__With_Tolerance_Success()
    {
        var value1 = TimeSpan.FromSeconds(1.1);
        var value2 = TimeSpan.FromSeconds(1.2);
        
        await TUnitAssert.That(value1).IsEqualTo(value2).Within(TimeSpan.FromSeconds(0.1));
    }
    
    [Test]
    public void EqualsTo__With_Tolerance_Failure()
    {
        var value1 = TimeSpan.FromSeconds(1.1);
        var value2 = TimeSpan.FromSeconds(1.3);
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).Within(TimeSpan.FromSeconds(0.1)));
    }
}