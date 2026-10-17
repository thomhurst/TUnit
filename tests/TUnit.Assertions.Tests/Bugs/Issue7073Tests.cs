using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #7073: <c>IsEqualTo</c> with an argument of a different
/// numeric type compiled but threw at runtime.
///
/// <list type="bullet">
/// <item><c>Assert.That(3).IsEqualTo(3.0)</c> binds the cross-type <c>IsEqualTo&lt;TValue, TOther&gt;</c>
/// on every language version, which only looked for user-defined <c>op_Implicit</c> methods and so
/// missed the built-in <c>int</c> to <c>double</c> conversion.</item>
/// <item><c>Assert.That(3m).IsEqualTo(3)</c> and <c>Assert.That((decimal?)3m).IsEqualTo(3m)</c> bind the
/// same-type overload on C# 13+ via <c>[OverloadResolutionPriority]</c>, but C# 12 ignores ORP and binds
/// the cross-type overload, which tried to convert the actual value to the expected type (an explicit
/// conversion) and threw.</item>
/// </list>
///
/// This project builds with the latest language version, so the C# 12 binding is reproduced by calling
/// the cross-type overload explicitly (<c>EqualsAssertionExtensions.IsEqualTo&lt;decimal, int&gt;</c>).
/// </summary>
public class Issue7073Tests
{
    [Test]
    public async Task Int_IsEqualTo_Double_Passes()
    {
        await Assert.That(3).IsEqualTo(3.0);
    }

    [Test]
    public async Task Int_IsEqualTo_Double_Fails_When_Different()
    {
        await Assert.That(async () => await Assert.That(3).IsEqualTo(3.5))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Int_IsNotEqualTo_Double_Passes_When_Different()
    {
        await Assert.That(3).IsNotEqualTo(3.5);
    }

    [Test]
    public async Task Int_IsEqualTo_NullableLong_Passes()
    {
        await Assert.That(3).IsEqualTo((long?)3);
    }

    [Test]
    public async Task Char_IsEqualTo_Int_Passes()
    {
        await Assert.That('A').IsEqualTo(65);
    }

    [Test]
    public async Task String_IsEqualTo_Object_Passes()
    {
        await Assert.That("value").IsEqualTo((object)"value");
    }

    [Test]
    public async Task Decimal_IsEqualTo_Int_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<decimal, int>(Assert.That(3m), 3);
    }

    [Test]
    public async Task Decimal_IsEqualTo_Int_CrossTypeOverload_Fails_Without_Truncating()
    {
        var exception = await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<decimal, int>(Assert.That(3.5m), 3))
            .Throws<AssertionException>();

        await Assert.That(exception!.Message).Contains("received 3.5");
    }

    [Test]
    public async Task Decimal_IsEqualTo_Int_CrossTypeOverload_Or_Chain_Compares_Exact_Value()
    {
        await EqualsAssertionExtensions.IsEqualTo<decimal, int>(Assert.That(4m), 3).Or.IsEqualTo(4);
    }

    [Test]
    public async Task Decimal_IsNotEqualTo_Int_CrossTypeOverload_Passes_When_Different()
    {
        await NotEqualsAssertionExtensions.IsNotEqualTo<decimal, int>(Assert.That(3.5m), 3);
    }

    [Test]
    public async Task Decimal_IsNotEqualTo_Int_CrossTypeOverload_Fails_When_Equal()
    {
        await Assert.That(async () =>
                await NotEqualsAssertionExtensions.IsNotEqualTo<decimal, int>(Assert.That(3m), 3))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task NullableDecimal_IsEqualTo_Decimal_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<decimal?, decimal>(Assert.That((decimal?)3m), 3m);
    }

    [Test]
    public async Task NullableDecimal_IsEqualTo_Decimal_CrossTypeOverload_Fails_When_Null()
    {
        var exception = await Assert.That(async () =>
                await EqualsAssertionExtensions.IsEqualTo<decimal?, decimal>(Assert.That((decimal?)null), 3m))
            .Throws<AssertionException>();

        await Assert.That(exception!.Message).Contains("received null");
    }

    [Test]
    public async Task NullableDecimal_IsNotEqualTo_Decimal_CrossTypeOverload_Passes_When_Null()
    {
        await NotEqualsAssertionExtensions.IsNotEqualTo<decimal?, decimal>(Assert.That((decimal?)null), 3m);
    }

    [Test]
    public async Task Object_IsEqualTo_String_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<object, string>(Assert.That((object)"value"), "value");
    }

    [Test]
    public async Task Object_IsEqualTo_String_CrossTypeOverload_Chains_The_Actual_Reference()
    {
        // Equal contents, distinct instances: the chained assertion must see the actual object, not the expected one.
        var actual = new string('x', 3);
        var expected = new string('x', 3);

        await EqualsAssertionExtensions.IsEqualTo<object, string>(Assert.That((object)actual), expected)
            .And.IsSameReferenceAs(actual);
    }

    [Test]
    public async Task Object_IsEqualTo_String_CrossTypeOverload_Returns_The_Actual_Reference()
    {
        var actual = new string('x', 3);
        var expected = new string('x', 3);

        var result = await EqualsAssertionExtensions.IsEqualTo<object, string>(Assert.That((object)actual), expected);

        await Assert.That(ReferenceEquals(result, actual)).IsTrue();
    }

    [Test]
    public async Task Looser_Equality_CrossTypeOverload_Passes_And_Chains_The_Actual_Value()
    {
        // CaseInsensitiveName equality ignores case, so the comparison as the source type passes even
        // though the string forms differ. The chained assertion sees the actual value's string, "ABC".
        await EqualsAssertionExtensions.IsEqualTo<CaseInsensitiveName, string>(Assert.That(new CaseInsensitiveName("ABC")), "abc")
            .And.IsEqualTo("ABC");
    }

    [Test]
    public async Task Looser_Equality_CrossTypeOverload_IsNotEqualTo_Fails_When_Equal_As_Source_Type()
    {
        await Assert.That(async () =>
                await NotEqualsAssertionExtensions.IsNotEqualTo<CaseInsensitiveName, string>(Assert.That(new CaseInsensitiveName("ABC")), "abc"))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Null_Expected_CrossTypeOverload_Invokes_User_Conversion()
    {
        // The same-type overload would run `implicit operator NullSentinelWrapper(string?)` on null,
        // which maps it to the None sentinel, so this passes on C# 13+.
        await EqualsAssertionExtensions.IsEqualTo<NullSentinelWrapper, string>(Assert.That(NullSentinelWrapper.None), (string?)null);
    }

    [Test]
    public async Task Null_Expected_CrossTypeOverload_IsNotEqualTo_Invokes_User_Conversion()
    {
        await Assert.That(async () =>
                await NotEqualsAssertionExtensions.IsNotEqualTo<NullSentinelWrapper, string>(Assert.That(NullSentinelWrapper.None), (string?)null))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task Null_Object_IsEqualTo_Null_String_CrossTypeOverload_Passes()
    {
        await EqualsAssertionExtensions.IsEqualTo<object, string>(Assert.That((object?)null), (string?)null);
    }

    public sealed class CaseInsensitiveName(string value) : IEquatable<CaseInsensitiveName>
    {
        public string Value { get; } = value;

        public static implicit operator CaseInsensitiveName(string value) => new(value);

        public static implicit operator string(CaseInsensitiveName name) => name.Value;

        public bool Equals(CaseInsensitiveName? other)
            => other is not null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object? obj) => Equals(obj as CaseInsensitiveName);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public override string ToString() => Value;
    }

    public sealed record NullSentinelWrapper(string Value)
    {
        public static readonly NullSentinelWrapper None = new("<none>");

        public static implicit operator NullSentinelWrapper(string? value) => value is null ? None : new(value);
    }
}
