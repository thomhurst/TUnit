using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7388;

#pragma warning disable TUnit0001 // Null sources deliberately exercise the empty-source rule with parameterless tests.

[EngineTest(ExpectedResult.Pass)]
public class StaticNullMethodDataSourceTests
{
    public static object? ValueRows() => null;
    public static IEnumerable<int>? EnumerableRows() => null;
    public static Task<int>? TaskRows() => null;
    public static Task<string?> TaskResultRows() => Task.FromResult<string?>(null);
    public static Task<IEnumerable<int>?> TaskCollectionRows() => Task.FromResult<IEnumerable<int>?>(null);
    public static ValueTask<string?> ValueTaskResultRows() => ValueTask.FromResult<string?>(null);
    public static IAsyncEnumerable<int>? AsyncEnumerableRows() => null;
    public static object? RuntimeRows = null;

    [Test, MethodDataSource(nameof(ValueRows))]
    public async Task Value_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, MethodDataSource(nameof(EnumerableRows))]
    public async Task Enumerable_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, MethodDataSource(nameof(TaskRows))]
    public async Task Task_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, MethodDataSource(nameof(TaskResultRows))]
    public async Task TaskResult_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, MethodDataSource(nameof(TaskCollectionRows))]
    public async Task TaskCollection_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, MethodDataSource(nameof(ValueTaskResultRows))]
    public async Task ValueTaskResult_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, MethodDataSource(nameof(AsyncEnumerableRows))]
    public async Task AsyncEnumerable_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, MethodDataSource(nameof(RuntimeRows))]
    public async Task Runtime_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, MethodDataSource(nameof(ValueRows), SkipIfEmpty = true)]
    public void Value_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, MethodDataSource(nameof(EnumerableRows), SkipIfEmpty = true)]
    public void Enumerable_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, MethodDataSource(nameof(TaskRows), SkipIfEmpty = true)]
    public void Task_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, MethodDataSource(nameof(TaskResultRows), SkipIfEmpty = true)]
    public void TaskResult_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, MethodDataSource(nameof(TaskCollectionRows), SkipIfEmpty = true)]
    public void TaskCollection_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, MethodDataSource(nameof(ValueTaskResultRows), SkipIfEmpty = true)]
    public void ValueTaskResult_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, MethodDataSource(nameof(AsyncEnumerableRows), SkipIfEmpty = true)]
    public void AsyncEnumerable_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, MethodDataSource(nameof(RuntimeRows), SkipIfEmpty = true)]
    public void Runtime_Skipped() => throw new InvalidOperationException("Null source should skip the test.");
}

[EngineTest(ExpectedResult.Pass)]
public class InstanceNullMethodDataSourceTests
{
    public object? ValueRows => null;
    public IEnumerable<int>? EnumerableRows() => null;
    public Task<int>? TaskRows() => null;
    public Task<string?> TaskResultRows() => Task.FromResult<string?>(null);
    public Task<IEnumerable<int>?> TaskCollectionRows() => Task.FromResult<IEnumerable<int>?>(null);
    public ValueTask<string?> ValueTaskResultRows() => ValueTask.FromResult<string?>(null);
    public IAsyncEnumerable<int>? AsyncEnumerableRows() => null;
    public object? RuntimeRows = null;

    [Test, InstanceMethodDataSource(nameof(ValueRows))]
    public async Task Value_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, InstanceMethodDataSource(nameof(EnumerableRows))]
    public async Task Enumerable_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, InstanceMethodDataSource(nameof(TaskRows))]
    public async Task Task_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, InstanceMethodDataSource(nameof(TaskResultRows))]
    public async Task TaskResult_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, InstanceMethodDataSource(nameof(TaskCollectionRows))]
    public async Task TaskCollection_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, InstanceMethodDataSource(nameof(ValueTaskResultRows))]
    public async Task ValueTaskResult_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, InstanceMethodDataSource(nameof(AsyncEnumerableRows))]
    public async Task AsyncEnumerable_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, InstanceMethodDataSource(nameof(RuntimeRows))]
    public async Task Runtime_Runs_Without_Arguments()
        => await Assert.That(TestContext.Current!.Metadata.TestDetails.TestMethodArguments).IsEmpty();

    [Test, InstanceMethodDataSource(nameof(ValueRows), SkipIfEmpty = true)]
    public void Value_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, InstanceMethodDataSource(nameof(EnumerableRows), SkipIfEmpty = true)]
    public void Enumerable_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, InstanceMethodDataSource(nameof(TaskRows), SkipIfEmpty = true)]
    public void Task_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, InstanceMethodDataSource(nameof(TaskResultRows), SkipIfEmpty = true)]
    public void TaskResult_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, InstanceMethodDataSource(nameof(TaskCollectionRows), SkipIfEmpty = true)]
    public void TaskCollection_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, InstanceMethodDataSource(nameof(ValueTaskResultRows), SkipIfEmpty = true)]
    public void ValueTaskResult_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, InstanceMethodDataSource(nameof(AsyncEnumerableRows), SkipIfEmpty = true)]
    public void AsyncEnumerable_Skipped() => throw new InvalidOperationException("Null source should skip the test.");

    [Test, InstanceMethodDataSource(nameof(RuntimeRows), SkipIfEmpty = true)]
    public void Runtime_Skipped() => throw new InvalidOperationException("Null source should skip the test.");
}

[EngineTest(ExpectedResult.Pass)]
public class NullElementMethodDataSourceTests
{
    public static IEnumerable<string?> Rows() => [null];

    [Test, MethodDataSource(nameof(Rows), SkipIfEmpty = true)]
    public async Task Null_Element_Remains_A_Row(string? value) => await Assert.That(value).IsNull();
}

#pragma warning restore TUnit0001
