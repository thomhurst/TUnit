using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext : 
    Context, 
    IDisposable,
    ITestRegisteredEventReceiver,
    ITestStartEventReceiver,
    ITestEndEventReceiver,
    ILastTestInClassEventReceiver,
    ILastTestInAssemblyEventReceiver,
    ILastTestInTestSessionEventReceiver,
    ITestRetryEventReceiver
{
    private readonly IServiceProvider _serviceProvider;

    internal T GetService<T>() => (T) _serviceProvider.GetService(typeof(T))!;
    
    internal readonly List<Artifact> Artifacts = [];
    internal readonly List<CancellationToken> LinkedCancellationTokens = [];
    internal readonly TestMetadata OriginalMetadata;
    
#if NET9_0_OR_GREATER
    public readonly Lock Lock = new();
#else
    public readonly object Lock = new();
#endif
    
    internal bool ReportResult = true;
    
    internal TestContext(IServiceProvider serviceProvider, TestDetails testDetails, TestMetadata originalMetadata)
    {
        _serviceProvider = serviceProvider;
        OriginalMetadata = originalMetadata;
        TestDetails = testDetails;
        ObjectBag = originalMetadata.ObjectBag;
    }
    
    public DateTimeOffset? TestStart { get; internal set; }
    
    internal Task? TestTask { get; set; }

    public TestDetails TestDetails { get; }

    public int CurrentRetryAttempt { get; internal set; }

    public List<ArgumentDisplayFormatter> ArgumentDisplayFormatters { get; } = [];
    
    public List<Timing> Timings { get; } = [];
    public Dictionary<string, object?> ObjectBag { get; }
    
    public TestResult? Result { get; internal set; }
    internal DiscoveredTest InternalDiscoveredTest { get; set; } = null!;

    public void SuppressReportingResult()
    {
        ReportResult = false;
    }
    
    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }
    
    internal string? SkipReason { get; set; }

    public EventHandler? OnDispose { get; set; }
    public void Dispose()
    {
        OnDispose?.Invoke(this, EventArgs.Empty);
    }
    
    public EventHandler? OnTestRegistered { get; set; }
    public EventHandler? OnTestStart { get; set; }
    public EventHandler? OnTestEnd { get; set; }
    public EventHandler? OnLastTestInClass { get; set; }
    public EventHandler? OnLastTestInAssembly { get; set; }
    public EventHandler? OnLastTestInTestSession { get; set; }
    public EventHandler? OnRetry { get; set; }
    
    ValueTask ITestRegisteredEventReceiver.OnTestRegistered(TestRegisteredContext context)
    {
        OnTestRegistered?.Invoke(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }

    ValueTask ITestStartEventReceiver.OnTestStart(BeforeTestContext beforeTestContext)
    {
        OnTestStart?.Invoke(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }

    ValueTask ITestEndEventReceiver.OnTestEnd(TestContext testContext)
    {
        OnTestEnd?.Invoke(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }

    public ValueTask IfLastTestInClass(ClassHookContext context, TestContext testContext)
    {
        OnLastTestInClass?.Invoke(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }

    public ValueTask IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext)
    {
        OnLastTestInAssembly?.Invoke(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }

    public ValueTask IfLastTestInTestSession(TestSessionContext current, TestContext testContext)
    {
        OnLastTestInTestSession?.Invoke(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }

    public ValueTask OnTestRetry(TestContext testContext, int retryAttempt)
    {
        OnRetry?.Invoke(this, EventArgs.Empty);
        return ValueTask.CompletedTask;
    }
}