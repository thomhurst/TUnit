namespace TUnit.Core;

/// <summary>
/// Defines a contract for objects that require asynchronous initialization.
/// Objects implementing this interface will have InitializeAsync called
/// after construction but before use in tests.
/// </summary>
public interface IAsyncInitializable
{
    /// <summary>
    /// Performs asynchronous initialization of the object.
    /// </summary>
    /// <returns>A task representing the asynchronous initialization operation.</returns>
    Task InitializeAsync();
}
