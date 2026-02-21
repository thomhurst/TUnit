using System.ComponentModel;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Setup;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks;

/// <summary>
/// Non-generic interface for interacting with a mock engine.
/// Used by property accessor structs that don't know the mock's type parameter.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMockEngineAccess
{
    /// <summary>Registers a new method setup with the engine.</summary>
    void AddSetup(MethodSetup setup);

    /// <summary>Creates a call verification builder for the specified member.</summary>
    ICallVerification CreateVerification(int memberId, string memberName, IArgumentMatcher[] matchers);
}
