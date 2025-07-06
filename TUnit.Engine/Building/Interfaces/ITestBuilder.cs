namespace TUnit.Engine.Building.Interfaces;

/// <summary>
/// Interface for building executable tests from expanded test data
/// </summary>
public interface ITestBuilder
{
    /// <summary>
    /// Builds an executable test from expanded test data
    /// </summary>
    /// <param name="expandedData">The expanded test data containing all necessary information</param>
    /// <returns>An executable test ready for execution</returns>
    Task<ExecutableTest> BuildTestAsync(ExpandedTestData expandedData);
}
