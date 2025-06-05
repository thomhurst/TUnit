using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[UniqueObjectsOnEnumerableDataGeneratorTestsGenerator]
public class UniqueObjectsOnEnumerableDataGeneratorTests(object classValue)
{
    [Test, UniqueObjectsOnEnumerableDataGeneratorTestsGenerator]
    public async Task Test(object methodValue)
    {
        await Assert.That(classValue).IsNotSameReferenceAs(methodValue);
    }
}

public class UniqueObjectsOnEnumerableDataGeneratorTestsGenerator : DataSourceGeneratorAttribute<object>
{
    public override IEnumerable<Func<object>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => new object();
        yield return () => new object();
        yield return () => new object();
    }
}