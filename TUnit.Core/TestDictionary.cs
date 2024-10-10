using System.Collections.Concurrent;
using System.Reflection;
using TUnit.Core.Data;

namespace TUnit.Core;

#if !DEBUG
[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
#endif
internal static class TestDictionary
{
    public static readonly Dictionary<string, DiscoveredTest> Tests = new();
    public static readonly Dictionary<string, FailedInitializationTest> FailedInitializationTests = new();
    
    public static readonly ConcurrentDictionary<Type, ClassHookContext> ClassHookContexts = new();
    public static readonly ConcurrentDictionary<Type, List<(string Name, StaticHookMethod HookMethod, LazyHook<string, IHookMessagePublisher> Action)>> ClassSetUps = new();
    public static readonly ConcurrentDictionary<Type, List<(string Name, StaticHookMethod HookMethod, Func<Task> Action)>> ClassCleanUps = new();
    
    public static readonly ConcurrentDictionary<Assembly, AssemblyHookContext> AssemblyHookContexts = new();
    public static readonly GetOnlyDictionary<Assembly, List<(string Name, StaticHookMethod HookMethod, LazyHook<string, IHookMessagePublisher> Action)>> AssemblySetUps = new();
    public static readonly GetOnlyDictionary<Assembly, List<(string Name, StaticHookMethod HookMethod, Func<Task> Action)>> AssemblyCleanUps = new();

    public static readonly ConcurrentDictionary<Type, List<(string Name, int Order, Func<object, DiscoveredTest, Task> Action)>> TestSetUps = new();
    public static readonly ConcurrentDictionary<Type, List<(string Name, int Order, Func<object, DiscoveredTest, Task> Action)>> TestCleanUps = new();
    
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<TestContext, Task> Action)> GlobalTestSetUps = [];
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<TestContext, Task> Action)> GlobalTestCleanUps = [];

    public static readonly List<(string Name, StaticHookMethod HookMethod, LazyHook<string, IHookMessagePublisher> Action)> GlobalClassSetUps = [];
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<ClassHookContext, Task> Action)> GlobalClassCleanUps = [];
    
    public static readonly List<(string Name, StaticHookMethod HookMethod, LazyHook<string, IHookMessagePublisher> Action)> GlobalAssemblySetUps = [];
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<AssemblyHookContext, Task> Action)> GlobalAssemblyCleanUps = [];
    
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<BeforeTestDiscoveryContext, Task> Action)> BeforeTestDiscovery = [];
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<TestDiscoveryContext, Task> Action)> AfterTestDiscovery = [];
    
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<TestSessionContext, Task> Action)> BeforeTestSession = [];
    public static readonly List<(string Name, StaticHookMethod HookMethod, Func<TestSessionContext, Task> Action)> AfterTestSession = [];
    
    internal static void AddTest(string testId, DiscoveredTest discoveredTest)
    {
        Tests[testId] = discoveredTest;
    }

    public static void RegisterFailedTest(string testId, FailedInitializationTest failedInitializationTest)
    {
        FailedInitializationTests[testId] = failedInitializationTest;
    }
    
    internal static IReadOnlyCollection<DiscoveredTest> GetAllTests()
    {
        return Tests.Values;
    }

    internal static DiscoveredTest[] GetTestsByNameAndParameters(string testName, IEnumerable<Type> methodParameterTypes, Type classType, IEnumerable<Type> classParameterTypes)
    {
        var testsWithoutMethodParameterTypesMatching = Tests.Values.Where(x =>
            x.TestContext.TestDetails.TestName == testName &&
            x.TestContext.TestDetails.ClassType == classType &&
            x.TestContext.TestDetails.TestClassParameterTypes.SequenceEqual(classParameterTypes))
            .ToArray();

        if (testsWithoutMethodParameterTypesMatching.GroupBy(x => string.Join(", ", x.TestContext.TestDetails.TestMethodParameterTypes.Select(t => t.FullName)))
                .Count() > 1)
        {
            return testsWithoutMethodParameterTypesMatching.Where(x =>
                    x.TestContext.TestDetails.TestMethodParameterTypes.SequenceEqual(methodParameterTypes)).ToArray();
        }
        
        return testsWithoutMethodParameterTypesMatching;
    }
    
    internal static FailedInitializationTest[] GetFailedToInitializeTests()
    {
        return FailedInitializationTests.Values.ToArray();
    }
}