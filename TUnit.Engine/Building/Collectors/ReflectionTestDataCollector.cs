using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using TUnit.Core;
using TUnit.Core.Interfaces;
using TUnit.Engine.Building.Interfaces;

namespace TUnit.Engine.Building.Collectors;

/// <summary>
/// Test data collector for reflection mode - discovers tests at runtime
/// </summary>
[RequiresDynamicCode("Reflection mode requires dynamic code generation")]
[RequiresUnreferencedCode("Reflection mode may access types not preserved by trimming")]
public sealed class ReflectionTestDataCollector : ITestDataCollector
{
    private readonly Assembly[] _assemblies;
    
    public ReflectionTestDataCollector(params Assembly[] assemblies)
    {
        _assemblies = assemblies ?? throw new ArgumentNullException(nameof(assemblies));
    }
    
    public async Task<IEnumerable<TestMetadata>> CollectTestsAsync()
    {
        var testMetadata = new List<TestMetadata>();
        
        foreach (var assembly in _assemblies)
        {
            var types = assembly.GetTypes()
                .Where(t => !t.IsAbstract && t.IsClass && !t.IsGenericTypeDefinition);
            
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .Where(m => m.GetCustomAttribute<TestAttribute>() != null);
                
                foreach (var method in methods)
                {
                    var metadata = CreateTestMetadata(type, method);
                    if (metadata != null)
                    {
                        testMetadata.Add(metadata);
                    }
                }
            }
        }
        
        return await Task.FromResult(testMetadata);
    }
    
    private TestMetadata? CreateTestMetadata(Type testClass, MethodInfo testMethod)
    {
        var testAttribute = testMethod.GetCustomAttribute<TestAttribute>();
        if (testAttribute == null) return null;
        
        // Get test information
        var testId = GenerateTestId(testClass, testMethod);
        var displayName = testMethod.Name;
        
        // Collect data sources
        var methodDataSources = CollectMethodDataSources(testMethod);
        var classDataSources = CollectClassDataSources(testClass);
        var propertyDataSources = CollectPropertyDataSources(testClass);
        
        // Get test properties
        var categories = testMethod.GetCustomAttributes<CategoryAttribute>()
            .Select(c => c.Category)
            .ToArray();
        
        var skipAttribute = testMethod.GetCustomAttribute<SkipAttribute>();
        var timeoutAttribute = testMethod.GetCustomAttribute<TimeoutAttribute>();
        var retryAttribute = testMethod.GetCustomAttribute<RetryAttribute>();
        var notInParallelAttribute = testMethod.GetCustomAttribute<NotInParallelAttribute>();
        var dependsOnAttributes = testMethod.GetCustomAttributes<DependsOnAttribute>();
        
        // Create hooks metadata
        var hooks = CollectHooks(testClass, testMethod);
        
        return new TestMetadata
        {
            TestId = testId,
            TestName = displayName,
            TestClassType = testClass,
            TestMethodName = testMethod.Name,
            Categories = categories,
            IsSkipped = skipAttribute != null,
            SkipReason = skipAttribute?.Reason,
            TimeoutMs = (int?)timeoutAttribute?.Timeout.TotalMilliseconds,
            RetryCount = retryAttribute?.Times ?? 0,
            CanRunInParallel = notInParallelAttribute == null,
            DependsOn = dependsOnAttributes.Select(d => d.TestName!).ToArray(),
            DataSources = methodDataSources,
            ClassDataSources = classDataSources,
            PropertyDataSources = propertyDataSources,
            InstanceFactory = null, // Will use reflection
            TestInvoker = null, // Will use reflection
            ParameterCount = testMethod.GetParameters().Length,
            ParameterTypes = testMethod.GetParameters().Select(p => p.ParameterType).ToArray(),
            Hooks = hooks,
            MethodInfo = testMethod,
            FilePath = null, // Not available in reflection mode
            LineNumber = null, // Not available in reflection mode
            GenericTypeInfo = testClass.IsGenericType ? CreateGenericTypeInfo(testClass) : null,
            GenericMethodInfo = testMethod.IsGenericMethodDefinition ? CreateGenericMethodInfo(testMethod) : null
        };
    }
    
    private TestDataSource[] CollectMethodDataSources(MethodInfo method)
    {
        var dataSources = new List<TestDataSource>();
        
        // Arguments attributes
        var argumentsAttributes = method.GetCustomAttributes<ArgumentsAttribute>();
        foreach (var args in argumentsAttributes)
        {
            dataSources.Add(new StaticTestDataSource(new object?[][] { args.Values }));
        }
        
        // Method data source attributes
        var methodDataSources = method.GetCustomAttributes<MethodDataSourceAttribute>();
        foreach (var mds in methodDataSources)
        {
            dataSources.Add(new DynamicTestDataSource(false)
            {
                SourceType = mds.ClassProvidingDataSource ?? method.DeclaringType!,
                SourceMemberName = mds.MethodNameProvidingDataSource
            });
        }
        
        // Data source attributes implementing IDataSource
        var dataSourceAttributes = method.GetCustomAttributes()
            .OfType<IDataSource>();
        foreach (var ds in dataSourceAttributes)
        {
            dataSources.Add(new AttributeDataSource(ds));
        }
        
        return dataSources.ToArray();
    }
    
    private TestDataSource[] CollectClassDataSources(Type testClass)
    {
        var dataSources = new List<TestDataSource>();
        
        // Class constructor arguments
        var constructor = testClass.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault();
            
        if (constructor != null)
        {
            var parameters = constructor.GetParameters();
            foreach (var param in parameters)
            {
                // Check for data source attributes on parameters
                var argumentsAttr = param.GetCustomAttribute<ArgumentsAttribute>();
                if (argumentsAttr != null)
                {
                    dataSources.Add(new StaticTestDataSource(new object?[][] { argumentsAttr.Values }));
                }
                
                var methodDataSource = param.GetCustomAttribute<MethodDataSourceAttribute>();
                if (methodDataSource != null)
                {
                    dataSources.Add(new DynamicTestDataSource(false)
                    {
                        SourceType = methodDataSource.ClassProvidingDataSource ?? testClass,
                        SourceMemberName = methodDataSource.MethodNameProvidingDataSource
                    });
                }
            }
        }
        
        return dataSources.ToArray();
    }
    
    private PropertyDataSource[] CollectPropertyDataSources(Type testClass)
    {
        var propertyDataSources = new List<PropertyDataSource>();
        
        var properties = testClass.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);
            
        foreach (var property in properties)
        {
            var dataSourceAttr = property.GetCustomAttributes()
                .OfType<IDataSource>()
                .FirstOrDefault();
                
            if (dataSourceAttr != null)
            {
                propertyDataSources.Add(new PropertyDataSource
                {
                    PropertyName = property.Name,
                    PropertyType = property.PropertyType,
                    DataSource = new AttributeDataSource(dataSourceAttr)
                });
            }
        }
        
        return propertyDataSources.ToArray();
    }
    
    private TestHooks CollectHooks(Type testClass, MethodInfo testMethod)
    {
        var hooks = new TestHooks
        {
            BeforeClass = CollectHooksOfType<BeforeAttribute>(testClass, HookLevel.Class),
            AfterClass = CollectHooksOfType<AfterAttribute>(testClass, HookLevel.Class),
            BeforeTest = CollectHooksOfType<BeforeAttribute>(testClass, HookLevel.Test),
            AfterTest = CollectHooksOfType<AfterAttribute>(testClass, HookLevel.Test)
        };
        
        return hooks;
    }
    
    private HookMetadata[] CollectHooksOfType<TAttribute>(Type testClass, HookLevel level) 
        where TAttribute : Attribute
    {
        var hookMethods = testClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static)
            .Where(m => m.GetCustomAttribute<TAttribute>() != null)
            .Select(m => new HookMetadata
            {
                Name = m.Name,
                Level = level,
                Order = 0, // Could be extended to support ordering
                Invoker = null, // Will use reflection
                MethodInfo = m,
                DeclaringType = testClass,
                IsStatic = m.IsStatic
            })
            .ToArray();
            
        return hookMethods;
    }
    
    private static string GenerateTestId(Type testClass, MethodInfo testMethod)
    {
        return $"{testClass.FullName}.{testMethod.Name}";
    }
    
    private static GenericTypeInfo CreateGenericTypeInfo(Type type)
    {
        var genericArgs = type.GetGenericArguments();
        return new GenericTypeInfo
        {
            ParameterNames = genericArgs.Select(a => a.Name).ToArray(),
            Constraints = genericArgs.Select(CreateGenericConstraints).ToArray()
        };
    }
    
    private static GenericMethodInfo CreateGenericMethodInfo(MethodInfo method)
    {
        var genericArgs = method.GetGenericArguments();
        return new GenericMethodInfo
        {
            ParameterNames = genericArgs.Select(a => a.Name).ToArray(),
            Constraints = genericArgs.Select(CreateGenericConstraints).ToArray(),
            ParameterPositions = new int[0] // Would need more analysis to determine
        };
    }
    
    private static GenericParameterConstraints CreateGenericConstraints(Type genericParam)
    {
        return new GenericParameterConstraints
        {
            ParameterName = genericParam.Name,
            BaseTypeConstraint = genericParam.BaseType != typeof(object) ? genericParam.BaseType : null,
            InterfaceConstraints = genericParam.GetInterfaces(),
            HasDefaultConstructorConstraint = genericParam.GenericParameterAttributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint),
            HasReferenceTypeConstraint = genericParam.GenericParameterAttributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint),
            HasValueTypeConstraint = genericParam.GenericParameterAttributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint),
            HasNotNullConstraint = false // Not easily detectable via reflection
        };
    }
}

/// <summary>
/// Wrapper for attribute-based data sources
/// </summary>
internal sealed class AttributeDataSource : TestDataSource
{
    private readonly IDataSource _dataSource;
    
    public AttributeDataSource(IDataSource dataSource)
    {
        _dataSource = dataSource;
    }
    
    public override IEnumerable<Func<object?[]>> GetDataFactories()
    {
        // Create a context for the data source
        var context = new DataSourceContext(
            GetType(), // Placeholder - should be the test class type
            DataSourceLevel.Method,
            null,
            null,
            null,
            null);
            
        return _dataSource.GenerateDataFactories(context);
    }
    
    public override bool IsShared => _dataSource.IsShared;
}