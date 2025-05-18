﻿using System.Diagnostics;

namespace TUnit.Core;

/// <summary>
/// Represents the context for a test.
/// </summary>
[DebuggerDisplay("{TestDetails.TestClass.Name}.{TestDetails.TestName}")]
public partial class TestContext : Context
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Gets a service of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the service.</typeparam>
    /// <returns>The service instance.</returns>
    internal T GetService<T>() => (T) _serviceProvider.GetService(typeof(T))!;
    
    internal readonly List<Artifact> Artifacts = [];
    internal readonly List<CancellationToken> LinkedCancellationTokens = [];
    internal readonly TestMetadata OriginalMetadata;

#if NET9_0_OR_GREATER
    /// <summary>
    /// Gets the lock object.
    /// </summary>
    public readonly Lock Lock = new();
#else
    /// <summary>
    /// Gets the lock object.
    /// </summary>
    public readonly object Lock = new();
#endif

    internal bool ReportResult = true;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="TestContext"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider.</param>
    /// <param name="testDetails">The test details.</param>
    /// <param name="originalMetadata">The original metadata.</param>
    internal TestContext(IServiceProvider serviceProvider, TestDetails testDetails, TestMetadata originalMetadata)
    {
        _serviceProvider = serviceProvider;
        OriginalMetadata = originalMetadata;
        TestDetails = testDetails;
        ObjectBag = originalMetadata.TestBuilderContext.ObjectBag;
        Events = originalMetadata.TestBuilderContext.Events;
    }

    /// <summary>
    /// Gets the events associated with the test context.
    /// </summary>
    public TestContextEvents Events { get; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the test is registered.
    /// </summary>
    public bool IsRegistered { get; internal set; }

    /// <summary>
    /// Gets or sets the start time of the test.
    /// </summary>
    public DateTimeOffset? TestStart { get; internal set; }
    
    internal Task? TestTask { get; set; }

    /// <summary>
    /// Gets the details of the test.
    /// </summary>
    public TestDetails TestDetails { get; }

    /// <summary>
    /// Gets or sets the current retry attempt for the test.
    /// </summary>
    public int CurrentRetryAttempt { get; internal set; }

    /// <summary>
    /// Gets the argument display formatters for the test.
    /// </summary>
    public List<ArgumentDisplayFormatter> ArgumentDisplayFormatters { get; } = [];
    
    /// <summary>
    /// Gets the timings for the test.
    /// </summary>
    public List<Timing> Timings { get; } = [];
    
    /// <summary>
    /// Gets the object bag for the test.
    /// </summary>
    public Dictionary<string, object?> ObjectBag { get; }
    
    /// <summary>
    /// Gets or sets the result of the test.
    /// </summary>
    public TestResult? Result { get; internal set; }
    
    /// <summary>
    /// Gets or sets the cancellation token for the test.
    /// </summary>
    public CancellationToken CancellationToken { get; internal set; }
    
    /// <summary>
    /// Gets or sets the internal discovered test.
    /// </summary>
    internal DiscoveredTest InternalDiscoveredTest { get; set; } = null!;

    /// <summary>
    /// Suppresses reporting the result.
    /// </summary>
    public void SuppressReportingResult()
    {
        ReportResult = false;
    }
    
    /// <summary>
    /// Adds an artifact to the test context.
    /// </summary>
    /// <param name="artifact">The artifact to add.</param>
    public void AddArtifact(Artifact artifact)
    {
        Artifacts.Add(artifact);
    }
    
    /// <summary>
    /// Gets or sets the reason for skipping the test.
    /// </summary>
    internal string? SkipReason { get; set; }
    
    /// <summary>
    /// Gets or sets the event objects.
    /// </summary>
    internal object?[]? EventObjects { get; set; }

    internal bool RunOnTestDiscovery { get; set; }
}