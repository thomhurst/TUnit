using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7596;

public enum RowColor
{
    Red = 1,
    Green = 2,
}

// Reports TypeCode.Object, so Enum.ToObject rejects it, but Convert.ChangeType binds it through ToType.
public sealed class ColorConvertible : IConvertible
{
    public object ToType(Type conversionType, IFormatProvider? provider) => RowColor.Green;
    public TypeCode GetTypeCode() => TypeCode.Object;
    public bool ToBoolean(IFormatProvider? provider) => throw new InvalidCastException();
    public byte ToByte(IFormatProvider? provider) => throw new InvalidCastException();
    public char ToChar(IFormatProvider? provider) => throw new InvalidCastException();
    public DateTime ToDateTime(IFormatProvider? provider) => throw new InvalidCastException();
    public decimal ToDecimal(IFormatProvider? provider) => throw new InvalidCastException();
    public double ToDouble(IFormatProvider? provider) => throw new InvalidCastException();
    public short ToInt16(IFormatProvider? provider) => throw new InvalidCastException();
    public int ToInt32(IFormatProvider? provider) => throw new InvalidCastException();
    public long ToInt64(IFormatProvider? provider) => throw new InvalidCastException();
    public sbyte ToSByte(IFormatProvider? provider) => throw new InvalidCastException();
    public float ToSingle(IFormatProvider? provider) => throw new InvalidCastException();
    public string ToString(IFormatProvider? provider) => nameof(ColorConvertible);
    public ushort ToUInt16(IFormatProvider? provider) => throw new InvalidCastException();
    public uint ToUInt32(IFormatProvider? provider) => throw new InvalidCastException();
    public ulong ToUInt64(IFormatProvider? provider) => throw new InvalidCastException();
}

// A custom IConvertible row stays on an enum instantiation alongside a row of the enum itself.
[EngineTest(ExpectedResult.Pass)]
[GenerateGenericTest(typeof(RowColor), typeof(string))]
[Arguments(RowColor.Red, "x")]
[MethodDataSource(nameof(Rows))]
public class CustomConvertibleEnumRows<T, U>(T first, U second = default!)
{
    public static IEnumerable<object?[]> Rows() => [[RowColor.Red], [new ColorConvertible()]];

    [Test]
    public async Task Test()
    {
        await Assert.That(first).IsTypeOf<RowColor>();
    }
}
