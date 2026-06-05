using System.Collections.Immutable;
using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Internal extension of the engine's verification surface that also filters by a generic method's
/// type arguments. Kept off the public <see cref="IMockEngineAccess"/> interface so adding generic
/// type-argument support does not break external implementers (a default interface method is not an
/// option while the library targets netstandard2.0). The only implementer is
/// <see cref="MockEngine{T}"/>; generic mock-call wrappers reach it via a type test and fall back to
/// the public, non-filtering surface for any custom engine.
/// </summary>
internal interface ITypeArgumentVerificationFactory
{
    /// <summary>
    /// Creates a call verification builder that filters recorded calls by the supplied type arguments
    /// (concrete types or <see cref="AnyType"/>/<see cref="AnyValueType"/> wildcards).
    /// </summary>
    ICallVerification CreateVerification(int memberId, string memberName, IArgumentMatcher[] matchers, ImmutableArray<Type> typeArguments);
}

/// <summary>
/// Shared verification-builder routing for <see cref="MockMethodCall{TReturn}"/> and
/// <see cref="VoidMockMethodCall"/>: non-generic calls use the public engine surface unchanged;
/// generic calls route through <see cref="ITypeArgumentVerificationFactory"/> (always implemented by
/// MockEngine). A custom <see cref="IMockEngineAccess"/> implementation falls back to the public
/// surface, losing type-argument filtering but never throwing.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class MockCallVerification
{
    public static ICallVerification Create(IMockEngineAccess engine, int memberId, string memberName, IArgumentMatcher[] matchers, ImmutableArray<Type> typeArguments)
        => !typeArguments.IsDefault && engine is ITypeArgumentVerificationFactory typeArgFactory
            ? typeArgFactory.CreateVerification(memberId, memberName, matchers, typeArguments)
            : engine.CreateVerification(memberId, memberName, matchers);
}
