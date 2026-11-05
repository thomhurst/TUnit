using TUnit.Core.Enums;
using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

/// <summary>
/// Tests for <see cref="DataSourceFactories.FromRuntimeValue"/> and <see cref="DataSourceFactories.FromInstanceRuntimeValue"/>,
/// which the generated factory uses for a private data member whose declared type does not give the row shape.
/// </summary>
public class DataSourceFactoriesRuntimeValueTests
{
    [Test]
    public async Task FromRuntimeValue_Null_Yields_No_Rows()
    {
        var rows = await ReadRowsAsync(DataSourceFactories.FromRuntimeValue(CreateMetadata(instance: null), static () => null));

        await Assert.That(rows).IsEmpty();
    }

    [Test]
    public async Task FromInstanceRuntimeValue_Null_Yields_No_Rows()
    {
        var rows = await ReadRowsAsync(DataSourceFactories.FromInstanceRuntimeValue(CreateMetadata(instance: new object()), static _ => null));

        await Assert.That(rows).IsEmpty();
    }

    [Test]
    public async Task Null_Sources_And_Awaited_Null_Results_Yield_No_Rows()
    {
        var metadata = CreateMetadata(new object());
        IAsyncEnumerable<Func<Task<object?[]?>>>[] sources =
        [
            DataSourceFactories.FromValue(static () => null),
            DataSourceFactories.FromEnumerable(static () => null),
            DataSourceFactories.FromTask<string?>(static () => null!),
            DataSourceFactories.FromTask(static () => Task.FromResult<string?>(null)),
            DataSourceFactories.FromValueTask(static () => ValueTask.FromResult<string?>(null)),
            DataSourceFactories.FromAsyncEnumerable<string>(static () => null!),
            DataSourceFactories.FromInstanceValue(metadata, static _ => null),
            DataSourceFactories.FromInstanceEnumerable(metadata, static _ => null),
            DataSourceFactories.FromInstanceTask<string?>(metadata, static _ => null!),
            DataSourceFactories.FromInstanceTask(metadata, static _ => Task.FromResult<string?>(null)),
            DataSourceFactories.FromInstanceValueTask(metadata, static _ => ValueTask.FromResult<string?>(null)),
            DataSourceFactories.FromInstanceAsyncEnumerable<string>(metadata, static _ => null!),
        ];

        foreach (var source in sources)
        {
            await Assert.That(await ReadRowsAsync(source)).IsEmpty();
        }
    }

    [Test]
    public async Task FromRuntimeValue_Array_Yields_A_Row_Per_Element()
    {
        var rows = await ReadRowsAsync(DataSourceFactories.FromRuntimeValue(CreateMetadata(instance: null), static () => new[] { 1, 2 }));

        await Assert.That(rows).Count().IsEqualTo(2);
        await Assert.That(rows[0]![0]).IsEqualTo(1);
        await Assert.That(rows[1]![0]).IsEqualTo(2);
    }

    private static async Task<List<object?[]?>> ReadRowsAsync(IAsyncEnumerable<Func<Task<object?[]?>>> factories)
    {
        var rows = new List<object?[]?>();
        await foreach (var factory in factories)
        {
            rows.Add(await factory());
        }

        return rows;
    }

    private static DataGeneratorMetadata CreateMetadata(object? instance) => new()
    {
        // Not read when turning a value into rows.
        TestBuilderContext = null!,
        MembersToGenerate = [],
        TestInformation = null,
        Type = DataGeneratorType.TestParameters,
        TestSessionId = string.Empty,
        TestClassInstance = instance,
        ClassInstanceArguments = null,
    };
}
