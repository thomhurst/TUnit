namespace TUnit.Core;

/// <summary>
/// Represents the context for building tests.
/// </summary>
public record TestBuilderContext
{
    private static readonly AsyncLocal<TestBuilderContext?> BuilderContexts = new();

    /// <summary>
    /// Gets the current test builder context.
    /// </summary>
    public static TestBuilderContext? Current
    {
        get => BuilderContexts.Value;
        internal set => BuilderContexts.Value = value;
    }

    public Guid Id { get; } = Guid.NewGuid();
    public Dictionary<string, object?> ObjectBag { get; } = [];
    public TestContextEvents Events { get; } = new();

    public List<IDataAttribute> DataAttributes { get; } = [];

    /// <summary>
    /// Gets the test method name, if available.
    /// </summary>
    public string? TestMethodName { get; init; }

    /// <summary>
    /// Gets the test class information, if available during source generation.
    /// </summary>
    public ClassMetadata? ClassInformation { get; init; }

    /// <summary>
    /// Gets the test method information, if available during source generation.
    /// </summary>
    public MethodMetadata? MethodInformation { get; init; }

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
