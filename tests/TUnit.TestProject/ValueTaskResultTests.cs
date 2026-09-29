namespace TUnit.TestProject;

public class ValueTaskResultTests
{
    [Test]
    public ValueTask<int> GenericValueTaskResult() => ValueTask.FromResult(42);

    [Test]
    [GenerateGenericTest(typeof(int))]
    public ValueTask<int> GenericMethodValueTaskResult<T>() => ValueTask.FromResult(43);
}
