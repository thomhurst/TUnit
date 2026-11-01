using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Tracking;

namespace TUnit.Core;

public abstract class DependencyInjectionDataSourceAttribute<TScope> : UntypedDataSourceGeneratorAttribute
{
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            // Create a new scope for each test execution
            var scope = CreateScope(dataGeneratorMetadata);

            // Set up disposal for this specific scope in the current test context
            dataGeneratorMetadata.TestBuilderContext.Current.Events.OnDispose += async (_, _) =>
            {
                if (scope is IAsyncDisposable asyncDisposable)
                {
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                }
                else if (scope is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            };

            return dataGeneratorMetadata.MembersToGenerate
                .Select(m => m switch
                {
                    PropertyMetadata prop => prop.Type,
                    ParameterMetadata param => param.Type,
                    ClassMetadata cls => cls.Type,
                    MethodMetadata method => method.Type,
                    _ => throw new InvalidOperationException($"Unknown member type: {m.GetType()}")
                })
                .Select(x =>
                {
                    // The scope owns what it resolves and disposes it with itself above, so the
                    // object tracker must not dispose it a second time when the test ends.
                    var created = Create(scope, x);
                    ObjectTracker.MarkExternallyOwned(created);
                    return created;
                })
                .ToArray();
        };
    }

    public abstract TScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata);

    public abstract object? Create(TScope scope, Type type);
}
