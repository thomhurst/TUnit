// The IsEqualTo<TValue, TOther> overload these tests exercise lives behind
// `#if NET9_0_OR_GREATER` in TUnit.Assertions (see issue #5765 / #5751) because it
// relies on [OverloadResolutionPriority] for disambiguation, which is only honored
// by C# 13+ and only present in System.Runtime.CompilerServices on .NET 9+. The
// assertions test project multi-targets net8.0/net9.0/net10.0, so this file must
// match the same gate to keep the net8.0 build green.
#if NET9_0_OR_GREATER
namespace TUnit.Assertions.Tests.Bugs;

/// <summary>
/// Regression tests for GitHub issue #5720:
/// Wrapper Value Objects with an implicit conversion to a primitive (e.g.
/// <c>ProductCode</c> with <c>implicit operator string</c>) should be comparable
/// against values of the underlying primitive without an explicit cast or
/// <c>.Value</c> access:
///
/// <code>
/// await Assert.That(updatedStockItem.ProductCode).IsEqualTo("Example");
/// </code>
///
/// Before 1.39.0 this worked for string wrappers because <c>Assert.That(string?)</c>
/// silently accepted the implicitly-converted value. The new generalized solution
/// adds <c>IsEqualTo&lt;TValue, TOther&gt;</c> overloads that detect a user-defined
/// implicit operator at runtime and compare the converted values.
/// </summary>
public class Issue5720Tests
{
    public sealed record ProductCode(string Value)
    {
        public static implicit operator string(ProductCode pc) => pc.Value;
    }

    public readonly record struct WrappedNumber(int Value)
    {
        public static implicit operator int(WrappedNumber w) => w.Value;
    }

    public sealed record StockItem(ProductCode ProductCode, WrappedNumber Quantity);

    /// <summary>
    /// Source type with no operators of its own — the implicit conversion to <see cref="TargetWithIncoming"/>
    /// is declared on the target. Exercises the <c>FindImplicitOperatorOnTarget</c> fallback.
    /// </summary>
    public sealed record SourceWithoutOperators(string Value);

    public sealed record TargetWithIncoming(string Value)
    {
        public static implicit operator TargetWithIncoming(SourceWithoutOperators s) => new(s.Value);
    }

    /// <summary>
    /// Type with no implicit conversion to or from <see cref="string"/>. Used to verify
    /// the diagnostic thrown when no implicit operator can be found.
    /// </summary>
    public sealed record UnrelatedType(string Value);

    [Test]
    public async Task IsEqualTo_StringWrapper_Against_Primitive_Passes()
    {
        var stock = new StockItem(new ProductCode("Example"), new WrappedNumber(5));

        await Assert.That(stock.ProductCode).IsEqualTo("Example");
    }

    [Test]
    public async Task IsEqualTo_StringWrapper_Against_Primitive_Fails_When_Different()
    {
        var stock = new StockItem(new ProductCode("Example"), new WrappedNumber(5));

        await Assert.That(async () => await Assert.That(stock.ProductCode).IsEqualTo("Other"))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsEqualTo_IntWrapper_Against_Primitive_Passes()
    {
        var stock = new StockItem(new ProductCode("Example"), new WrappedNumber(5));

        await Assert.That(stock.Quantity).IsEqualTo(5);
    }

    [Test]
    public async Task IsEqualTo_IntWrapper_Against_Primitive_Fails_When_Different()
    {
        var stock = new StockItem(new ProductCode("Example"), new WrappedNumber(5));

        await Assert.That(async () => await Assert.That(stock.Quantity).IsEqualTo(6))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsNotEqualTo_StringWrapper_Against_Primitive_Passes_When_Different()
    {
        var stock = new StockItem(new ProductCode("Example"), new WrappedNumber(5));

        await Assert.That(stock.ProductCode).IsNotEqualTo("Other");
    }

    [Test]
    public async Task IsNotEqualTo_IntWrapper_Against_Primitive_Passes_When_Different()
    {
        var stock = new StockItem(new ProductCode("Example"), new WrappedNumber(5));

        await Assert.That(stock.Quantity).IsNotEqualTo(6);
    }

    [Test]
    public async Task IsNotEqualTo_IntWrapper_Against_Primitive_Fails_When_Equal()
    {
        var stock = new StockItem(new ProductCode("Example"), new WrappedNumber(5));

        await Assert.That(async () => await Assert.That(stock.Quantity).IsNotEqualTo(5))
            .Throws<AssertionException>();
    }

    [Test]
    public async Task IsEqualTo_SameType_Wrapper_Still_Compares_By_Default_Equality()
    {
        // When both sides are the same wrapper type, the original same-type overload
        // is selected and the wrapper's record-based Equals is used (not the converter).
        var a = new ProductCode("X");
        var b = new ProductCode("X");

        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task IsEqualTo_NullWrapper_Against_Null_Primitive_Passes()
    {
        ProductCode? code = null;

        await Assert.That(code).IsEqualTo((string?)null);
    }

    [Test]
    public async Task IsNotEqualTo_NullWrapper_Against_Non_Null_Primitive_Passes()
    {
        ProductCode? code = null;

        await Assert.That(code).IsNotEqualTo("Example");
    }

    [Test]
    public async Task IsNotEqualTo_NonNull_Wrapper_Against_Null_Primitive_Passes()
    {
        var stock = new StockItem(new ProductCode("Example"), new WrappedNumber(5));

        await Assert.That(stock.ProductCode).IsNotEqualTo((string?)null);
    }

    [Test]
    public async Task IsEqualTo_OperatorDefinedOnTargetType_Passes()
    {
        // The implicit conversion lives on TargetWithIncoming, not on SourceWithoutOperators.
        // Verifies the FindImplicitOperatorOnTarget fallback in BuildConverter.
        var source = new SourceWithoutOperators("Example");

        await Assert.That(source).IsEqualTo(new TargetWithIncoming("Example"));
    }

    [Test]
    public async Task IsNotEqualTo_OperatorDefinedOnTargetType_Passes_When_Different()
    {
        var source = new SourceWithoutOperators("Example");

        await Assert.That(source).IsNotEqualTo(new TargetWithIncoming("Other"));
    }

    [Test]
    public async Task IsEqualTo_NoImplicitOperator_Surfaces_Diagnostic()
    {
        // When no implicit conversion exists, the converter throws InvalidOperationException
        // with a clear diagnostic. EqualsAssertion captures the evaluation exception and
        // re-throws it as an AssertionException whose InnerException is the original
        // InvalidOperationException — the diagnostic must be reachable via that chain.
        var source = new UnrelatedType("Example");

        var assertionException = await Assert.That(
                async () => await Assert.That(source).IsEqualTo("Example"))
            .Throws<AssertionException>();

        await Assert.That(assertionException!.InnerException).IsTypeOf<InvalidOperationException>();
        await Assert.That(assertionException.InnerException!.Message).StartsWith(
            $"No implicit conversion operator from '{typeof(UnrelatedType)}' to '{typeof(string)}' was found.");
    }

    [Test]
    public async Task IsNotEqualTo_NoImplicitOperator_Surfaces_Diagnostic()
    {
        var source = new UnrelatedType("Example");

        var assertionException = await Assert.That(
                async () => await Assert.That(source).IsNotEqualTo("Other"))
            .Throws<AssertionException>();

        await Assert.That(assertionException!.InnerException).IsTypeOf<InvalidOperationException>();
        await Assert.That(assertionException.InnerException!.Message).StartsWith(
            $"No implicit conversion operator from '{typeof(UnrelatedType)}' to '{typeof(string)}' was found.");
    }
}

#endif
