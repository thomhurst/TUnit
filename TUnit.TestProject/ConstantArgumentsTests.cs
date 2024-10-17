using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class ConstantArgumentsTests
{
    public const string DummyString = "123";
    public const int DummyInt = 123;
    public const double DummyDouble = 1.23;
    public const float DummyFloat = 1.23f;
    public const long DummyLong = 123;
    public const uint DummyUInt = 123;
    public const ulong DummyULong = 123;
    
    [Test]
    [Arguments(DummyString)]
    public async Task String1(string dummy)
    {
        await Assert.That(dummy).IsEqualTo(DummyString);
    }
    
    [Test]
    [Arguments(DummyInt)]
    public async Task Int(int dummy)
    {
        await Assert.That(dummy).IsEqualTo(DummyInt);
    }
    
    [Test]
    [Arguments(DummyDouble)]
    public async Task Double(double dummy)
    {
        await Assert.That(dummy).IsEqualTo(DummyDouble);
    }
    
    [Test]
    [Arguments(DummyFloat)]
    public async Task Float(float dummy)
    {
        await Assert.That(dummy).IsEqualTo(DummyFloat);
    }
    
    [Test]
    [Arguments(DummyLong)]
    public async Task Long(long dummy)
    {
        await Assert.That(dummy).IsEqualTo(DummyLong);
    }
    
    [Test]
    [Arguments(DummyUInt)]
    public async Task UInt(uint dummy)
    {
        await Assert.That(dummy).IsEqualTo(DummyUInt);
    }
    
    [Test]
    [Arguments(DummyULong)]
    public async Task ULong(ulong dummy)
    {
        await Assert.That(dummy).IsEqualTo(DummyULong);
    }
}