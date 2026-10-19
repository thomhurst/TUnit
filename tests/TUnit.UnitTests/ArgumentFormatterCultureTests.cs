using System.Globalization;
using System.Numerics;
using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

/// <summary>
/// Regression tests for https://github.com/thomhurst/TUnit/issues/7126: display-name arguments must not
/// depend on the current culture.
/// </summary>
public class ArgumentFormatterCultureTests
{
    public static IEnumerable<Func<CultureInfo>> Cultures()
    {
        yield return () => CultureInfo.InvariantCulture;
        yield return () => CultureInfo.GetCultureInfo("en-US");
        // Pin the decimal comma and U+2212 MINUS SIGN: sv-SE uses both under ICU, but not under NLS.
        yield return () => WithDecimalCommaAndUnicodeMinus("sv-SE");
        yield return () => WithDecimalCommaAndUnicodeMinus("de-DE");
    }

    [Test]
    [MethodDataSource(nameof(Cultures))]
    public async Task FormatArguments_Formats_Numbers_Invariantly(CultureInfo culture)
    {
        object?[] args = [1.5, -2, 2.25f, 3.75m, -1234567.891, (short)-7, 9_000_000_000L, new BigInteger(-42)];

        var result = WithCulture(culture, () => ArgumentFormatter.FormatArguments(args));

        await Assert.That(result).IsEqualTo("1.5, -2, 2.25, 3.75, -1234567.891, -7, 9000000000, -42");
    }

    [Test]
    [MethodDataSource(nameof(Cultures))]
    public async Task Format_Formats_Numbers_Inside_Tuples_And_Collections_Invariantly(CultureInfo culture)
    {
        var (tuple, array) = WithCulture(culture, () => (
            ArgumentFormatter.Format((1.5, -2), []),
            ArgumentFormatter.Format(new[] { 0.5, -0.25 }, [])));

        await Assert.That(tuple).IsEqualTo("(1.5, -2)");
        await Assert.That(array).IsEqualTo("0.5, -0.25");
    }

    [Test]
    [MethodDataSource(nameof(Cultures))]
    public async Task Format_With_Parameter_Type_Formats_Numbers_Invariantly(CultureInfo culture)
    {
        var result = WithCulture(culture, () => ArgumentFormatter.Format(-1.5m, typeof(decimal), []));

        await Assert.That(result).IsEqualTo("-1.5");
    }

    [Test]
    [MethodDataSource(nameof(Cultures))]
    public async Task Format_Formats_Complex_Invariantly(CultureInfo culture)
    {
        var value = new Complex(1.5, -2);

        var result = WithCulture(culture, () => ArgumentFormatter.Format(value, []));

        // Complex's own layout differs between runtimes ("(1.5, -2)" vs "<1.5; -2>"), so compare against
        // its invariant text rather than a literal; a culture-dependent result would contain "1,5" or U+2212.
        await Assert.That(result).IsEqualTo(value.ToString(null, CultureInfo.InvariantCulture));
    }

#if NET
    [Test]
    [MethodDataSource(nameof(Cultures))]
    public async Task FormatArguments_Formats_Modern_Numeric_Types_Invariantly(CultureInfo culture)
    {
        object?[] args = [(Half)(-1.5), (Int128)(-3), (UInt128)4, (nint)(-5), new System.Runtime.InteropServices.NFloat(-2.5)];

        var result = WithCulture(culture, () => ArgumentFormatter.FormatArguments(args));

        await Assert.That(result).IsEqualTo("-1.5, -3, 4, -5, -2.5");
    }
#endif

#if NET8_0_OR_GREATER
    /// <summary>
    /// ArgumentFormatter.IsNumeric is an explicit list of types. This fails when a .NET version adds a
    /// numeric type (one implementing INumberBase&lt;T&gt;) that the list misses, so that the type's display
    /// text does not quietly fall back to the current culture.
    /// </summary>
    [Test]
    public async Task Every_Runtime_Numeric_Type_Is_Formatted_Invariantly()
    {
        var culture = WithDecimalCommaAndUnicodeMinus("sv-SE");
        var assemblies = new[] { typeof(int).Assembly, typeof(BigInteger).Assembly, typeof(System.Runtime.InteropServices.NFloat).Assembly }.Distinct();
        var createValue = typeof(ArgumentFormatterCultureTests).GetMethod(nameof(MinusOneAndAHalf), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;

        var checkedTypes = new List<Type>();
        var cultureDependent = new List<string>();
        foreach (var type in assemblies.SelectMany(a => a.GetExportedTypes()))
        {
            if (!type.IsValueType || type.IsGenericTypeDefinition
                || !type.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INumberBase<>) && i.GetGenericArguments()[0] == type))
            {
                continue;
            }

            object value;
            try
            {
                value = createValue.MakeGenericMethod(type).Invoke(null, null)!;
            }
            catch (System.Reflection.TargetInvocationException)
            {
                // The type can't be created from a double (for example char); its text has no culture-specific parts
                continue;
            }

            checkedTypes.Add(type);
            var expected = ((IFormattable)value).ToString(null, CultureInfo.InvariantCulture);
            var actual = WithCulture(culture, () => ArgumentFormatter.Format(value, []));
            if (actual != expected)
            {
                cultureDependent.Add($"{type.FullName}: '{actual}', expected '{expected}'");
            }
        }

        await Assert.That(checkedTypes).Contains(typeof(double));
        await Assert.That(checkedTypes).Contains(typeof(BigInteger));
        await Assert.That(cultureDependent).IsEmpty();
    }

    private static object MinusOneAndAHalf<T>() where T : INumberBase<T> => T.CreateTruncating(-1.5);
#endif

    [Test]
    [MethodDataSource(nameof(Cultures))]
    public async Task Custom_Formatters_Still_Take_Precedence(CultureInfo culture)
    {
        var result = WithCulture(culture, () =>
            ArgumentFormatter.Format(1.5, [o => o is double d ? d.ToString("F2", CultureInfo.CurrentCulture) : null]));

        await Assert.That(result).IsEqualTo(1.5.ToString("F2", culture));
    }

    [Test]
    [MethodDataSource(nameof(Cultures))]
    public async Task Non_Numeric_Primitives_And_Enums_Are_Unchanged(CultureInfo culture)
    {
        var result = WithCulture(culture, () =>
            ArgumentFormatter.FormatArguments([true, 'x', DayOfWeek.Monday, "a.b"]));

        await Assert.That(result).IsEqualTo("True, x, Monday, a·b");
    }

    private static CultureInfo WithDecimalCommaAndUnicodeMinus(string name)
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo(name).Clone();
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = " ";
        culture.NumberFormat.NegativeSign = "−";
        return culture;
    }

    private static T WithCulture<T>(CultureInfo culture, Func<T> action)
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        try
        {
            return action();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }
}
