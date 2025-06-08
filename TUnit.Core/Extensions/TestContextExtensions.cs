using System.Collections;
using System.Data;
using System.Diagnostics.CodeAnalysis;
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
        GetPossibleEventObjects(context, EventType.TestRegistered).OfType<ITestRegisteredEventReceiver>();

    internal static IEnumerable<ITestStartEventReceiver> GetTestStartEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.TestStart).OfType<ITestStartEventReceiver>();

    internal static IEnumerable<IAsyncInitializer> GetOnInitializeObjects(this TestContext context) =>
        GetEventObjects(context, EventType.Initialize).OfType<IAsyncInitializer>();

    internal static IEnumerable<ITestRetryEventReceiver> GetTestRetryEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.TestRetry).OfType<ITestRetryEventReceiver>();

    internal static IEnumerable<ITestEndEventReceiver> GetTestEndEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.TestEnd).OfType<ITestEndEventReceiver>();

    internal static IEnumerable<object> GetOnDisposeObjects(this TestContext context)
    {
        IEnumerable<object?> disposableObjects =
        [
            ..context.TestDetails.Attributes,
            context.InternalDiscoveredTest.ClassConstructor,
            context.TestDetails.ClassInstance,
            context.Events,
            context
        ];

        return disposableObjects
            .Where(x => x is IDisposable or IAsyncDisposable)
            .OfType<object>();
    }

    internal static IEnumerable<ITestSkippedEventReceiver> GetTestSkippedEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.TestSkipped).OfType<ITestSkippedEventReceiver>();

    internal static IEnumerable<ILastTestInClassEventReceiver> GetLastTestInClassEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.LastTestInClass).OfType<ILastTestInClassEventReceiver>();

    internal static IEnumerable<ILastTestInAssemblyEventReceiver> GetLastTestInAssemblyEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.LastTestInAssembly).OfType<ILastTestInAssemblyEventReceiver>();

    internal static IEnumerable<ILastTestInTestSessionEventReceiver> GetLastTestInTestSessionEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.LastTestInTestSession).OfType<ILastTestInTestSessionEventReceiver>();

    internal static IEnumerable<IFirstTestInClassEventReceiver> GetFirstTestInClassEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.FirstTestInClass).OfType<IFirstTestInClassEventReceiver>();

    internal static IEnumerable<IFirstTestInAssemblyEventReceiver> GetFirstTestInAssemblyEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.FirstTestInAssembly).OfType<IFirstTestInAssemblyEventReceiver>();

    internal static IEnumerable<IFirstTestInTestSessionEventReceiver> GetFirstTestInTestSessionEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.FirstTestInTestSession).OfType<IFirstTestInTestSessionEventReceiver>();

    private static IEnumerable<object?> GetPossibleEventObjects(this TestContext context, EventType eventType)
    {
        return GetEventObjects(context, eventType).OfType<IEventReceiver>().OrderBy(x => x.Order);
    }

    private static object?[] GetEventObjects(TestContext context, EventType eventType)
    {
        return
        [
            context.InternalDiscoveredTest.ClassConstructor,
            ..context.TestDetails.Attributes,
            ..GetEvents(context.Events, eventType),
            context.TestDetails.TestClassArguments,
            context.TestDetails.ClassInstance,
            context.TestDetails.TestMethodArguments,
            ..context.TestDetails.TestClassInjectedPropertyArguments.Select(p => CollectProperties(p.Value)),
        ];
    }

    private static IEnumerable<IEventReceiver> GetEvents(TestContextEvents contextEvents, EventType eventType)
    {
        IEnumerable<IAsyncEventInvocation> events = eventType switch
        {
            EventType.Initialize => contextEvents.OnInitialize?.InvocationList ?? [],
            EventType.Dispose => contextEvents.OnDispose?.InvocationList ?? [],
            EventType.TestRegistered => contextEvents.OnTestRegistered?.InvocationList ?? [],
            EventType.TestStart => contextEvents.OnTestStart?.InvocationList ?? [],
            EventType.TestEnd => contextEvents.OnTestEnd?.InvocationList ?? [],
            EventType.TestSkipped => contextEvents.OnTestSkipped?.InvocationList ?? [],
            EventType.LastTestInClass => contextEvents.OnLastTestInClass?.InvocationList ?? [],
            EventType.LastTestInAssembly => contextEvents.OnLastTestInAssembly?.InvocationList ?? [],
            EventType.LastTestInTestSession => contextEvents.OnLastTestInTestSession?.InvocationList ?? [],
            EventType.TestRetry => contextEvents.OnTestRetry?.InvocationList ?? [],
            EventType.FirstTestInClass => contextEvents.OnFirstTestInClass?.InvocationList ?? [],
            EventType.FirstTestInAssembly => contextEvents.OnFirstTestInAssembly?.InvocationList ?? [],
            EventType.FirstTestInTestSession => contextEvents.OnFirstTestInTestSession?.InvocationList ?? [],
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
        };
    }

    private static IEnumerable<object?> CollectProperties(this object? obj)
    {
        if (obj is null)
        {
            yield break;
        }

        if (!Sources.DataGeneratorProperties.TryGetValue(obj.GetType(), out var properties))
        {
            // We assume we are not in a source generator context, so we use reflection to get the properties.
#pragma warning disable IL2075
            properties = obj.GetType().GetProperties();
#pragma warning restore IL2075
        }

        foreach (var property in properties
                     .Select(x => x.GetValue(obj))
                     .OfType<IAsyncInitializer>())
        {
            foreach (var innerProperty in CollectProperties(property))
            {
                yield return innerProperty;
            }

            yield return property;
        }
    }
}
