using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7092;

public class LongRowSource : DataSourceGeneratorAttribute<long>
{
    protected override IEnumerable<Func<long>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => 2L;
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestTypedDataSourceTests
{
    // Source-gen mode runs <int>(1) from [Arguments] and <long>(2) from the typed source, which infers <long>.
    // Reflection mode dropped the typed source's row: <int> took only the [Arguments] row.
    [Test]
    [GenerateGenericTest(typeof(int))]
    [Arguments(1)]
    [LongRowSource]
    public async Task Typed_Source_Runs_For_Its_Own_Type<T>(T value)
    {
        await Assert.That(value!.GetType()).IsEqualTo(typeof(T));
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestCoveredByTypedDataSourceTests
{
    // Source-gen mode runs <int>(1) and <long>(2). [GenerateGenericTest(typeof(long))] adds nothing because
    // the typed source already produced <long>. Reflection mode also ran <long>(1).
    [Test]
    [GenerateGenericTest(typeof(long))]
    [Arguments(1)]
    [LongRowSource]
    public async Task Generic_Type_Inferred_From_Typed_Source_Is_Not_Repeated<T>(T value)
    {
        await Assert.That(value!.GetType()).IsEqualTo(typeof(T));
    }
}
