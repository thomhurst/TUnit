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

            // Only a disposable scope can dispose what it resolves. Otherwise the object tracker keeps
            // disposing the returned objects, as it does for any other data source.
            var scopeOwnsCreatedObjects = scope is IDisposable or IAsyncDisposable;

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
                    var created = Create(scope, x);

                    if (scopeOwnsCreatedObjects)
                    {
                        // The scope owns what it resolves and disposes it with itself above, so the
                        // object tracker must not dispose it a second time when the test ends.
                        ObjectTracker.MarkExternallyOwned(created);
                    }

                    return created;
                })
                .ToArray();
        };
    }

    /// <summary>
    /// Creates the scope that resolves one test's arguments. A disposable scope is disposed when the
    /// test ends, after its last attempt.
    /// </summary>
    public abstract TScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata);

    /// <summary>
    /// Resolves an argument of type <paramref name="type"/> from <paramref name="scope"/>.
    /// </summary>
    /// <remarks>
    /// When the scope is disposable, it owns what this method returns: TUnit disposes the scope, and the
    /// scope is expected to dispose the objects it resolved, so TUnit does not dispose them itself.
    /// When the scope is not disposable, TUnit disposes disposable returned objects when the test ends.
    /// </remarks>
    public abstract object? Create(TScope scope, Type type);
}
