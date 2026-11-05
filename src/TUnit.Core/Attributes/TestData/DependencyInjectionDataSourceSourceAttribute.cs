using TUnit.Core.Discovery;
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

            // Register before resolving so a failed factory still releases its scope.
            var cleanup = dataGeneratorMetadata.TestBuilderContext.Current.Events.GetOrCreateDataRowScopes();
            var owner = cleanup.Register(scope);

            // Custom scopes can leave disposal of their resolved objects to the tracker.
            var scopeOwnsCreatedObjects = ScopeOwnsCreatedObjects(scope);
            owner.OwnsCreatedObjects = scopeOwnsCreatedObjects;

            var values = new object?[dataGeneratorMetadata.MembersToGenerate.Length];
            var resolvedCount = 0;
            try
            {
                foreach (var member in dataGeneratorMetadata.MembersToGenerate)
                {
                    var type = member switch
                    {
                        PropertyMetadata prop => prop.Type,
                        ParameterMetadata param => param.Type,
                        ClassMetadata cls => cls.Type,
                        MethodMetadata method => method.Type,
                        _ => throw new InvalidOperationException($"Unknown member type: {member.GetType()}")
                    };
                    var created = Create(scope, type);
                    values[resolvedCount++] = created;
                    cleanup.RegisterObject(owner, created);
                }

                return values;
            }
            catch
            {
                if (!scopeOwnsCreatedObjects)
                {
                    // No row reaches normal tracking after a factory failure. Discover the same
                    // initializer graph, including beneath non-disposable roots, without initializing it.
                    var discoverer = new ObjectGraphDiscoverer();
                    var objectsByDepth = new SortedList<int, HashSet<object>>();
                    var refreshed = new HashSet<object>(Helpers.ReferenceEqualityComparer.Instance);
                    void RefreshOwner(object instance)
                    {
                        if (refreshed.Add(instance))
                        {
                            foreach (var existingOwner in ExternalObjectOwnership.FindOwners(instance))
                                existingOwner.CaptureOwnershipFor(instance, [], retainPartialGraphOnError: true);
                        }
                    }
                    for (var i = 0; i < resolvedCount; i++)
                    {
                        if (values[i] is not { } root)
                            continue;

                        RefreshOwner(root);
                        var graph = discoverer.DiscoverTrackableObjectGraph(root, CancellationToken.None, retainPartialGraphOnError: true);
                        foreach (var pair in graph)
                        {
                            if (!objectsByDepth.TryGetValue(pair.Key, out var bucket))
                                objectsByDepth.Add(pair.Key, bucket = new(Helpers.ReferenceEqualityComparer.Instance));
                            bucket.UnionWith(pair.Value);
                            foreach (var instance in pair.Value) RefreshOwner(instance);
                        }
                    }

                    // As in normal tracking, release roots before their nested dependencies.
                    var retained = new HashSet<object>(Helpers.ReferenceEqualityComparer.Instance);
                    foreach (var bucket in objectsByDepth.Values)
                    foreach (var value in bucket)
                    {
                        // Shared roots and nested aliases need only one reference per failed row.
                        if (value is (IDisposable or IAsyncDisposable) && !ReferenceEquals(value, scope)
                            && !ObjectTracker.IsExternallyOwned(value) && retained.Add(value))
                        {
                            cleanup.Register(ObjectTracker.RetainForRowCleanup(value));
                        }
                    }
                }

                throw;
            }
        };
    }

    /// <summary>
    /// Determines whether the scope owns and disposes the objects returned by <see cref="Create"/>.
    /// </summary>
    /// <remarks>
    /// Defaults to true for <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/> scopes.
    /// Override to return false when the scope does not dispose the objects it resolves; TUnit will
    /// track and dispose those objects instead. The scope itself is still disposed when disposable.
    /// This hook is evaluated once per scope; overrides should be cheap and side-effect free.
    /// </remarks>
    protected virtual bool ScopeOwnsCreatedObjects(TScope scope) => scope is IDisposable or IAsyncDisposable;

    /// <summary>
    /// Creates the scope that resolves one test's arguments. A disposable scope is disposed when the
    /// test ends, after its last attempt.
    /// </summary>
    /// <remarks>
    /// A scope that implements <see cref="IDisposable"/> or <see cref="IAsyncDisposable"/> owns the objects
    /// <see cref="Create"/> returns: TUnit disposes the scope and does not dispose those objects itself, so
    /// the scope's disposal must dispose them. Override <see cref="ScopeOwnsCreatedObjects"/> to change this ownership.
    /// </remarks>
    public abstract TScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata);

    /// <summary>
    /// Resolves an argument of type <paramref name="type"/> from <paramref name="scope"/>.
    /// </summary>
    /// <remarks>
    /// When <see cref="ScopeOwnsCreatedObjects"/> returns true, the scope owns what this method returns, including the <see cref="Interfaces.IAsyncInitializer"/>
    /// objects held in their properties: TUnit disposes the scope, and the scope is expected to dispose the
    /// objects it resolved, so TUnit does not dispose them itself. TUnit still initializes them.
    /// When it returns false, TUnit disposes disposable returned objects when the test ends.
    /// </remarks>
    public abstract object? Create(TScope scope, Type type);
}
