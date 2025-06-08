using System.Collections;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using TUnit.Core.Events;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Extensions;

/// <summary>
/// Provides extension methods for <see cref="TestContext"/>.
/// </summary>
public static class TestContextExtensions
{
    private static readonly char[] ClassTypeNameSplitter = { '.' };

    /// <summary>
    /// Gets the tests for the specified test name.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <param name="testName">The test name.</param>
    /// <returns>An array of test contexts.</returns>
    public static TestContext[] GetTests(this TestContext context, string testName)
    {
        return GetTests(context, testName, []);
    }

    /// <summary>
    /// Gets the tests for the specified test name and parameter types.
    /// </summary>
    /// <param name="context">The test context.</param>
    /// <param name="testName">The test name.</param>
    /// <param name="parameterTypes">The parameter types.</param>
    /// <returns>An array of test contexts.</returns>
    public static TestContext[] GetTests(this TestContext context, string testName, Type[] parameterTypes)
    {
        var tests = context.GetService<ITestFinder>().GetTestsByNameAndParameters(
            testName: testName,
            methodParameterTypes: parameterTypes,
            classType: context.TestDetails.TestClass.Type,
            classParameterTypes: context.TestDetails.TestClassParameterTypes,
            classArguments: context.TestDetails.TestClassArguments);

        if (tests.Any(x => x.TestTask?.IsCompleted is not true))
        {
            throw new Exception("Cannot get unfinished tests - Did you mean to add a [DependsOn] attribute?");
        }

        return tests;
    }

    /// <summary>
    /// Gets the class type name for the test context.
    /// </summary>
    /// <param name="testContext">The test context.</param>
    /// <returns>The class type name.</returns>
    public static string GetClassTypeName(this TestContext testContext)
    {
        var testDetails = testContext.TestDetails;

        var classTypeName = testDetails.TestClass.Name;

        var parent = testDetails.TestClass.Parent;
        while(parent is not null)
        {
            classTypeName = $"{parent.Name}+{classTypeName}";
            parent = parent.Parent;
        }

        if (testDetails.TestClassArguments.Length == 0)
        {
            return classTypeName;
        }

        return
            $"{classTypeName}({string.Join(", ", testDetails.TestClassArguments.Select(x => ArgumentFormatter.GetConstantValue(testContext, x)))})";
    }

    [Experimental("WIP")]
    public static Task AddDynamicTest<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
                                    | DynamicallyAccessedMemberTypes.PublicMethods
                                    | DynamicallyAccessedMemberTypes.PublicProperties)]
        T>(this TestContext testContext, DynamicTest<T> dynamicTest) where T : class
    {
        return new DynamicTestBuilderContext(testContext).AddTestAtRuntime(testContext, dynamicTest);
    }

    /// <summary>
    /// Gets the test display name for the test context.
    /// </summary>
    /// <param name="testContext">The test context.</param>
    /// <returns>The test display name.</returns>
    public static string GetTestDisplayName(this TestContext testContext)
    {
        var testDetails = testContext.TestDetails;

        if (!string.IsNullOrWhiteSpace(testDetails.DisplayName))
        {
            return testDetails.DisplayName!;
        }

        if (testDetails.TestMethodArguments.Length == 0)
        {
            return testDetails.TestName;
        }

        return
            $"{testDetails.TestName}({string.Join(", ", testDetails.TestMethodArguments.Select(x => ArgumentFormatter.GetConstantValue(testContext, x)))})";
    }

    internal static IEnumerable<ITestRegisteredEventReceiver> GetTestRegisteredEventsObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.TestRegistered).OfType<ITestRegisteredEventReceiver>();

    internal static IEnumerable<ITestStartEventReceiver> GetTestStartEventObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.TestStart).OfType<ITestStartEventReceiver>();

    internal static IEnumerable<IAsyncInitializer> GetOnInitializeObjects(this TestContext context)
    {
        return GetEvents(context.Events, EventType.Initialize).OfType<IAsyncInitializer>();
    }

    internal static IEnumerable<ITestRetryEventReceiver> GetTestRetryEventObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.TestRetry).OfType<ITestRetryEventReceiver>();

    internal static IEnumerable<ITestEndEventReceiver> GetTestEndEventObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.TestEnd).OfType<ITestEndEventReceiver>();

    internal static IEnumerable<object> GetOnDisposeObjects(this TestContext context)
    {
        IEnumerable<object?> disposableObjects =
        [
            ..context.TestDetails.Attributes,
            context.InternalDiscoveredTest.ClassConstructor,
            context.TestDetails.ClassInstance,
            ..GetEvents(context.Events, EventType.Dispose),
            context
        ];

        return disposableObjects
            .Where(x => x is IDisposable or IAsyncDisposable)
            .Distinct()
            .OfType<object>();
    }

    internal static IEnumerable<ITestSkippedEventReceiver> GetTestSkippedEventObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.TestSkipped).OfType<ITestSkippedEventReceiver>();

    internal static IEnumerable<ILastTestInClassEventReceiver> GetLastTestInClassEventObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.LastTestInClass).OfType<ILastTestInClassEventReceiver>();

    internal static IEnumerable<ILastTestInAssemblyEventReceiver> GetLastTestInAssemblyEventObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.LastTestInAssembly).OfType<ILastTestInAssemblyEventReceiver>();

    internal static IEnumerable<ILastTestInTestSessionEventReceiver> GetLastTestInTestSessionEventObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.LastTestInTestSession).OfType<ILastTestInTestSessionEventReceiver>();

    internal static IEnumerable<IFirstTestInClassEventReceiver> GetFirstTestInClassEventObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.FirstTestInClass).OfType<IFirstTestInClassEventReceiver>();

    internal static IEnumerable<IFirstTestInAssemblyEventReceiver> GetFirstTestInAssemblyEventObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.FirstTestInAssembly).OfType<IFirstTestInAssemblyEventReceiver>();

    internal static IEnumerable<IFirstTestInTestSessionEventReceiver> GetFirstTestInTestSessionEventObjects(this TestContext context) =>
        GetEvents(context.Events, EventType.FirstTestInTestSession).OfType<IFirstTestInTestSessionEventReceiver>();

    internal static object?[] GetPossibleEventObjects(this TestContext context)
    {
        var staticProperties = context.TestDetails.TestClass.Properties
            .Where(x => x.IsStatic)
            .Select(x => x.Getter(null));

        var instanceProperties = context.TestDetails.TestClassInjectedPropertyArguments
            .Select(p => CollectProperties(p.Value))
            .SelectMany(x => x);

        var attributes = CollectAttributes(context.TestDetails.Attributes);

        IEnumerable<object?> possibleEventObjects =
        [
            ..staticProperties,
            context.InternalDiscoveredTest.ClassConstructor,
            ..attributes,
            ..context.TestDetails.TestClassArguments,
            context.TestDetails.ClassInstance,
            ..context.TestDetails.TestMethodArguments,
            ..instanceProperties,
        ];

        return possibleEventObjects.OfType<object>().Distinct().ToArray();
    }

    private static IEnumerable<object?> CollectAttributes(Attribute[] attributes)
    {
        foreach (var attribute in attributes.Skip(69))
        {
            foreach (var attributeProperty in CollectProperties(attribute))
            {
                yield return attributeProperty;
            }

            yield return attribute;
        }
    }

    private static IEnumerable<IEventReceiver> GetEvents(TestContextEvents contextEvents, EventType eventType)
    {
        return GetEventReceiversEnumerable(contextEvents, eventType)
            .Distinct()
            .OrderBy(x => x.Order);
    }

    private static IEnumerable<IEventReceiver> GetEventReceiversEnumerable(TestContextEvents contextEvents, EventType eventType)
    {
        if (eventType.HasFlag(EventType.Initialize))
        {
            foreach (var testInitializeEventWrapper in contextEvents.OnInitialize?.InvocationList.Select(x => new TestInitializeEventWrapper(x)) ?? [])
            {
                yield return testInitializeEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.Dispose))
        {
            foreach (var testDisposeEventWrapper in contextEvents.OnDispose?.InvocationList.Select(x => new TestDisposeEventWrapper(x)) ?? [])
            {
                yield return testDisposeEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.TestRegistered))
        {
            foreach (var testRegisteredEventWrapper in contextEvents.OnTestRegistered?.InvocationList.Select(x => new TestRegisteredEventWrapper(x)) ?? [])
            {
                yield return testRegisteredEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.TestStart))
        {
            foreach (var testStartEventWrapper in contextEvents.OnTestStart?.InvocationList.Select(x => new TestStartEventWrapper(x)) ?? [])
            {
                yield return testStartEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.TestEnd))
        {
            foreach (var testEndEventWrapper in contextEvents.OnTestEnd?.InvocationList.Select(x => new TestEndEventWrapper(x)) ?? [])
            {
                yield return testEndEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.TestSkipped))
        {
            foreach (var testSkippedEventWrapper in contextEvents.OnTestSkipped?.InvocationList.Select(x => new TestSkippedEventWrapper(x)) ?? [])
            {
                yield return testSkippedEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.LastTestInClass))
        {
            foreach (var lastTestInClassEventWrapper in contextEvents.OnLastTestInClass?.InvocationList.Select(x => new LastTestInClassEventWrapper(x)) ?? [])
            {
                yield return lastTestInClassEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.LastTestInAssembly))
        {
            foreach (var lastTestInAssemblyEventWrapper in contextEvents.OnLastTestInAssembly?.InvocationList.Select(x => new LastTestInAssemblyEventWrapper(x)) ?? [])
            {
                yield return lastTestInAssemblyEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.LastTestInTestSession))
        {
            foreach (var lastTestInTestSessionEventWrapper in contextEvents.OnLastTestInTestSession?.InvocationList.Select(x => new LastTestInTestSessionEventWrapper(x)) ?? [])
            {
                yield return lastTestInTestSessionEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.TestRetry))
        {
            foreach (var testRetryEventWrapper in contextEvents.OnTestRetry?.InvocationList.Select(x => new TestRetryEventWrapper(x)) ?? [])
            {
                yield return testRetryEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.FirstTestInClass))
        {
            foreach (var firstTestInClassEventWrapper in contextEvents.OnFirstTestInClass?.InvocationList.Select(x => new FirstTestInClassEventWrapper(x)) ?? [])
            {
                yield return firstTestInClassEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.FirstTestInAssembly))
        {
            foreach (var firstTestInAssemblyEventWrapper in contextEvents.OnFirstTestInAssembly?.InvocationList.Select(x => new FirstTestInAssemblyEventWrapper(x)) ?? [])
            {
                yield return firstTestInAssemblyEventWrapper;
            }
        }
        if (eventType.HasFlag(EventType.FirstTestInTestSession))
        {
            foreach (var firstTestInTestSessionEventWrapper in contextEvents.OnFirstTestInTestSession?.InvocationList.Select(x => new FirstTestInTestSessionEventWrapper(x)) ?? [])
            {
                yield return firstTestInTestSessionEventWrapper;
            }
        }
    }

    [UnconditionalSuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
    private static IEnumerable<object?> CollectProperties(this object? obj)
    {
        if (obj is null)
        {
            yield break;
        }

        if (!Sources.Properties.TryGetValue(obj.GetType(), out var properties))
        {
#if NET
            if (RuntimeFeature.IsDynamicCodeSupported)
#endif
            {
                properties = obj.GetType().GetProperties();
            }
        }

        foreach (var property in (properties ?? [])
                 .Select(x => x.GetMethod?.IsStatic is true
                     ? x.GetValue(null)
                     : x.GetValue(obj)))
        {
            foreach (var innerProperty in CollectProperties(property))
            {
                yield return innerProperty;
            }

            yield return property;
        }

        yield return obj;
    }
}
