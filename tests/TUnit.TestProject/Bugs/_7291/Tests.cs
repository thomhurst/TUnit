using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

// Deliberately outside TUnit.TestProject: the bug only appears when the first namespace segment is "Core".
namespace Core.Api.Tests;

/// <summary>
/// Regression fixture for PR #7291: property injection sources are generated inside
/// namespace TUnit.Core, so an unqualified Core.Api.X there binds to TUnit.Core.Api.X and fails
/// with CS0234. Every type name the generator writes for these properties must be globally
/// qualified: the containing type, the data source attribute, generic base type arguments and
/// constraints, and enum casts in attribute arguments (including keyword-named enums).
/// https://github.com/thomhurst/TUnit/pull/7291
/// </summary>
public enum Mode { A, B }

public enum @event { A, B }

public sealed class InMemoryDatabase : IAsyncInitializer
{
    public bool IsInitialized { get; private set; }

    public Task InitializeAsync()
    {
        IsInitialized = true;
        return Task.CompletedTask;
    }
}

public sealed class ModeDataSourceAttribute : DataSourceGeneratorAttribute<string>
{
    public Mode Mode { get; set; }

    public @event Kind { get; set; }

    protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => $"{Mode}-{Kind}";
    }
}

public interface IMarker;

public abstract class GenericFixtureBase<TProgram> where TProgram : class, IMarker
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public InMemoryDatabase? Database { get; init; }
}

[EngineTest(ExpectedResult.Pass)]
public class Tests : GenericFixtureBase<Tests>, IMarker
{
    [ClassDataSource<InMemoryDatabase>(Shared = SharedType.PerTestSession)]
    public required InMemoryDatabase Db { get; init; }

    [ModeDataSource(Mode = Mode.B, Kind = @event.B)]
    public string? Value { get; set; }

    [Test]
    public async Task Properties_Are_Injected()
    {
        await Assert.That(Db.IsInitialized).IsTrue();
        await Assert.That(Database).IsNotNull();
        await Assert.That(Database!.IsInitialized).IsTrue();
        await Assert.That(Value).IsEqualTo("B-B");
    }
}
