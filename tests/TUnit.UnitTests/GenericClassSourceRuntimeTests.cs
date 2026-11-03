using TUnit.Core.Enums;

namespace TUnit.UnitTests;

public class GenericClassSourceRuntimeTests
{
    [Test]
    public async Task Instance_Conversion_Preserves_Inferred_Source_Settings()
    {
        var source = new MethodDataSourceAttribute(typeof(GenericClassSourceRuntimeTests), "Rows");
        GenericClassRowPartition.ConfigureInferredSource(source, true, typeof(string));

        var converted = source.ToInstanceVariant();
        await Assert.That(converted.FilterClassRows).IsTrue();
        await Assert.That(converted.InferredClassTypeArguments).IsEquivalentTo(new[] { typeof(string) });
        await Assert.That(converted.SkipIfEmpty).IsFalse();

        // The converted external source must still discard an all-null inferred row.
        converted.Factory = _ => OneRow(() => Task.FromResult<object?[]?>([null]));
        await Assert.That((await ReadFactories(converted)).Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(false, 1)]
    [Arguments(true, 0)]
    public async Task Inferred_Null_Row_Is_Retained_Only_For_Own_Class(bool external, int expectedCount)
    {
        var source = external
            ? new MethodDataSourceAttribute(typeof(GenericClassSourceRuntimeTests), "Rows")
            : new MethodDataSourceAttribute("Rows");
        source.Factory = _ => OneRow(() => Task.FromResult<object?[]?>([null]));
        GenericClassRowPartition.ConfigureInferredSource(source, true, typeof(string));

        var rows = await ReadFactories(source);
        await Assert.That(rows.Count).IsEqualTo(expectedCount);
        if (rows.Count > 0)
        {
            await Assert.That((await rows[0]())![0]).IsNull();
        }
    }

    [Test]
    public async Task Inferred_Filter_Reuses_First_Fetch_Then_Creates_Fresh_Objects()
    {
        var calls = 0;
        var source = new MethodDataSourceAttribute("Rows")
        {
            Factory = _ => OneRow(() =>
            {
                calls++;
                return Task.FromResult<object?[]?>([new object()]);
            })
        };
        GenericClassRowPartition.ConfigureInferredSource(source, true, typeof(object));

        var factories = await ReadFactories(source);
        await Assert.That(calls).IsEqualTo(1);
        var first = await factories[0]();
        await Assert.That(calls).IsEqualTo(1);
        var second = await factories[0]();
        await Assert.That(calls).IsEqualTo(2);
        await Assert.That(ReferenceEquals(first![0], second![0])).IsFalse();
    }

    [Test]
    [Arguments(false, 1)]
    [Arguments(true, 0)]
    public async Task Empty_Inferred_Source_Has_No_Explicit_Fallback(bool inferred, int expectedCount)
    {
        var source = new MethodDataSourceAttribute("Rows") { Factory = _ => EmptyRows() };
        GenericClassRowPartition.ConfigureInferredSource(source, inferred, typeof(string));

        var factories = await ReadFactories(source);
        await Assert.That(factories.Count).IsEqualTo(expectedCount);
        if (factories.Count > 0)
        {
            await Assert.That((await factories[0]())!.Length).IsEqualTo(0);
        }
    }

    [Test]
    public async Task Declared_Constructor_Mapping_Overrides_Leading_Runtime_Value()
    {
        var source = new ArgumentsAttribute(7, "constructor-value");
        GenericClassRowPartition.ConfigureInferredSource(source, false, typeof(string));
        var partition = GenericClassRowPartition.Create([typeof(string)], GenericClassRowFallback.None, [source], 0);
        var rows = await ReadFactories(partition[0]);
        await Assert.That(rows.Count).IsEqualTo(1);
        await Assert.That((await rows[0]())![1]).IsEqualTo("constructor-value");
    }

    [Test]
    [Arguments(false, 0)]
    [Arguments(true, 1)]
    public async Task Constraint_Violations_Report_Only_Actual_Source_Rows(bool populated, int expectedCount)
    {
        var source = new MethodDataSourceAttribute("Rows")
        {
            Factory = _ => populated ? OneRow(() => Task.FromResult<object?[]?>(["invalid"])) : EmptyRows(),
        };
        GenericClassRowPartition.ConfigureInferredSource(source, true, typeof(string));
        var entry = new GenericConstraintViolationEntry
        {
            MethodName = "Test", FilePath = "test.cs", LineNumber = 1,
            MethodMetadata = null!, Messages = ["constraint violated"],
            DataSources = [GenericConstraintViolationDataSource.Create("constraint violated", source)],
        };
        var metadata = entry.ToTestMetadata(typeof(ConstraintSubject<>), "session");
        var rows = await ReadFactories(metadata.DataSources.Single());
        await Assert.That(rows.Count).IsEqualTo(expectedCount);
        await Assert.That(metadata.DataSources.Single().SkipIfEmpty).IsFalse();
        if (populated)
        {
            await Assert.That(async () => { await rows[0](); }).Throws<Exception>();
        }
    }

    private class ConstraintSubject<T> where T : struct;

    private static async IAsyncEnumerable<Func<Task<object?[]?>>> OneRow(Func<Task<object?[]?>> factory)
    {
        await Task.CompletedTask;
        yield return factory;
    }

    private static async IAsyncEnumerable<Func<Task<object?[]?>>> EmptyRows()
    {
        await Task.CompletedTask;
        yield break;
    }

    private static async Task<List<Func<Task<object?[]?>>>> ReadFactories(IDataSourceAttribute source)
    {
        var factories = new List<Func<Task<object?[]?>>>();
        await foreach (var factory in source.GetDataRowsAsync(new DataGeneratorMetadata
        {
            TestBuilderContext = null!, MembersToGenerate = [], TestInformation = null,
            Type = DataGeneratorType.ClassParameters, TestSessionId = string.Empty,
            TestClassInstance = null, ClassInstanceArguments = null
        }))
        {
            factories.Add(factory);
        }
        return factories;
    }
}
