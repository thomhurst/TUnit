namespace TUnit.Core.Interfaces;

/// <summary>
/// Provides access to test identification properties.
/// Accessed via <see cref="TestDetails.Identity"/>.
/// </summary>
public interface ITestIdentity
{
    /// <summary>
    /// Gets the unique identifier for this test.
    /// </summary>
    string TestId { get; }

    /// <summary>
    /// Gets the display name of this test.
    /// </summary>
    string TestName { get; }

    /// <summary>
    /// Gets the name of the method that defines this test.
    /// </summary>
    string MethodName { get; }
}
