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
    ICallVerification CreateVerification(int memberId, string memberName, IArgumentMatcher[] matchers, Type[]? typeArguments);
}
