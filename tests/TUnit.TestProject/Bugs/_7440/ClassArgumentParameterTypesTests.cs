using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7440;

[EngineTest(ExpectedResult.Pass)]
[Arguments(1)]
[IntRows7440]
public class ClassArgumentParameterTypesTests<T>(T value)
{
    [Test]
    [Arguments(2)]
    public async Task Run(decimal number)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That(value is int actual && (actual == 1 || actual == 7)).IsTrue();
        await Assert.That(number).IsEqualTo(2m);
    }
}

public class IntRows7440Attribute : DataSourceGeneratorAttribute<int>
{
    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
        => [() => 7];
}

[EngineTest(ExpectedResult.Pass)]
[Arguments(3)]
public class DecimalConstructorArgumentTests(decimal value)
{
    [Test]
    [Arguments(4)]
    public async Task Run(int number)
    {
        await Assert.That(value).IsEqualTo(3m);
        await Assert.That(number).IsEqualTo(4);
    }
}
