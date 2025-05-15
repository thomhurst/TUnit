namespace TUnit.Assertions.Tests.Old;

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
    public async Task Long_EqualsTo_Failure()
    {
        var value1 = 1L;
        var value2 = 2L;
        
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2));
    }
    
#if NET
    [Test]
    public async Task Long_EqualsTo__With_Tolerance_Success()
    {
        var value1 = 1L;
        var value2 = 2L;
        
        await TUnitAssert.That(value1).IsEqualTo(value2).Within(1);
    }
    
    [Test]
    public async Task Long_EqualsTo__With_Tolerance_Failure()
    {
        var value1 = 1L;
        var value2 = 3;
        
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).Within(1));
    }
#endif
}