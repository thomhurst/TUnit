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
    public Dictionary<string, object?> ObjectBag { get; set; } = [];
    public TestContextEvents Events { get; set; } = new();

    public IDataSourceAttribute? DataSourceAttribute { get; set; }

    /// <summary>
    /// Gets the test method information, if available during source generation.
    /// </summary>
    public required MethodMetadata TestMetadata { get; init; }

    public void RegisterForInitialization(object? obj)
    {
        Events.OnInitialize += async (sender, args) =>
        {
            await ObjectInitializer.InitializeAsync(obj);
        };
    }

    internal static TestBuilderContext FromTestContext(TestContext testContext, IDataSourceAttribute? dataSourceAttribute)
    {
        return new TestBuilderContext
        {
            Events = testContext.Events, TestMetadata = testContext.TestDetails.MethodMetadata, DataSourceAttribute = dataSourceAttribute, ObjectBag = testContext.ObjectBag,
        };
    }
}

/// <summary>
/// Provides access to the current <see cref="TestBuilderContext"/>.
/// </summary>
public class TestBuilderContextAccessor(TestBuilderContext context)
{
    public TestBuilderContext Current { get; set; } = context;
}
