namespace TUnit.Assertions.Tests.Old;

public class IntegerEqualsToAssertionTests
{
    [Test]
    public async Task Integer_EqualsTo_Success()
    {
        var value1 = 1;
        var value2 = 1;
        
        await TUnitAssert.That(value1).IsEqualTo(value2);
    }
    
    [Test]
    public async Task Integer_EqualsTo_Failure()
    {
        var value1 = 1;
        var value2 = 2;
        
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2));
    }
    
#if NET
    [Test]
    public async Task Integer_EqualsTo__With_Tolerance_Success()
    {
        var value1 = 1;
        var value2 = 2;
        
        await TUnitAssert.That(value1).IsEqualTo(value2).Within(1);
    }
    
    [Test]
    public async Task Integer_EqualsTo__With_Tolerance_Failure()
    {
        var value1 = 1;
        var value2 = 3;
        
        await TUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).Within(1));
    }
#endif
}