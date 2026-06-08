using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Issue #5922: an <see cref="AssertionExtensionAttribute"/> class that declares its own generic
/// parameter over a concrete, non-sealed (covariance-candidate) receiver gets TWO generated
/// overloads: a covariant <c>&lt;TActual, T&gt;</c> one (so a more-derived static receiver can
/// bind) and an inference-friendly pinned <c>&lt;T&gt;</c> one whose receiver is the concrete type.
///
/// These tests are primarily a COMPILE-TIME guard: the call sites below only compile if overload
/// resolution selects the right method without ambiguity (no CS0121). That is exactly what the
/// generator's own snapshot/compile-clean tests cannot prove, because they have no call sites.
/// </summary>
public class CovariantGenericExtensionInferenceTests
{
    public sealed class Payload
    {
        public bool Ok { get; init; }
    }

    [Test]
    public async Task Pinned_Overload_Exact_Receiver_Only_Needs_Own_Type_Argument()
    {
        var ex = new Exception("boom");

        // Receiver is exactly Exception: the pinned overload binds, so only the class's own
        // type argument (Payload) is named — the redundant <Exception> is NOT required.
        await Assert.That(ex).MatchesPayload<Payload>(p => p.Ok);
    }

    [Test]
    public async Task Covariant_Overload_Still_Binds_For_Subclass_Receiver()
    {
        var ex = new ArgumentException("bad arg");

        // Receiver is a subclass (ArgumentException). The pinned IAssertionSource<Exception>
        // overload is not applicable (the interface is invariant), so the covariant overload
        // binds — both type arguments are named because C# forbids partial specification.
        await Assert.That(ex).MatchesPayload<ArgumentException, Payload>(p => p.Ok);
    }

    [Test]
    public async Task Both_Overloads_Coexist_Without_Ambiguity_When_Type_Arg_Is_Inferable()
    {
        var ex = new Exception("boom");

        // Here the class's own type arg is inferable from the value argument, so NO type
        // arguments are written. Both overloads are applicable for an exact Exception receiver;
        // this only compiles because C#'s "more specific parameter types" rule prefers the
        // pinned IAssertionSource<Exception> receiver over the covariant IAssertionSource<TActual>.
        await Assert.That(ex).HasTag(42);
    }
}

/// <summary>Issue #5922 fixture: own generic parameter is NOT inferable (lambda predicate).</summary>
[AssertionExtension("MatchesPayload")]
public class InferencePayloadMatchesAssertion<T> : Assertion<Exception>
{
    private readonly Func<T, bool> _predicate;

    public InferencePayloadMatchesAssertion(AssertionContext<Exception> context, Func<T, bool> predicate)
        : base(context)
    {
        _predicate = predicate;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Exception> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => "to match the payload predicate";
}

/// <summary>Issue #5922 fixture: own generic parameter IS inferable (value argument), which
/// exercises the case where both overloads are simultaneously applicable.</summary>
[AssertionExtension("HasTag")]
public class InferenceTaggedExceptionAssertion<T> : Assertion<Exception>
{
    private readonly T _tag;

    public InferenceTaggedExceptionAssertion(AssertionContext<Exception> context, T tag)
        : base(context)
    {
        _tag = tag;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Exception> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => $"to be tagged with {_tag}";
}
