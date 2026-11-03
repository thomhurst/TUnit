using TUnit.Core.Enums;
using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

public class DataSourceCollectionTests
{
    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    public async Task Null_Collection_Contributes_No_Rows(int variant)
    {
        var rows = await ReadRows(CreateSource(variant, null));
        await Assert.That(rows.Count).IsEqualTo(0);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    [Arguments(3)]
    [Arguments(4)]
    [Arguments(5)]
    [Arguments(6)]
    [Arguments(7)]
    public async Task Null_Element_Is_A_Row(int variant)
    {
        var rows = await ReadRows(CreateSource(variant, [null, "value"]));
        await Assert.That(rows.Count).IsEqualTo(2);
        await Assert.That(rows[0]!.Length).IsEqualTo(1);
        await Assert.That(rows[0]![0]).IsNull();
        await Assert.That(rows[1]![0]).IsEqualTo("value");
    }

    [Test]
    public async Task Null_Scalar_Remains_A_Row()
    {
        var rows = await ReadRows(DataSourceFactories.FromTask<string?>(() => Task.FromResult<string?>(null)));
        await Assert.That(rows.Count).IsEqualTo(1);
        await Assert.That(rows[0]![0]).IsNull();
    }

    private static IAsyncEnumerable<Func<Task<object?[]?>>> CreateSource(int variant, string?[]? values)
    {
        var metadata = new DataGeneratorMetadata
        {
            TestBuilderContext = null!, MembersToGenerate = [], TestInformation = null,
            Type = DataGeneratorType.TestParameters, TestSessionId = string.Empty,
            TestClassInstance = new object(), ClassInstanceArguments = null,
        };
        return variant switch
        {
            0 => DataSourceFactories.FromEnumerable(() => values),
            1 => DataSourceFactories.FromTask(() => Task.FromResult<IEnumerable<string?>?>(values)),
            2 => DataSourceFactories.FromValueTask(() => ValueTask.FromResult<IEnumerable<string?>?>(values)),
            3 => DataSourceFactories.FromAsyncEnumerable(() => values is null ? null! : AsyncValues(values)),
            4 => DataSourceFactories.FromInstanceEnumerable(metadata, _ => values),
            5 => DataSourceFactories.FromInstanceTask(metadata, _ => Task.FromResult<IEnumerable<string?>?>(values)),
            6 => DataSourceFactories.FromInstanceValueTask(metadata, _ => ValueTask.FromResult<IEnumerable<string?>?>(values)),
            7 => DataSourceFactories.FromInstanceAsyncEnumerable(metadata, _ => values is null ? null! : AsyncValues(values)),
            _ => throw new ArgumentOutOfRangeException(nameof(variant)),
        };
    }

    private static async IAsyncEnumerable<string?> AsyncValues(string?[] values)
    {
        foreach (var value in values)
        {
            yield return value;
        }
        await Task.CompletedTask;
    }

    private static async Task<List<object?[]?>> ReadRows(IAsyncEnumerable<Func<Task<object?[]?>>> factories)
    {
        var rows = new List<object?[]?>();
        await foreach (var factory in factories)
        {
            rows.Add(await factory());
        }
        return rows;
    }
}
