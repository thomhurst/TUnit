namespace TUnit.Mocks;

/// <summary>
/// Common non-generic interface for <see cref="Mock{T}"/>, enabling batch operations via <see cref="MockRepository"/>.
/// </summary>
public interface IMock
{
    /// <summary>The mock implementation object (non-generic accessor).</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    object ObjectInstance { get; }

    /// <summary>Verifies all registered setups were invoked at least once.</summary>
    void VerifyAll();

    /// <summary>Fails if any recorded call was not matched by a prior verification.</summary>
    void VerifyNoOtherCalls();

    /// <summary>
    /// Clears recorded call history and each setup's invoke count, so <c>VerifyAll</c> only counts calls made afterwards.
    /// Setups and state are kept, including the position of sequenced setups (e.g. <c>ReturnsSequentially</c>).
    /// </summary>
    void ClearCalls();

    /// <summary>Clears all setups and call history.</summary>
    void Reset();
}
