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

[Arguments(1, "literal")]
[InheritedConstructorSource]
public abstract class SelectedConstructorSourcesBase;

public class InheritedConstructorSource : DataSourceGeneratorAttribute<string, int>
{
    protected override IEnumerable<Func<(string, int)>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => ("source", 42);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class InheritedSelectedConstructor<T> : SelectedConstructorSourcesBase
{
    private readonly T _value;
    private readonly object _other;

    public InheritedSelectedConstructor(object other, T value)
    {
        throw new InvalidOperationException("The unmarked constructor must not be selected.");
    }

    [TestConstructor]
    public InheritedSelectedConstructor(T value, object other)
    {
        _value = value;
        _other = other;
    }

    [Test]
    [Arguments(2)]
    [Arguments("method")]
    public void Test(T methodValue)
    {
        if (!((object?)_value is 1 && _other is "literal" && (object?)methodValue is 2
              || (object?)_value is "source" && _other is 42 && (object?)methodValue is "method"))
        {
            throw new InvalidOperationException("Expected the inherited row to use the marked constructor.");
        }
    }
}
