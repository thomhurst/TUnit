namespace TUnit.Mocks;

/// <summary>
/// Optional capability of a mock: clearing recorded call history while keeping setups and state.
/// Kept separate from <see cref="IMock"/> so adding it did not break existing implementers of that interface.
/// <see cref="Mock{T}"/> implements it; <see cref="MockRepository"/> calls it on every mock that does.
/// </summary>
public interface IMockCallHistory
{
    /// <summary>
    /// Clears recorded call history and each setup's invoke count, so <c>VerifyAll</c> only counts calls made afterwards.
    /// Setups and state are kept, including the position of sequenced setups (e.g. <c>ReturnsSequentially</c>).
    /// </summary>
    void ClearCalls();
}
