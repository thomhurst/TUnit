namespace TUnit.Mock;

/// <summary>
/// Common non-generic interface for <see cref="Mock{T}"/>, enabling batch operations via <see cref="MockRepository"/>.
/// </summary>
public interface IMock
{
    /// <summary>Verifies all registered setups were invoked at least once.</summary>
    void VerifyAll();

    /// <summary>Fails if any recorded call was not matched by a prior verification.</summary>
    void VerifyNoOtherCalls();

    /// <summary>Clears all setups and call history.</summary>
    void Reset();
}
