using System.ComponentModel;

namespace TUnit.Mocks;

/// <summary>
/// Provides access to the mock engine for generated code. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMockEngineAccess<T> where T : class
{
    /// <summary>The mock engine instance.</summary>
    MockEngine<T> Engine { get; }
}
