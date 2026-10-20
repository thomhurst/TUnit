using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7122;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/7122: when a row omitted optional
/// parameters, the source-generated call left them out, so C# overload resolution could bind it to a
/// sibling overload without those parameters. Omitted optionals now receive their declared defaults,
/// as in reflection mode.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
public class OmittedOptionalOverloadTests
{
    [Test]
    [Arguments]
    [Arguments(5)]
    [Arguments(5, 1, 2)]
    public async Task Optional_Before_Params(int count = 42, params int[] values)
    {
        // Rows: () -> (42, []), (5) -> (5, []), (5, 1, 2) -> (5, [1, 2]).
        if (count == 42)
        {
            await Assert.That(values).IsEmpty();
        }
        else
        {
            await Assert.That(count).IsEqualTo(5);
            await Assert.That(values.Length is 0 or 2).IsTrue();
        }
    }

    internal Task Optional_Before_Params(params int[] values)
        => throw new InvalidOperationException("Bound to the wrong overload");

    [Test]
    [Arguments(1)]
    public async Task Required_And_Optional_Before_Params(int first, int count = 42, params int[] values)
    {
        await Assert.That(first).IsEqualTo(1);
        await Assert.That(count).IsEqualTo(42);
        await Assert.That(values).IsEmpty();
    }

    internal Task Required_And_Optional_Before_Params(int first, params int[] values)
        => throw new InvalidOperationException("Bound to the wrong overload");

    [Test]
    [Arguments]
    public async Task Optional_Before_Params_With_Token(int count = 42, CancellationToken token = default, params int[] values)
    {
        await Assert.That(count).IsEqualTo(42);
        await Assert.That(values).IsEmpty();
    }

    internal Task Optional_Before_Params_With_Token(CancellationToken token = default, params int[] values)
        => throw new InvalidOperationException("Bound to the wrong overload");

    [Test]
    [Arguments(1)]
    public async Task Trailing_Optional(int first, int second = 42)
    {
        await Assert.That(first).IsEqualTo(1);
        await Assert.That(second).IsEqualTo(42);
    }

    internal Task Trailing_Optional(int first)
        => throw new InvalidOperationException("Bound to the wrong overload");

    [Test]
    [Arguments]
    public async Task Declared_Defaults_Of_Every_Kind(
        DayOfWeek day = DayOfWeek.Friday,
        decimal money = 1.5m,
        double ratio = -0.25,
        float nan = float.NaN,
        double infinity = double.NegativeInfinity,
        long min = long.MinValue,
        ulong max = ulong.MaxValue,
        byte small = 7,
        char quote = '\'',
        string text = "a \"quoted\"\n\\ text",
        string? nothing = null,
        int? nullableValue = 3,
        int? nullableNull = null,
        TimeSpan structDefault = default,
        params int[] values)
    {
        await Assert.That(day).IsEqualTo(DayOfWeek.Friday);
        await Assert.That(money).IsEqualTo(1.5m);
        await Assert.That(ratio).IsEqualTo(-0.25);
        await Assert.That(float.IsNaN(nan)).IsTrue();
        await Assert.That(double.IsNegativeInfinity(infinity)).IsTrue();
        await Assert.That(min).IsEqualTo(long.MinValue);
        await Assert.That(max).IsEqualTo(ulong.MaxValue);
        await Assert.That(small).IsEqualTo((byte)7);
        await Assert.That(quote).IsEqualTo('\'');
        await Assert.That(text).IsEqualTo("a \"quoted\"\n\\ text");
        await Assert.That(nothing).IsNull();
        await Assert.That(nullableValue).IsEqualTo(3);
        await Assert.That(nullableNull).IsNull();
        await Assert.That(structDefault).IsEqualTo(TimeSpan.Zero);
        await Assert.That(values).IsEmpty();
    }

    internal Task Declared_Defaults_Of_Every_Kind(params int[] values)
        => throw new InvalidOperationException("Bound to the wrong overload");

    [Test]
    [Arguments]
    public async Task Object_String_Default([Optional, DefaultParameterValue("declared")] object value)
    {
        await Assert.That(value).IsEqualTo("declared");
    }

    internal Task Object_String_Default(string value)
        => throw new InvalidOperationException("Bound to the wrong overload");

    [Test]
    [Arguments]
    public async Task Object_Non_Finite_Defaults_Keep_Their_Type([Optional, DefaultParameterValue(float.NaN)] object floatNan, [Optional, DefaultParameterValue(float.NegativeInfinity)] object floatInfinity, [Optional, DefaultParameterValue(double.NaN)] object doubleNan)
    {
        await Assert.That(floatNan is float f && float.IsNaN(f)).IsTrue();
        await Assert.That(floatInfinity is float g && float.IsNegativeInfinity(g)).IsTrue();
        await Assert.That(doubleNan is double d && double.IsNaN(d)).IsTrue();
    }

    [Test]
    [Arguments]
    public async Task Object_Integral_Defaults_Keep_Their_Type([Optional, DefaultParameterValue((byte)7)] object b, [Optional, DefaultParameterValue((short)-3)] object s, [Optional, DefaultParameterValue(5L)] object l, [Optional, DefaultParameterValue(9UL)] object ul)
    {
        await Assert.That(b is byte bv && bv == 7).IsTrue();
        await Assert.That(s is short sv && sv == -3).IsTrue();
        await Assert.That(l is long lv && lv == 5).IsTrue();
        await Assert.That(ul is ulong ulv && ulv == 9).IsTrue();
    }

    [Test]
    [Arguments]
    public async Task Caller_Info_Receives_Declared_Default([CallerMemberName] string caller = "declared")
    {
        // Reflection mode invokes with Type.Missing, which supplies the declared default.
        await Assert.That(caller).IsEqualTo("declared");
    }
}
