using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;
using TUnit.Engine.Building;
using TUnit.Engine.Discovery;

namespace TUnit.UnitTests;

[RequiresUnreferencedCode("Reflection tests require unreferenced code")]
[RequiresDynamicCode("Reflection tests require dynamic code")]
public class ReflectionTestDataCollectorTests
{
    [Test]
    public async Task ReflectionTestDataCollector_DiscoversSingleTest()
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        await Assert.That(tests).IsNotNull();
        await Assert.That(tests.Count).IsGreaterThan(0);
        
        var thisTest = tests.FirstOrDefault(t => t.TestMethodName == nameof(ReflectionTestDataCollector_DiscoversSingleTest));
        await Assert.That(thisTest).IsNotNull();
        await Assert.That(thisTest!.TestClassType).IsEqualTo(typeof(ReflectionTestDataCollectorTests));
    }

    [Test]
    [Category("Reflection")]
    [Category("Discovery")]
    public async Task ReflectionTestDataCollector_ExtractsCategories()
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        var thisTest = tests.FirstOrDefault(t => t.TestMethodName == nameof(ReflectionTestDataCollector_ExtractsCategories));
        await Assert.That(thisTest).IsNotNull();
        await Assert.That(thisTest!.Categories).Contains("Reflection");
        await Assert.That(thisTest.Categories).Contains("Discovery");
    }

    [Test]
    [Timeout(5000)]
    public async Task ReflectionTestDataCollector_ExtractsTimeout(CancellationToken cancellationToken)
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        var thisTest = tests.FirstOrDefault(t => t.TestMethodName == nameof(ReflectionTestDataCollector_ExtractsTimeout));
        await Assert.That(thisTest).IsNotNull();
        await Assert.That(thisTest!.TimeoutMs).IsEqualTo(5000);
    }

    [Test]
    [Skip("Test skip functionality")]
    public async Task ReflectionTestDataCollector_ExtractsSkipAttribute()
    {
        await Task.CompletedTask;
    }

    [Test]
    public async Task ReflectionTestDataCollector_ExtractsSkipAttributeFromDiscovery()
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        var skippedTest = tests.FirstOrDefault(t => t.TestMethodName == nameof(ReflectionTestDataCollector_ExtractsSkipAttribute));
        await Assert.That(skippedTest).IsNotNull();
        await Assert.That(skippedTest!.IsSkipped).IsTrue();
        await Assert.That(skippedTest.SkipReason).IsEqualTo("Test skip functionality");
    }

    [Test]
    [Arguments(1, 2, 3)]
    [Arguments(4, 5, 6)]
    public async Task ReflectionTestDataCollector_ExtractsArgumentsDataSource(int a, int b, int c)
    {
        await Assert.That(a + b + c).IsGreaterThan(0);
    }

    [Test]
    public async Task ReflectionTestDataCollector_ExtractsArgumentsFromDiscovery()
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        var paramTest = tests.FirstOrDefault(t => t.TestMethodName == nameof(ReflectionTestDataCollector_ExtractsArgumentsDataSource));
        await Assert.That(paramTest).IsNotNull();
        await Assert.That(paramTest!.DataSources.Length).IsEqualTo(2);
        
        var metadata = new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext
            {
                TestMetadata = null
            }),
            MembersToGenerate = [],
            TestInformation = new MethodMetadata
            {
                Name = "TestMethod",
                Type = typeof(object),
                Class = new ClassMetadata
                {
                    Name = "TestClass",
                    Type = typeof(object),
                    Namespace = string.Empty,
                    TypeReference = new TypeReference { AssemblyQualifiedName = typeof(object).AssemblyQualifiedName },
                    Assembly = AssemblyMetadata.GetOrAdd("Test", () => new AssemblyMetadata { Name = "Test" }),
                    Parameters = [],
                    Properties = [],
                    Parent = null
                },
                Parameters = [],
                GenericTypeCount = 0,
                ReturnTypeReference = new TypeReference { AssemblyQualifiedName = typeof(void).AssemblyQualifiedName },
                ReturnType = typeof(void),
                TypeReference = new TypeReference { AssemblyQualifiedName = typeof(object).AssemblyQualifiedName }
            },
            Type = DataGeneratorType.TestParameters,
            TestSessionId = "test",
            TestClassInstance = null,
            ClassInstanceArguments = null
        };
        var dataSources = new List<Func<Task<object?[]?>>>();
        foreach (var ds in paramTest.DataSources)
        {
            await foreach (var dataRow in ds.GetDataRowsAsync(metadata))
            {
                dataSources.Add(dataRow);
            }
        }
        await Assert.That(dataSources.Count).IsEqualTo(2);

        var firstData = await dataSources[0]();
        await Assert.That(firstData).IsEqualTo([1, 2, 3]);

        var secondData = await dataSources[1]();
        await Assert.That(secondData).IsEqualTo([4, 5, 6]);
    }

    [Test]
    [Retry(3)]
    public async Task ReflectionTestDataCollector_ExtractsRetryCount()
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        var thisTest = tests.FirstOrDefault(t => t.TestMethodName == nameof(ReflectionTestDataCollector_ExtractsRetryCount));
        await Assert.That(thisTest).IsNotNull();
        await Assert.That(thisTest!.RetryCount).IsEqualTo(3);
    }

    [Test]
    [NotInParallel]
    public async Task ReflectionTestDataCollector_ExtractsNotInParallel()
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        var thisTest = tests.FirstOrDefault(t => t.TestMethodName == nameof(ReflectionTestDataCollector_ExtractsNotInParallel));
        await Assert.That(thisTest).IsNotNull();
        await Assert.That(thisTest!.CanRunInParallel).IsFalse();
    }

    [Test]
    public async Task TestDataCollectorFactory_CreatesReflectionCollectorForReflectionMode()
    {
        var collector = TestDataCollectorFactory.Create(TestExecutionMode.Reflection);
        
        await Assert.That(collector).IsNotNull();
        await Assert.That(collector).IsTypeOf<ReflectionTestDataCollector>();
    }

    [Test]
    [MethodDataSource(nameof(GetTestData))]
    public async Task ReflectionTestDataCollector_ExtractsMethodDataSource(string value)
    {
        await Assert.That(value).IsNotNull();
    }

    public static IEnumerable<object?[]> GetTestData()
    {
        yield return ["test1"];
        yield return ["test2"];
    }

    [Test]
    public async Task ReflectionTestDataCollector_ExtractsMethodDataSourceFromDiscovery()
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        var methodDataTest = tests.FirstOrDefault(t => t.TestMethodName == nameof(ReflectionTestDataCollector_ExtractsMethodDataSource));
        await Assert.That(methodDataTest).IsNotNull();
        await Assert.That(methodDataTest!.DataSources.Length).IsEqualTo(1);
        
        var metadata2 = new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext
            {
                TestMetadata = null
            }),
            MembersToGenerate = [],
            TestInformation = new MethodMetadata
            {
                Name = "TestMethod",
                Type = typeof(object),
                Class = new ClassMetadata
                {
                    Name = "TestClass",
                    Type = typeof(object),
                    Namespace = string.Empty,
                    TypeReference = new TypeReference { AssemblyQualifiedName = typeof(object).AssemblyQualifiedName },
                    Assembly = AssemblyMetadata.GetOrAdd("Test", () => new AssemblyMetadata { Name = "Test" }),
                    Parameters = [],
                    Properties = [],
                    Parent = null
                },
                Parameters = [],
                GenericTypeCount = 0,
                ReturnTypeReference = new TypeReference { AssemblyQualifiedName = typeof(void).AssemblyQualifiedName },
                ReturnType = typeof(void),
                TypeReference = new TypeReference { AssemblyQualifiedName = typeof(object).AssemblyQualifiedName }
            },
            Type = DataGeneratorType.TestParameters,
            TestSessionId = "test",
            TestClassInstance = null,
            ClassInstanceArguments = null
        };
        var dataFactories = new List<Func<Task<object?[]?>>>();
        await foreach (var dataRow in methodDataTest.DataSources[0].GetDataRowsAsync(metadata2))
        {
            dataFactories.Add(dataRow);
        }
        await Assert.That(dataFactories.Count).IsEqualTo(2);

        var firstData = await dataFactories[0]();
        await Assert.That(firstData[0]).IsEqualTo("test1");

        var secondData = await dataFactories[1]();
        await Assert.That(secondData[0]).IsEqualTo("test2");
    }
}

// Test class for hook discovery
public class ReflectionHookTests
{
    private static int _beforeClassCallCount;
    private static int _afterClassCallCount;
    private int _beforeTestCallCount;
    private int _afterTestCallCount;

    [Before(HookType.Class)]
    public static void BeforeClass()
    {
        _beforeClassCallCount++;
    }

    [After(HookType.Class)]
    public static void AfterClass()
    {
        _afterClassCallCount++;
    }

    [Before(HookType.Test)]
    public void BeforeTest()
    {
        _beforeTestCallCount++;
    }

    [After(HookType.Test)]
    public void AfterTest()
    {
        _afterTestCallCount++;
    }

    [Test]
    public async Task ReflectionTestDataCollector_DiscoverHooks()
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        var thisTest = tests.FirstOrDefault(t =>
            t.TestClassType == typeof(ReflectionHookTests) &&
            t.TestMethodName == nameof(ReflectionTestDataCollector_DiscoverHooks));

        await Assert.That(thisTest).IsNotNull();
    }
}

// Test generic classes and methods
public abstract class ReflectionGenericTestClass<T>
{
    [Test]
    public async Task GenericClassTest()
    {
        await Assert.That(typeof(T)).IsNotNull();
    }

    [Test]
    public async Task GenericMethodTest<TMethod>()
    {
        await Assert.That(typeof(TMethod)).IsNotNull();
    }
}

public class ReflectionGenericTests
{
    [Test]
    public async Task ReflectionTestDataCollector_DiscoverGenericTypeInfo()
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        var genericTest = tests.FirstOrDefault(t =>
            t.TestClassType.IsGenericTypeDefinition &&
            t.TestClassType.Name.StartsWith("ReflectionGenericTestClass"));

        await Assert.That(genericTest).IsNotNull();
        await Assert.That(genericTest!.GenericTypeInfo).IsNotNull();
        await Assert.That(genericTest.GenericTypeInfo!.ParameterNames.Length).IsEqualTo(1);
        await Assert.That(genericTest.GenericTypeInfo.ParameterNames[0]).IsEqualTo("T");
    }

    [Test]
    public async Task ReflectionTestDataCollector_DiscoverGenericMethodInfo()
    {
        var collector = new ReflectionTestDataCollector();
        var tests = (await collector.CollectTestsAsync("123")).ToList();
        
        var genericMethodTest = tests.FirstOrDefault(t =>
            t is { TestMethodName: "GenericMethodTest", TestClassType.IsGenericTypeDefinition: true });

        await Assert.That(genericMethodTest).IsNotNull();
        await Assert.That(genericMethodTest!.GenericMethodInfo).IsNotNull();
        await Assert.That(genericMethodTest.GenericMethodInfo!.ParameterNames.Length).IsEqualTo(1);
        await Assert.That(genericMethodTest.GenericMethodInfo.ParameterNames[0]).IsEqualTo("TMethod");
    }
}
