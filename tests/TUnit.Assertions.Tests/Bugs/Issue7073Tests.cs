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
}
