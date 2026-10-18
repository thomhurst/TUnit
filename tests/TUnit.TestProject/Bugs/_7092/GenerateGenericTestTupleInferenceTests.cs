using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7092;

[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestMethodDataSourceInferenceTests
{
    public static IEnumerable<Func<(int, string)>> Rows()
    {
        yield return () => (1, "a");
    }

    // Source-gen mode infers <int, string> from the data source's tuple type and adds it next to the
    // [GenerateGenericTest] instantiation. Both run every row. Reflection mode ran only <long, string>.
    [Test]
    [GenerateGenericTest(typeof(long), typeof(string))]
#pragma warning disable TUnit0001 // the analyzer does not resolve T1/T2 from the tuple
    [MethodDataSource(nameof(Rows))]
#pragma warning restore TUnit0001
    public async Task Tuple_Method_Data_Source_Infers_Its_Own_Types<T1, T2>(T1 first, T2 second)
    {
        await Assert.That(first is T1 && second is T2).IsTrue();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestMultipleMethodDataSourceInferenceTests
{
    public static IEnumerable<Func<(int, string)>> IntRows()
    {
        yield return () => (1, "a");
    }

    public static IEnumerable<Func<(string, string)>> StringRows()
    {
        yield return () => ("not a number", "b");
    }

    // Each inferred instantiation runs only the source that inferred it: <int, string> runs IntRows and
    // <string, string> runs StringRows. The [GenerateGenericTest] instantiation runs both sources.
    [Test]
    [GenerateGenericTest(typeof(object), typeof(object))]
#pragma warning disable TUnit0001 // the analyzer does not resolve T1/T2 from the tuple
    [MethodDataSource(nameof(IntRows))]
    [MethodDataSource(nameof(StringRows))]
#pragma warning restore TUnit0001
    public async Task Inferred_Instantiation_Runs_Only_Its_Own_Source<T1, T2>(T1 first, T2 second)
    {
        await Assert.That(first is T1 && second is T2).IsTrue();
    }
}

public class EightIntRowSource : DataSourceGeneratorAttribute<(int, int, int, int, int, int, int, int)>
{
    protected override IEnumerable<Func<(int, int, int, int, int, int, int, int)>> GenerateDataSources(DataGeneratorMetadata metadata)
    {
        yield return () => (1, 2, 3, 4, 5, 6, 7, 8);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestEightElementTupleTests
{
    // An eight-element tuple is ValueTuple<T1, ..., T7, ValueTuple<T8>> at run time. Source-gen mode reads the
    // eight logical elements, so the source infers eight ints and [GenerateGenericTest] adds nothing.
    // Reflection mode inferred ValueTuple<int> for T8 and added a test that failed.
    [Test]
    [GenerateGenericTest(typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int), typeof(int))]
    [EightIntRowSource]
    public async Task Eight_Element_Tuple_Infers_Eight_Types<T1, T2, T3, T4, T5, T6, T7, T8>(T1 a, T2 b, T3 c, T4 d, T5 e, T6 f, T7 g, T8 h)
    {
        await Assert.That(typeof(T8)).IsEqualTo(typeof(int));
        await Assert.That(h).IsEqualTo((T8)(object)8);
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestDuplicateMethodDataSourceInferenceTests
{
    public static IEnumerable<Func<(int, string)>> FirstRows()
    {
        yield return () => (1, "a");
    }

    public static IEnumerable<Func<(int, string)>> SecondRows()
    {
        yield return () => (2, "b");
    }

    // Both sources infer <int, string>. Source-gen mode keeps only the first source for that instantiation, so
    // <int, string> runs FirstRows and <object, object> runs both sources: three tests. Reflection mode also ran
    // SecondRows for <int, string>.
    [Test]
    [GenerateGenericTest(typeof(object), typeof(object))]
#pragma warning disable TUnit0001 // the analyzer does not resolve T1/T2 from the tuple
    [MethodDataSource(nameof(FirstRows))]
    [MethodDataSource(nameof(SecondRows))]
#pragma warning restore TUnit0001
    public async Task Duplicate_Inferred_Types_Keep_The_First_Source<T1, T2>(T1 first, T2 second)
    {
        await Assert.That(first is T1 && second is T2).IsTrue();
    }
}

public class GenericMethodDataSourceProvider7092
{
    public static IEnumerable<Func<(int, string)>> Rows()
    {
        yield return () => (1, "a");
    }
}

[EngineTest(ExpectedResult.Pass)]
public class GenerateGenericTestGenericMethodDataSourceProviderTests
{
    // Same name as the provider's member, with different element types. [MethodDataSource<T>] must infer
    // from T's Rows (<int, string>), not from this one (<string, string>).
    public static IEnumerable<Func<(string, string)>> Rows()
    {
        yield return () => ("x", "y");
    }

    [Test]
    [GenerateGenericTest(typeof(object), typeof(object))]
#pragma warning disable TUnit0001 // the analyzer does not resolve T1/T2 from the tuple
    [MethodDataSource<GenericMethodDataSourceProvider7092>(nameof(GenericMethodDataSourceProvider7092.Rows))]
#pragma warning restore TUnit0001
    public async Task Generic_Method_Data_Source_Infers_From_Its_Provider<T1, T2>(T1 first, T2 second)
    {
        await Assert.That(first is T1 && second is T2).IsTrue();
        await Assert.That(first).IsEqualTo((T1)(object)1);
    }
}
