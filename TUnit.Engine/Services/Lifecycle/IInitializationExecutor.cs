using System.Collections.Concurrent;
using TUnit.Core;

namespace TUnit.Engine.Services.Lifecycle;

/// <summary>
/// Consolidated interface for object initialization in TUnit.
/// Replaces the fragmented initialization logic in:
/// - DataSourceInitializer
/// - PropertyInjectionService
/// - PropertyInitializationOrchestrator
/// - ObjectRegistrationService
/// </summary>
public interface IInitializationExecutor
{
    /// <summary>
    /// Ensures an object is fully initialized for test execution.
    /// This includes property injection and IAsyncInitializer calls.
    /// Idempotent - calling multiple times on the same object is safe.
    /// </summary>
    /// <typeparam name="T">The type of object to initialize.</typeparam>
    /// <param name="instance">The object to initialize.</param>
    /// <param name="context">The initialization context with required parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The initialized object.</returns>
    ValueTask<T> EnsureFullyInitializedAsync<T>(
        T instance,
        InitializationContext context,
        CancellationToken cancellationToken = default) where T : notnull;

    /// <summary>
    /// Registers an object during test discovery without calling IAsyncInitializer.
    /// This is used during the registration phase to set up property injection and tracking.
    /// IAsyncInitializer is deferred to execution phase.
    /// </summary>
    /// <typeparam name="T">The type of object to register.</typeparam>
    /// <param name="instance">The object to register.</param>
    /// <param name="context">The initialization context with required parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The registered object.</returns>
    ValueTask<T> EnsureRegisteredAsync<T>(
        T instance,
        InitializationContext context,
        CancellationToken cancellationToken = default) where T : notnull;

    /// <summary>
    /// Tracks an object for reference counting and disposal.
    /// </summary>
    /// <param name="instance">The object to track.</param>
    void TrackForDisposal(object instance);

    /// <summary>
    /// Releases an object reference. When reference count reaches 0, the object is disposed.
    /// </summary>
    /// <param name="instance">The object to release.</param>
    ValueTask ReleaseAsync(object instance);
}

/// <summary>
/// Context for initialization operations.
/// Consolidates the various parameters that were scattered across different method signatures.
/// </summary>
public sealed record InitializationContext
{
    /// <summary>
    /// Shared object bag for the test context.
    /// Used for storing keyed data that can be retrieved by data sources.
    /// </summary>
    public required ConcurrentDictionary<string, object?> ObjectBag { get; init; }

    /// <summary>
    /// Method metadata for the test.
    /// Can be null for non-test-specific initialization.
    /// </summary>
    public MethodMetadata? MethodMetadata { get; init; }

    /// <summary>
    /// Events for disposal tracking.
    /// Must be unique per test permutation for proper cleanup.
    /// </summary>
    public TestContextEvents? Events { get; init; }

    /// <summary>
    /// Pre-resolved property values from registration phase.
    /// When provided, these values are used instead of creating new instances.
    /// This is the key mechanism for avoiding double initialization.
    /// </summary>
    public IDictionary<string, object?>? PreResolvedValues { get; init; }

    /// <summary>
    /// Creates a context with only the object bag (for simple cases).
    /// </summary>
    public static InitializationContext Create(ConcurrentDictionary<string, object?> objectBag)
        => new() { ObjectBag = objectBag };

    /// <summary>
    /// Creates a context from a TestContext.
    /// </summary>
    public static InitializationContext FromTestContext(TestContext testContext)
        => new()
        {
            ObjectBag = testContext.StateBag.Items,
            MethodMetadata = testContext.Metadata.TestDetails.MethodMetadata,
            Events = testContext.InternalEvents,
            PreResolvedValues = testContext.Metadata.TestDetails.TestClassInjectedPropertyArguments
        };
}
