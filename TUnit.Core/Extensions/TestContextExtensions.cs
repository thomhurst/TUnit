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
        GetPossibleEventObjects(context, EventType.TestRegistered).OfType<ITestRegisteredEventReceiver>();

    internal static IEnumerable<ITestStartEventReceiver> GetTestStartEventObjects(this TestContext context) =>
        GetPossibleEventObjects(context, EventType.TestStart).OfType<ITestStartEventReceiver>();

    internal static IEnumerable<IAsyncInitializer> GetOnInitializeObjects(this TestContext context)
    {
        return GetEventObjects(context, EventType.Initialize)
            .Distinct()
            .OfType<IAsyncInitializer>()
            .OrderBy(x => x is IEventReceiver receiver ? receiver.Order : int.MaxValue);
    }

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
            GetEventObjects(context, EventType.Dispose),
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
        return GetEventObjects(context, eventType)
            .Distinct()
            .OfType<IEventReceiver>()
            .OrderBy(x => x.Order);
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
        return eventType switch
        {
            EventType.Initialize => contextEvents.OnInitialize?.InvocationList.Select(x => new TestInitializeEventWrapper(x)) ?? [],
            EventType.Dispose => contextEvents.OnDispose?.InvocationList.Select(x => new TestDisposeEventWrapper(x)) ?? [],
            EventType.TestRegistered => contextEvents.OnTestRegistered?.InvocationList.Select(x => new TestRegisteredEventWrapper(x)) ?? [],
            EventType.TestStart => contextEvents.OnTestStart?.InvocationList.Select(x => new TestStartEventWrapper(x)) ?? [],
            EventType.TestEnd => contextEvents.OnTestEnd?.InvocationList.Select(x => new TestEndEventWrapper(x)) ?? [],
            EventType.TestSkipped => contextEvents.OnTestSkipped?.InvocationList.Select(x => new TestSkippedEventWrapper(x)) ?? [],
            EventType.LastTestInClass => contextEvents.OnLastTestInClass?.InvocationList.Select(x => new LastTestInClassEventWrapper(x)) ?? [],
            EventType.LastTestInAssembly => contextEvents.OnLastTestInAssembly?.InvocationList.Select(x => new LastTestInAssemblyEventWrapper(x)) ?? [],
            EventType.LastTestInTestSession => contextEvents.OnLastTestInTestSession?.InvocationList.Select(x => new LastTestInTestSessionEventWrapper(x)) ?? [],
            EventType.TestRetry => contextEvents.OnTestRetry?.InvocationList.Select(x => new TestRetryEventWrapper(x)) ?? [],
            EventType.FirstTestInClass => contextEvents.OnFirstTestInClass?.InvocationList.Select(x => new FirstTestInClassEventWrapper(x)) ?? [],
            EventType.FirstTestInAssembly => contextEvents.OnFirstTestInAssembly?.InvocationList.Select(x => new FirstTestInAssemblyEventWrapper(x)) ?? [],
            EventType.FirstTestInTestSession => contextEvents.OnFirstTestInTestSession?.InvocationList.Select(x => new FirstTestInTestSessionEventWrapper(x)) ?? [],
            _ => throw new ArgumentOutOfRangeException(nameof(eventType), eventType, null)
        };
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
