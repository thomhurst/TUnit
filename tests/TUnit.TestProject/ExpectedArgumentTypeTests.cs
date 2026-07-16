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
        => await Assert.That(value).IsOfType(expectedType);

    [Test]
    [Arguments(ByteEnum.Default, typeof(ByteEnum), typeof(byte))]
    [Arguments(SByteEnum.Default, typeof(SByteEnum), typeof(sbyte))]
    [Arguments(Int16Enum.Default, typeof(Int16Enum), typeof(short))]
    [Arguments(UInt16Enum.Default, typeof(UInt16Enum), typeof(ushort))]
    [Arguments(Int32Enum.Default, typeof(Int32Enum), typeof(int))]
    [Arguments(UInt32Enum.Default, typeof(UInt32Enum), typeof(uint))]
    [Arguments(Int64Enum.Default, typeof(Int64Enum), typeof(long))]
    [Arguments(UInt64Enum.Default, typeof(UInt64Enum), typeof(ulong))]
    public async Task EnumTypes(object value, Type expectedValueType, Type expectedEnumUnderlyingType)
    {
        await Assert.That(value).IsOfType(expectedValueType);
        await Assert.That(Enum.IsDefined(expectedValueType, value)).IsTrue();
        await Assert.That(Enum.GetUnderlyingType(expectedValueType)).IsEqualTo(expectedEnumUnderlyingType);
    }
}

public enum ByteEnum : byte { Default = 0 }
public enum SByteEnum : sbyte { Default = 0 }
public enum Int16Enum : short { Default = 0 }
public enum UInt16Enum : ushort { Default = 0 }
public enum Int32Enum : int { Default = 0 }
public enum UInt32Enum : uint { Default = 0 }
public enum Int64Enum : long { Default = 0 }
public enum UInt64Enum : ulong { Default = 0 }
