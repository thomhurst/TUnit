using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7490;

[Arguments(1)]
[InheritedStringSource]
public abstract class MixedSourcesBase;

public class InheritedStringSource : DataSourceGeneratorAttribute<string>
{
    protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => "source";
    }
}

[EngineTest(ExpectedResult.Pass)]
public class InheritedMixedSources<T>(T value) : MixedSourcesBase
{
    [Test]
    [Arguments(2)]
    [Arguments("method")]
    public void Test(T methodValue)
    {
        if (!((object?)value is 1 && (object?)methodValue is 2
              || (object?)value is "source" && (object?)methodValue is "method"))
        {
            throw new InvalidOperationException("Expected the matching inherited constructor source and method row.");
        }
    }
}
