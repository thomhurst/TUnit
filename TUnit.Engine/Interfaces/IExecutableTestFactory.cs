using TUnit.Core;

namespace TUnit.Engine.Interfaces;

/// <summary>
/// Factory for creating strongly-typed executable tests
/// </summary>
public interface IExecutableTestFactory
{
    /// <summary>
    /// Creates an executable test with all required parameters
    /// </summary>
    AbstractExecutableTest CreateExecutableTest(
        string testId,
        string displayName,
        object[] arguments,
        object[] classArguments,
        Dictionary<string, object?> propertyValues,
        Func<TestContext, CancellationToken, Task>[] beforeTestHooks,
        Func<TestContext, CancellationToken, Task>[] afterTestHooks,
        TestContext context,
        TestMetadata metadata);
}

