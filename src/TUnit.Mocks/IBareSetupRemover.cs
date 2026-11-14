using TUnit.Mocks.Setup;

namespace TUnit.Mocks;

/// <summary>
/// Lets a void mock-call wrapper withdraw the setup it registered eagerly when the wrapper turns out
/// to be used for verification. Kept off the public <see cref="IMockEngineAccess"/> interface so it
/// does not break external implementers. The only implementer is <see cref="MockEngine{T}"/>; wrappers
/// reach it via a type test, so a custom engine simply keeps the setup.
/// </summary>
internal interface IBareSetupRemover
{
    /// <summary>
    /// Removes <paramref name="setup"/> if nothing was configured on it and it was never invoked.
    /// Returns <see langword="true"/> if the setup was removed.
    /// </summary>
    bool TryRemoveBareSetup(MethodSetup setup);
}
