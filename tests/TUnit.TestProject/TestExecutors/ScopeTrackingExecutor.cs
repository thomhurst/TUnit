using TUnit.Core.Interfaces;

namespace TUnit.TestProject.TestExecutors;

/// <summary>
/// Base class for scope-tracking test executors.
/// Each executor records which scope level was used to execute the test.
/// </summary>
public abstract class ScopeTrackingExecutor : ITestExecutor
{
    /// <summary>
    /// Thread-local storage for tracking which executor actually ran for each test.
    /// Key: test method name, Value: executor scope name.
    /// </summary>
    public static readonly Dictionary<string, string> ExecutorUsedByTest = new();
    private static readonly object Lock = new();

    protected abstract string ScopeName { get; }

    public async ValueTask ExecuteTest(TestContext context, Func<ValueTask> action)
    {
        lock (Lock)
        {
            ExecutorUsedByTest[context.Metadata.TestDetails.MethodName] = ScopeName;
        }

        await action();
    }

    public static string? GetExecutorUsed(string testName)
    {
        lock (Lock)
        {
            return ExecutorUsedByTest.GetValueOrDefault(testName);
        }
    }

    public static void Clear()
    {
        lock (Lock)
        {
            ExecutorUsedByTest.Clear();
        }
    }
}

/// <summary>
/// Executor that marks itself as "Method" scope level.
/// </summary>
public class MethodScopeExecutor : ScopeTrackingExecutor
{
    protected override string ScopeName => "Method";
}

/// <summary>
/// Executor that marks itself as "Class" scope level.
/// </summary>
public class ClassScopeExecutor : ScopeTrackingExecutor
{
    protected override string ScopeName => "Class";
}

/// <summary>
/// Executor that marks itself as "Assembly" scope level.
/// </summary>
public class AssemblyScopeExecutor : ScopeTrackingExecutor
{
    protected override string ScopeName => "Assembly";
}
