using TUnit.Assertions.Attributes;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Issue #5922: an <see cref="AssertionExtensionAttribute"/> class that declares its own generic
/// parameter over a concrete, non-sealed (covariance-candidate) receiver gets a covariant
/// <c>&lt;TActual, T&gt;</c> overload (so a more-derived static receiver can bind). When at least one
/// own type parameter is NOT inferable from the value arguments (e.g. it appears only inside a
/// <c>Func&lt;T, bool&gt;</c>), the generator ALSO emits an inference-friendly pinned <c>&lt;T&gt;</c>
/// overload whose receiver is the concrete type, so the exact-receiver call site need only name the
/// class's own argument. When every own type parameter IS inferable, no pinned overload is emitted —
/// it would be redundant with the covariant overload.
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
    public async Task Inferable_Own_Generic_Binds_With_Full_Inference_And_No_Pinned_Overload()
    {
        var ex = new Exception("boom");

        // Here the class's own type arg is inferable from the value argument (HasTag<int>(42)), so
        // the caller writes NO type arguments. Because every own type parameter is inferable, the
        // generator does NOT emit a pinned-receiver overload (it would be pure dead weight) — only
        // the covariant overload exists, and it binds via full inference (TActual from the receiver,
        // T from the value). There is therefore no second overload and no ambiguity to resolve.
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

    // Always passes; this fixture is a compile-time overload-resolution guard only — the predicate
    // is never evaluated, so its result does not matter.
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Exception> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => "to match the payload predicate";
}

/// <summary>Issue #5922 fixture: own generic parameter IS inferable (a plain value argument), so the
/// generator emits only the covariant overload and no pinned-receiver overload.</summary>
[AssertionExtension("HasTag")]
public class InferenceTaggedExceptionAssertion<T> : Assertion<Exception>
{
    private readonly T _tag;

    public InferenceTaggedExceptionAssertion(AssertionContext<Exception> context, T tag)
        : base(context)
    {
        _tag = tag;
    }

    // Always passes; this fixture is a compile-time overload-resolution guard only.
    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<Exception> metadata)
        => Task.FromResult(AssertionResult.Passed);

    protected override string GetExpectation() => $"to be tagged with {_tag}";
}
