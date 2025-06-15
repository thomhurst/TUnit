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

    public void RegisterForInitialization(object? obj)
    {
        Events.OnInitialize += async (sender, args) =>
        {
            await ObjectInitializer.InitializeAsync(obj);
        };
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
