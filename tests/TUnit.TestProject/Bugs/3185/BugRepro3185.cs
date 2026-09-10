using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3185;

[Flags]
public enum FlagMock
{
    One = 1,
    Two = 2,
    Three = 4
}

public enum RegularEnum
{
    None = 0,
    First = 1,
    Second = 2,
    Third = 3
}

public static class FlagsHelper
{
    public static FlagMock? GetFlags(FlagMock[] flags)
    {
        if (flags == null || flags.Length == 0)
            return null;

        FlagMock result = 0;
        foreach (var flag in flags)
        {
            result |= flag;
        }
        return result;
    }

    public static RegularEnum? ProcessEnum(RegularEnum? input)
    {
        return input;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class NullableEnumParameterTests
{
    [Test]
    [Arguments(new FlagMock[] { }, null)]
    [Arguments(new FlagMock[] { FlagMock.Two }, FlagMock.Two)]
    [Arguments(new FlagMock[] { FlagMock.One, FlagMock.Three }, FlagMock.One | FlagMock.Three)]
    public async Task Nullable_FlagsEnum_WithNull(FlagMock[] flags, FlagMock? expected)
    {
        await Assert.That(FlagsHelper.GetFlags(flags)).IsEqualTo(expected);
    }

    [Test]
    [Arguments(null)]
    [Arguments(RegularEnum.First)]
    [Arguments(RegularEnum.Second)]
    [Arguments(RegularEnum.Third)]
    public async Task Nullable_RegularEnum_SingleParam(RegularEnum? value)
    {
        await Assert.That(FlagsHelper.ProcessEnum(value)).IsEqualTo(value);
    }

    [Test]
    [Arguments(null, null)]
    [Arguments(RegularEnum.First, RegularEnum.First)]
    [Arguments(RegularEnum.Second, RegularEnum.Third)]
    public async Task Nullable_RegularEnum_MultipleParams(RegularEnum? input, RegularEnum? expected)
    {
        if (input == RegularEnum.Second)
        {
            await Assert.That(expected).IsEqualTo(RegularEnum.Third);
        }
        else
        {
            await Assert.That(input).IsEqualTo(expected);
        }
    }

    [Test]
    [Arguments(FlagMock.One, null)]
    [Arguments(FlagMock.Two, FlagMock.Two)]
    [Arguments(null, null)]
    public async Task Nullable_FlagsEnum_MixedParams(FlagMock? input, FlagMock? expected)
    {
        if (input == FlagMock.One)
        {
            await Assert.That((FlagMock?)null).IsEqualTo(expected);
        }
        else
        {
            await Assert.That(input).IsEqualTo(expected);
        }
    }

    [Test]
    [Arguments(1, RegularEnum.First, null)]
    [Arguments(2, null, FlagMock.Two)]
    [Arguments(3, RegularEnum.Third, FlagMock.One | FlagMock.Three)]
    public async Task Nullable_MixedEnumTypes(int id, RegularEnum? regular, FlagMock? flags)
    {
        await Assert.That(id).IsGreaterThan(0);

        if (id == 2)
        {
            await Assert.That(regular).IsNull();
            await Assert.That(flags).IsNotNull();
        }
    }
}
