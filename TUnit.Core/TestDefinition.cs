namespace TUnit.Core;

/// <summary>
/// Marker interface for test definitions
/// </summary>
public interface ITestDefinition
{
}

/// <summary>
/// Base test definition
/// </summary>
public class TestDefinition : ITestDefinition
{
}

/// <summary>
/// Generic test definition
/// </summary>
public class TestDefinition<T> : TestDefinition where T : class
{
}