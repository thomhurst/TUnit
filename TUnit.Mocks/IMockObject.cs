using System.ComponentModel;

namespace TUnit.Mocks;

/// <summary>
/// Implemented by generated mock impl classes to allow reverse-lookup
/// from a mock object to its <see cref="Mock{T}"/> wrapper without using
/// ConditionalWeakTable. Not intended for direct use.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMockObject
{
    /// <summary>
    /// The <see cref="IMock"/> wrapper for this mock object.
    /// Set by the <see cref="Mock{T}"/> constructor.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    IMock? MockWrapper { get; set; }
}
