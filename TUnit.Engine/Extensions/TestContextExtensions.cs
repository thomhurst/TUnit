using System.Diagnostics;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.Engine.Utilities;

namespace TUnit.Engine.Extensions;

internal static class TestContextExtensions
{
    /// <summary>
    /// Ensures all event receiver caches are populated. Iterates through eligible objects once
    /// and categorizes them by type in a single pass.
    /// </summary>
    /// <remarks>
    /// Class instances change in these scenarios:
    /// - Test retries: A new instance is created for each retry attempt
    /// - Keyed test instances: Different data combinations may use different instances
    /// When this happens, eligible event objects may include the new instance (if it implements
    /// event receiver interfaces), so all caches must be invalidated and rebuilt.
    /// </remarks>
    public static void EnsureEventReceiversCached(this TestContext testContext)
    {
        var currentClassInstance = testContext.Metadata.TestDetails.ClassInstance;

        // Fast path: caches populated and class instance unchanged since last build.
        if (testContext.EventReceiversBuilt &&
            ReferenceEquals(testContext.CachedClassInstance, currentClassInstance))
        {
            return;
        }

        // Invalidate stale caches if class instance changed
        if (testContext.CachedClassInstance != null &&
            !ReferenceEquals(testContext.CachedClassInstance, currentClassInstance))
        {
            testContext.InvalidateEventReceiverCaches();
        }

        // Build caches - get eligible objects first
        var eligibleObjects = BuildEligibleEventObjects(testContext);
        testContext.CachedEligibleEventObjects = eligibleObjects;

        // Single pass: categorize each object by interface type
#if NET
        List<ITestStartEventReceiver>? startReceiversEarly = null;
        List<ITestStartEventReceiver>? startReceiversLate = null;
        List<ITestEndEventReceiver>? endReceiversEarly = null;
        List<ITestEndEventReceiver>? endReceiversLate = null;
#else
        List<ITestStartEventReceiver>? startReceivers = null;
        List<ITestEndEventReceiver>? endReceivers = null;
#endif
        List<ITestSkippedEventReceiver>? skippedReceivers = null;
        List<ITestDiscoveryEventReceiver>? discoveryReceivers = null;
        List<ITestRegisteredEventReceiver>? registeredReceivers = null;

        foreach (var obj in eligibleObjects)
        {
            // Check each interface - an object can implement multiple
            if (obj is ITestStartEventReceiver startReceiver)
            {
#if NET
                if (startReceiver.Stage == EventReceiverStage.Early)
                {
                    startReceiversEarly ??= [];
                    startReceiversEarly.Add(startReceiver);
                }
                else
                {
                    startReceiversLate ??= [];
                    startReceiversLate.Add(startReceiver);
                }
#else
                startReceivers ??= [];
                startReceivers.Add(startReceiver);
#endif
            }

            if (obj is ITestEndEventReceiver endReceiver)
            {
#if NET
                if (endReceiver.Stage == EventReceiverStage.Early)
                {
                    endReceiversEarly ??= [];
                    endReceiversEarly.Add(endReceiver);
                }
                else
                {
                    endReceiversLate ??= [];
                    endReceiversLate.Add(endReceiver);
                }
#else
                endReceivers ??= [];
                endReceivers.Add(endReceiver);
#endif
            }

            if (obj is ITestSkippedEventReceiver skippedReceiver)
            {
                skippedReceivers ??= [];
                skippedReceivers.Add(skippedReceiver);
            }

            if (obj is ITestDiscoveryEventReceiver discoveryReceiver)
            {
                discoveryReceivers ??= [];
                discoveryReceivers.Add(discoveryReceiver);
            }

            if (obj is ITestRegisteredEventReceiver registeredReceiver)
            {
                registeredReceivers ??= [];
                registeredReceivers.Add(registeredReceiver);
            }
        }

        // Sort and apply scoped filtering, then cache
#if NET
        testContext.CachedTestStartReceiversEarly = SortAndFilter(startReceiversEarly);
        testContext.CachedTestStartReceiversLate = SortAndFilter(startReceiversLate);
        testContext.CachedTestEndReceiversEarly = SortAndFilter(endReceiversEarly);
        testContext.CachedTestEndReceiversLate = SortAndFilter(endReceiversLate);
#else
        testContext.CachedTestStartReceivers = SortAndFilter(startReceivers);
        testContext.CachedTestEndReceivers = SortAndFilter(endReceivers);
#endif
        testContext.CachedTestSkippedReceivers = SortAndFilter(skippedReceivers);
        testContext.CachedTestDiscoveryReceivers = SortAndFilter(discoveryReceivers);
        testContext.CachedTestRegisteredReceivers = SortAndFilter(registeredReceivers);

        // Update cached class instance last
        testContext.CachedClassInstance = currentClassInstance;
        testContext.EventReceiversBuilt = true;
    }

    private static T[] SortAndFilter<T>(List<T>? receivers) where T : class, IEventReceiver
    {
        if (receivers == null || receivers.Count == 0)
        {
            return [];
        }

        // Sort by Order
        receivers.Sort(static (a, b) => a.Order.CompareTo(b.Order));

        // Apply scoped attribute filtering - FilterScopedAttributes already returns T[]
        return ScopedAttributeFilter.FilterScopedAttributes(receivers);
    }

    public static IEnumerable<object> GetEligibleEventObjects(this TestContext testContext)
    {
        // Use EnsureEventReceiversCached which builds eligible objects as part of cache initialization
        testContext.EnsureEventReceiversCached();
        return testContext.CachedEligibleEventObjects!;
    }

    private static object[] BuildEligibleEventObjects(TestContext testContext)
    {
        var details = testContext.Metadata.TestDetails;
        var testClassArgs = details.TestClassArguments;
        var attributes = details.GetAllAttributes();
        var testMethodArgs = details.TestMethodArguments;
        var injectedProps = details.TestClassInjectedPropertyArguments;

        // Count non-null items first to allocate exact size
        var count = CountNonNull(testContext.ClassConstructor)
                  + CountNonNull(testContext.Events)
                  + CountNonNullInArray(testClassArgs)
                  + CountNonNull(details.ClassInstance)
                  + attributes.Count  // Attributes are never null
                  + CountNonNullInArray(testMethodArgs)
                  + CountNonNullValues(injectedProps);

        if (count == 0)
        {
            return [];
        }

        // Single allocation with exact size
        var result = new object[count];
        var index = 0;

        // Add items, skipping nulls
        if (testContext.ClassConstructor is { } constructor)
        {
            result[index++] = constructor;
        }

        if (testContext.Events is { } events)
        {
            result[index++] = events;
        }

        foreach (var arg in testClassArgs)
        {
            if (arg is { } nonNullArg)
            {
                result[index++] = nonNullArg;
            }
        }

        if (details.ClassInstance is { } classInstance)
        {
            result[index++] = classInstance;
        }

        foreach (var attr in attributes)
        {
            result[index++] = attr;
        }

        foreach (var arg in testMethodArgs)
        {
            if (arg is { } nonNullArg)
            {
                result[index++] = nonNullArg;
            }
        }

        foreach (var prop in injectedProps)
        {
            if (prop.Value is { } value)
            {
                result[index++] = value;
            }
        }

        return result;
    }

    private static int CountNonNull(object? obj) => obj != null ? 1 : 0;

    private static int CountNonNullInArray(object?[] array)
    {
        var count = 0;
        foreach (var item in array)
        {
            if (item != null)
            {
                count++;
            }
        }
        return count;
    }

    private static int CountNonNullValues(IDictionary<string, object?> props)
    {
        var count = 0;
        foreach (var prop in props)
        {
            if (prop.Value != null)
            {
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Gets pre-computed test start receivers (filtered, sorted, scoped-attribute filtered).
    /// Assumes <see cref="EnsureEventReceiversCached"/> has been called by the lifecycle
    /// coordinator — hot-path callers (per-test start/end/skipped) skip the guard.
    /// </summary>
#if NET
    public static ITestStartEventReceiver[] GetTestStartReceivers(this TestContext testContext, EventReceiverStage stage)
    {
        Debug.Assert(testContext.EventReceiversBuilt, "EnsureEventReceiversCached must run before GetTestStartReceivers — caller is on the hot path and skips the guard.");
        return stage == EventReceiverStage.Early
            ? testContext.CachedTestStartReceiversEarly!
            : testContext.CachedTestStartReceiversLate!;
    }
#else
    public static ITestStartEventReceiver[] GetTestStartReceivers(this TestContext testContext)
    {
        Debug.Assert(testContext.EventReceiversBuilt, "EnsureEventReceiversCached must run before GetTestStartReceivers — caller is on the hot path and skips the guard.");
        return testContext.CachedTestStartReceivers!;
    }
#endif

    /// <summary>
    /// Gets pre-computed test end receivers (filtered, sorted, scoped-attribute filtered).
    /// </summary>
#if NET
    public static ITestEndEventReceiver[] GetTestEndReceivers(this TestContext testContext, EventReceiverStage stage)
    {
        Debug.Assert(testContext.EventReceiversBuilt, "EnsureEventReceiversCached must run before GetTestEndReceivers — caller is on the hot path and skips the guard.");
        return stage == EventReceiverStage.Early
            ? testContext.CachedTestEndReceiversEarly!
            : testContext.CachedTestEndReceiversLate!;
    }
#else
    public static ITestEndEventReceiver[] GetTestEndReceivers(this TestContext testContext)
    {
        Debug.Assert(testContext.EventReceiversBuilt, "EnsureEventReceiversCached must run before GetTestEndReceivers — caller is on the hot path and skips the guard.");
        return testContext.CachedTestEndReceivers!;
    }
#endif

    /// <summary>
    /// Gets pre-computed test skipped receivers (filtered, sorted, scoped-attribute filtered).
    /// </summary>
    public static ITestSkippedEventReceiver[] GetTestSkippedReceivers(this TestContext testContext)
    {
        Debug.Assert(testContext.EventReceiversBuilt, "EnsureEventReceiversCached must run before GetTestSkippedReceivers — caller is on the hot path and skips the guard.");
        return testContext.CachedTestSkippedReceivers!;
    }

    /// <summary>
    /// Gets pre-computed test discovery receivers (filtered, sorted, scoped-attribute filtered).
    /// Called from discovery / out-of-lifecycle paths, so the guard still builds on demand.
    /// </summary>
    public static ITestDiscoveryEventReceiver[] GetTestDiscoveryReceivers(this TestContext testContext)
    {
        testContext.EnsureEventReceiversCached();
        return testContext.CachedTestDiscoveryReceivers!;
    }

    /// <summary>
    /// Gets pre-computed test registered receivers (filtered, sorted, scoped-attribute filtered).
    /// Called from discovery / out-of-lifecycle paths, so the guard still builds on demand.
    /// </summary>
    public static ITestRegisteredEventReceiver[] GetTestRegisteredReceivers(this TestContext testContext)
    {
        testContext.EnsureEventReceiversCached();
        return testContext.CachedTestRegisteredReceivers!;
    }
}
