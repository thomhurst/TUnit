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

            // Register before resolving so a failed factory still releases its scope.
            var cleanup = dataGeneratorMetadata.TestBuilderContext.Current.Events.GetOrCreateDataRowScopes();
            var owner = cleanup.Register(scope);

            // Custom scopes can leave disposal of their resolved objects to the tracker.
            var scopeOwnsCreatedObjects = ScopeOwnsCreatedObjects(scope);

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

                    if (scopeOwnsCreatedObjects)
                    {
                        // The scope owns what it resolves; the tracker must not dispose it again.
                        ObjectTracker.MarkExternallyOwned(created);
                    }
                }

                return values;
            }
            catch
            {
                if (!scopeOwnsCreatedObjects)
                {
                    // No row reaches the tracker after a factory failure. Retain its resolved
                    // services for asynchronous cleanup, without replacing the resolution error.
                    var retained = new HashSet<object>(Helpers.ReferenceEqualityComparer.Instance);
                    for (var i = 0; i < resolvedCount; i++)
                    {
                        if (values[i] is { } value && !ReferenceEquals(value, scope)
                            && !ObjectTracker.IsExternallyOwned(value) && retained.Add(value))
                        {
                            if (value is IDisposable or IAsyncDisposable)
                            {
                                cleanup.Register(ObjectTracker.RetainForRowCleanup(value));
                            }
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
