using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

/// <summary>
/// Represents the context for building tests.
/// </summary>
public record TestBuilderContext
{
    public Guid Id { get; } = Guid.NewGuid();
    public Dictionary<string, object?> ObjectBag { get; } = [];
    public TestContextEvents Events { get; } = new();

    public List<IDataAttribute> DataAttributes { get; } = [];

    private List<object?>? _initializedObjects;
    public List<object?> InitializedObjects => _initializedObjects ??= [];

    public async Task InitializeAsync(object? obj)
    {
        foreach (var initializedObject in _initializedObjects ?? [])
        {
            await ObjectInitializer.InitializeAsync(initializedObject);
        }
    }
}

// A reference for us to access the context which might change within loops by calling the setter
/// <summary>
/// Provides access to the current <see cref="TestBuilderContext"/>.
/// </summary>
public class TestBuilderContextAccessor(TestBuilderContext context)
{
    public TestBuilderContext Current { get; set; } = context;
}
