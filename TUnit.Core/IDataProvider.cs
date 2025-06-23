namespace TUnit.Core;

/// <summary>
/// Provides data for parameterized tests.
/// </summary>
public interface IDataProvider
{
    /// <summary>
    /// Gets the test data as a sequence of parameter arrays.
    /// Each array represents one set of arguments for a test method.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains an enumerable sequence of parameter arrays.</returns>
    Task<IEnumerable<object?[]>> GetData();
}