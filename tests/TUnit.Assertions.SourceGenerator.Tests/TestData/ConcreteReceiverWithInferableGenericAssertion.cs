using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>Test case: an <see cref="AssertionExtensionAttribute"/>-decorated
/// <see cref="Assertion{T}"/> subclass over a CONCRETE, covariance-candidate receiver type that
/// ALSO declares its own generic type parameter — but whose own parameter <c>T</c> is INFERABLE
/// from a plain value argument (<c>T tag</c>). Because the caller never has to name a type
/// argument, the covariant <c>&lt;TActual, T&gt;</c> overload binds on its own and the
/// inference-friendly pinned-receiver overload would be redundant, so the generator must NOT emit
/// it. Contrast with <c>ConcreteReceiverWithExtraGenericAssertion</c>, whose <c>Func&lt;T, bool&gt;</c>
/// parameter makes <c>T</c> non-inferable and so DOES get the pinned overload. See issue #5922.</summary>
[AssertionExtension("ConcreteReceiverWithInferableGenericTagged")]
public class ConcreteReceiverWithInferableGenericAssertion<T> : Assertion<System.Exception>
{
    private readonly T _tag;

    public ConcreteReceiverWithInferableGenericAssertion(AssertionContext<System.Exception> context, T tag)
        : base(context)
    {
        _tag = tag;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<System.Exception> metadata)
    {
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to be tagged";
}
