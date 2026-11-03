namespace TUnit.TestProject;

public class ResultlessMethodDataSourceTests
{
    public async ValueTask ValueTaskRows()
    {
        await Task.Yield();
    }

    public async Task TaskRows()
    {
        await Task.Yield();
    }

    public ValueTask CompletedValueTaskRows() => ValueTask.CompletedTask;
    public Task CompletedTaskRows() => Task.CompletedTask;

    [Test]
    [InstanceMethodDataSource(nameof(ValueTaskRows))]
    [InstanceMethodDataSource(nameof(TaskRows))]
    public async Task Awaits_Resultless_Source(object? value)
    {
        await Assert.That(value).IsNull();
    }

    [Test]
    [InstanceMethodDataSource(nameof(CompletedValueTaskRows))]
    [InstanceMethodDataSource(nameof(CompletedTaskRows))]
    public async Task Completed_Resultless_Source_Has_One_Null_Column(object? value)
    {
        await Assert.That(value).IsNull();
    }
}
