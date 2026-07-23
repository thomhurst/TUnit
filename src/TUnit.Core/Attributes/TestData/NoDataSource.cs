namespace TUnit.Core;

/// <summary>
/// No-op data source used for tests that have no data source. Yields a single empty data row
/// so non-parameterized tests flow through the same building pipeline as data-driven tests.
/// </summary>
/// <remarks>
/// <see cref="TestContext.ClassDataSource"/> and <see cref="TestContext.MethodDataSource"/> return
/// <see cref="Instance"/> when the test has no class or method data source respectively, so callers
/// can pattern-match with <c>is NoDataSource</c> instead of null-checking.
/// </remarks>
public sealed class NoDataSource : IDataSourceAttribute
{
    private static readonly Task<object?[]?> _emptyRowTask = Task.FromResult<object?[]?>([]);

    /// <summary>
    /// Gets the singleton instance, shared process-wide across all tests.
    /// </summary>
    public static readonly NoDataSource Instance = new();

    private NoDataSource()
    {
    }

    /// <inheritdoc />
    /// <remarks>Always <c>false</c>. The setter is a no-op — this type is a shared singleton, so writes must not leak across tests.</remarks>
    public bool SkipIfEmpty { get => false; set { } }

    /// <inheritdoc />
    /// <remarks>Always <c>false</c>. The setter is a no-op — this type is a shared singleton, so writes must not leak across tests.</remarks>
    public bool DeferEnumeration { get => false; set { } }

    public async IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return static () => _emptyRowTask;
        await default(ValueTask);
    }
}
