using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ExpectedArgumentTypeTests
{
    [Test]
    [Arguments(0d, typeof(double))]
    [Arguments(0f, typeof(float))]
    [Arguments((sbyte)0, typeof(sbyte))]
    [Arguments((byte)0, typeof(byte))]
    [Arguments((short)0, typeof(short))]
    [Arguments((ushort)0, typeof(ushort))]
    [Arguments(0, typeof(int))]
    [Arguments((uint)0, typeof(uint))]
    [Arguments(0u, typeof(uint))]
    [Arguments((long)0, typeof(long))]
    [Arguments(0L, typeof(long))]
    [Arguments((ulong)0, typeof(ulong))]
    [Arguments(0UL, typeof(ulong))]
    public async Task TypedArguments(object value, Type expectedType)
        => await Assert.That(value).IsTypeOf(expectedType);
}
