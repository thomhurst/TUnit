using System.ComponentModel;

namespace TUnit.Mocks;

/// <summary>
/// Implemented by generated mock impl classes to allow reverse-lookup
/// from a mock object to its <see cref="Mock{T}"/> wrapper without using
/// ConditionalWeakTable. Not intended for direct use.
/// </summary>
/// <remarks>
/// This interface is public because it must be implemented by source-generated code
/// in the user's assembly. The setter is required because the wrapper is assigned by
/// <see cref="Mock{T}"/>'s constructor after the impl object is created.
/// <see cref="EditorBrowsableAttribute"/> hides it from IDE autocompletion.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IMockObject
{
    /// <summary>
    /// The <see cref="IMock"/> wrapper for this mock object.
    /// Set once by the <see cref="Mock{T}"/> constructor; should not be reassigned.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    IMock? MockWrapper { get; set; }
}
