using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests.TestData;

/// <summary>Test case: an <see cref="AssertionExtensionAttribute"/>-decorated
/// <see cref="Assertion{T}"/> subclass over a CONCRETE receiver type that ALSO declares
/// its own generic type parameter on the class. The generated extension method must merge
/// both the covariant receiver-type parameter and the class's own type parameter into a
/// single generic parameter list (e.g. <c>&lt;TActual, T&gt;</c>), NOT emit them as two
/// adjacent blocks (<c>&lt;TActual&gt;&lt;T&gt;</c>) which is invalid C# syntax.
/// Uses <see cref="System.Exception"/> as the receiver to anchor the fixture to a BCL
/// non-sealed class (the covariance candidate the generator looks for) without adding
/// a stand-in type to the test surface.</summary>
[AssertionExtension("ConcreteReceiverWithExtraGenericMatches")]
public class ConcreteReceiverWithExtraGenericAssertion<T> : Assertion<System.Exception>
{
    private readonly Func<T, bool> _predicate;

    public ConcreteReceiverWithExtraGenericAssertion(AssertionContext<System.Exception> context, Func<T, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<System.Exception> metadata)
    {
        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => "to match the predicate";
}
