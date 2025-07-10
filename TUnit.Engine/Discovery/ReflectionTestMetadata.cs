using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Test metadata implementation that uses reflection for legacy/discovery scenarios
/// </summary>
internal sealed class ReflectionTestMetadata : TestMetadata
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
    private readonly Type _testClass;
    private readonly MethodInfo _testMethod;
    private Func<IAsyncEnumerable<TestDataCombination>>? _dataCombinationGenerator;
    private Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest>? _createExecutableTestFactory;

    public ReflectionTestMetadata(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass, 
        MethodInfo testMethod)
    {
        _testClass = testClass;
        _testMethod = testMethod;
    }

    public override Func<IAsyncEnumerable<TestDataCombination>> DataCombinationGenerator
    {
        get
        {
            if (_dataCombinationGenerator == null)
            {
                _dataCombinationGenerator = GenerateDataCombinations;
            }
            return _dataCombinationGenerator;
        }
    }

    public override Func<ExecutableTestCreationContext, TestMetadata, ExecutableTest> CreateExecutableTestFactory
    {
        get
        {
            if (_createExecutableTestFactory == null)
            {
                _createExecutableTestFactory = CreateExecutableTest;
            }
            return _createExecutableTestFactory;
        }
    }

    #pragma warning disable CS1998 // Async method lacks 'await' operators
    private async IAsyncEnumerable<TestDataCombination> GenerateDataCombinations()
    {
        #pragma warning restore CS1998
        // Extract data sources from attributes using reflection
        var methodDataSources = ExtractMethodDataSources();
        var classDataSources = ExtractClassDataSources();
        var propertyDataSources = ExtractPropertyDataSources();

        // If no data sources, yield a single empty combination
        if (!methodDataSources.Any() && !classDataSources.Any() && !propertyDataSources.Any())
        {
            yield return new TestDataCombination
            {
                MethodDataFactories = Array.Empty<Func<object?>>(),
                ClassDataFactories = Array.Empty<Func<object?>>(),
                PropertyValueFactories = new Dictionary<string, Func<object?>>(),
                MethodDataSourceIndex = -1,
                MethodLoopIndex = 0,
                ClassDataSourceIndex = -1,
                ClassLoopIndex = 0
            };
            yield break;
        }

        // TODO: Implement full data source expansion logic
        // For now, just yield empty combination
        yield return new TestDataCombination
        {
            MethodDataFactories = Array.Empty<Func<object?>>(),
            ClassDataFactories = Array.Empty<Func<object?>>(),
            PropertyValueFactories = new Dictionary<string, Func<object?>>(),
            MethodDataSourceIndex = -1,
            MethodLoopIndex = 0,
            ClassDataSourceIndex = -1,
            ClassLoopIndex = 0
        };
    }

    private ExecutableTest CreateExecutableTest(ExecutableTestCreationContext context, TestMetadata metadata)
    {
        // Create instance factory that uses reflection
        #pragma warning disable CS1998 // Async method lacks 'await' operators
        Func<Task<object>> createInstance = async () =>
        {
            #pragma warning restore CS1998
            if (InstanceFactory == null)
            {
                throw new InvalidOperationException($"No instance factory for {_testClass.Name}");
            }
            
            var instance = InstanceFactory(context.ClassArguments);
            
            // Apply property values
            foreach (var kvp in context.PropertyValues)
            {
                var property = _testClass.GetProperty(kvp.Key);
                property?.SetValue(instance, kvp.Value);
            }
            
            return instance;
        };

        // Create test invoker that uses reflection
        #pragma warning disable CS1998 // Async method lacks 'await' operators
        Func<object, object?[], Task> invokeTest = async (instance, args) =>
        {
            #pragma warning restore CS1998
            if (TestInvoker == null)
            {
                throw new InvalidOperationException($"No test invoker for {_testMethod.Name}");
            }
            
            await TestInvoker(instance, args);
        };

        return new DynamicExecutableTest(createInstance, invokeTest)
        {
            TestId = context.TestId,
            DisplayName = context.DisplayName,
            Metadata = metadata,
            Arguments = context.Arguments,
            ClassArguments = context.ClassArguments,
            PropertyValues = context.PropertyValues,
            BeforeTestHooks = context.BeforeTestHooks,
            AfterTestHooks = context.AfterTestHooks,
            Context = context.Context
        };
    }

    private List<TestDataSource> ExtractMethodDataSources()
    {
        var sources = new List<TestDataSource>();
        
        var attributes = _testMethod.GetCustomAttributes()
            .Where(a => a is IDataAttribute || IsDataSourceAttribute(a.GetType()))
            .ToList();

        // TODO: Convert attributes to TestDataSource instances
        
        return sources;
    }

    private List<TestDataSource> ExtractClassDataSources()
    {
        var sources = new List<TestDataSource>();
        
        // Check constructor parameters for data attributes
        var constructors = _testClass.GetConstructors();
        foreach (var ctor in constructors)
        {
            var parameters = ctor.GetParameters();
            // TODO: Extract data sources from constructor parameters
        }
        
        return sources;
    }

    private List<PropertyDataSource> ExtractPropertyDataSources()
    {
        var sources = new List<PropertyDataSource>();
        
        var properties = _testClass.GetProperties()
            .Where(p => p.GetCustomAttributes().Any(a => a is IDataAttribute || IsDataSourceAttribute(a.GetType())))
            .ToList();

        // TODO: Convert properties to PropertyDataSource instances
        
        return sources;
    }

    private static bool IsDataSourceAttribute(Type attributeType)
    {
        return attributeType.Name.EndsWith("DataAttribute") || 
               attributeType.Name.EndsWith("DataSourceAttribute") ||
               attributeType.Name == "ArgumentsAttribute";
    }
}