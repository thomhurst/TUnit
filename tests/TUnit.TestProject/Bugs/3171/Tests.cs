using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3171;

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    [Test]
    [Arguments(null, 0d, 0d)]
    [Arguments(null, 12d, 12d)]
    [Arguments(false, 42d, 0d)]
    [Arguments(true, 42d, 1d)]
    [Arguments(0, 42d, 0d)]
    [Arguments(1, 42d, 1d)]
    [Arguments(42, 0d, 42d)]
    [Arguments(-1, 0d, -1d)]
    [Arguments(0L, 42d, 0d)]
    [Arguments(1L, 42d, 1d)]
    [Arguments(42L, 0d, 42d)]
    [Arguments(-1L, 0d, -1d)]
    [Arguments(0d, 42d, 0d)]
    [Arguments(1d, 42d, 1d)]
    [Arguments(42d, 0d, 42d)]
    [Arguments(-1d, 0d, -1d)]
    [Arguments(3.5d, 0d, 3.5d)]
    [Arguments(-3.14d, 0d, -3.14d)]
    [Arguments("123", 42d, 123d)]
    [Arguments("-1", 42d, -1d)]
    [Arguments("3.14", 42d, 3.14d)]
    [Arguments("-1.02", 42d, -1.02d)]
    public void DoubleConversion(object? input, double nullValue, double expected)
    { }

    [Test]
    [Arguments(null, Flags.Value1, Flags.Value1)]
    [Arguments(null, Flags.Value2, Flags.Value2)]
    [Arguments(null, Flags.Value1 | Flags.Value2, Flags.Value1 | Flags.Value2)]
    [Arguments(null, Flags.Max, Flags.Max)]
    [Arguments("0", Flags.Max, (Flags)0)]
    [Arguments("1", Flags.Max, Flags.Value1)]
    [Arguments("2", Flags.Max, Flags.Value2)]
    [Arguments("3", Flags.Max, (Flags)3)]
    [Arguments("4", Flags.Max, Flags.Value3)]
    [Arguments("Value1", Flags.Max, Flags.Value1)]
    [Arguments("Value2", Flags.Max, Flags.Value2)]
    [Arguments("value1", Flags.Max, Flags.Value1)]
    [Arguments("value2", Flags.Max, Flags.Value2)]
    [Arguments("Value1,Value2", Flags.Max, Flags.Value1 | Flags.Value2)]
    [Arguments("value1, value2", Flags.Max, Flags.Value1 | Flags.Value2)]
    [Arguments(Flags.Value1, Flags.Max, Flags.Value1)]
    [Arguments(Flags.Value2, Flags.Max, Flags.Value2)]
    [Arguments(Flags.Max, (Flags)0, Flags.Max)]
    public void FlagsEnumConversion(object? input, Flags nullValue, Flags expected)
    { }
}

[Flags]
public enum Flags : byte
{
    Value1 = 1 << 0,
    Value2 = 1 << 1,
    Value3 = 1 << 2,
    Max = 255,
}
