using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

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
    public void Integer_EqualsTo_Failure()
    {
        var value1 = 1;
        var value2 = 2;
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2));
    }
    
    [Test]
    public async Task Integer_EqualsTo__With_Tolerance_Success()
    {
        var value1 = 1;
        var value2 = 2;
        
        await TUnitAssert.That(value1).IsEqualTo(value2).Within(1);
    }
    
    [Test]
    public void Integer_EqualsTo__With_Tolerance_Failure()
    {
        var value1 = 1;
        var value2 = 3;
        
        NUnitAssert.ThrowsAsync<TUnitAssertionException>(async () => await TUnitAssert.That(value1).IsEqualTo(value2).Within(1));
    }
}