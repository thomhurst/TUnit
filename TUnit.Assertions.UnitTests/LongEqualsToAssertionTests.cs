using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class LongEqualsToAssertionTests
{
    [Test]
    public async Task Long_EqualsTo_Success()
    {
        var value1 = 1L;
        var value2 = 1L;
        
        await TUnitAssert.That(value1).IsEqualTo(value2);
    }
    
    [Test]
    public void Long_EqualsTo_Failure()
    {
        var value1 = 1L;
        var value2 = 2L;
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2));
    }
    
    [Test]
    public async Task Long_EqualsTo__With_Tolerance_Success()
    {
        var value1 = 1L;
        var value2 = 2L;
        
        await TUnitAssert.That(value1).IsEqualTo(value2).Within(1);
    }
    
    [Test]
    public void Long_EqualsTo__With_Tolerance_Failure()
    {
        var value1 = 1L;
        var value2 = 3;
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).Within(1));
    }
}