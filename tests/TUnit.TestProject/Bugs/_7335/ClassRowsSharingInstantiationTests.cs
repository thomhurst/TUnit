using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7335;

public class IntGenerator : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => 7;
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[IntGenerator]
public class ClassRowsSharingInstantiation<T>(T value)
{
    [Test]
    [Arguments(2)]
    public async Task Test(T arg)
    {
        await Assert.That(value is 1 or 7).IsTrue();
        await Assert.That(arg).IsEqualTo((T)(object)2);
        await Assert.That(TestContext.Current!.Metadata.TestDetails.TestClassArguments)
            .IsEquivalentTo(new object?[] { value });
    }
}

[EngineTest(ExpectedResult.Pass)]
[IntGenerator]
[Arguments(1)]
public class ClassRowsSharingInstantiationParameterless<T>(T value)
{
    [Test]
    public async Task Test()
    {
        await Assert.That(value is 1 or 7).IsTrue();
        await Assert.That(TestContext.Current!.Metadata.TestDetails.TestClassArguments)
            .IsEquivalentTo(new object?[] { value });
    }
}
