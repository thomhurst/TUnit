using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces.SourceGenerator;

namespace TUnit.Core;

/// <summary>
/// Test source that provides test descriptors to be expanded by the appropriate builder.
/// </summary>
public class TestMetadataSource : ITestSource
{
    private readonly IReadOnlyList<ITestDescriptor> _testDescriptors;
    private readonly ITestDefinitionBuilder _staticBuilder;
    private readonly ITestDefinitionBuilder _dynamicBuilder;
    
    [UnconditionalSuppressMessage("AOT", "IL3050:RequiresDynamicCode", 
        Justification = "DynamicTestBuilder is only used for dynamic test paths which are not AOT-compatible by design")]
    [UnconditionalSuppressMessage("Trimming", "IL2026:RequiresUnreferencedCode", 
        Justification = "DynamicTestBuilder is only used for dynamic test paths which are not AOT-compatible by design")]
    public TestMetadataSource(IReadOnlyList<ITestDescriptor> testDescriptors)
    {
        _testDescriptors = testDescriptors;
        _staticBuilder = new StaticTestBuilder();
        _dynamicBuilder = new DynamicTestBuilder();
    }
    
    public async Task<DiscoveryResult> DiscoverTestsAsync(string sessionId)
    {
        var allDefinitions = new List<ITestDefinition>();
        var failures = new List<DiscoveryFailure>();
        
        foreach (var descriptor in _testDescriptors)
        {
            try
            {
                // Skip if test is marked as skipped
                if (descriptor.IsSkipped)
                {
                    // Still need to create a test definition for skipped tests
                    // so they show up in test runners as skipped
                    var skippedDefinition = CreateSkippedTestDefinition(descriptor);
                    allDefinitions.Add(skippedDefinition);
                    continue;
                }
                
                // Select appropriate builder based on descriptor type
                var builder = descriptor switch
                {
                    StaticTestDefinition => _staticBuilder,
                    DynamicTestMetadata => _dynamicBuilder,
                    _ => throw new InvalidOperationException($"Unknown test descriptor type: {descriptor.GetType().Name}")
                };
                
                // Build all test definitions from descriptor
                var definitions = await builder.BuildTestDefinitionsAsync(descriptor);
                allDefinitions.AddRange(definitions);
            }
            catch (Exception ex)
            {
                // Record discovery failure
                failures.Add(new DiscoveryFailure
                {
                    TestId = descriptor.TestId,
                    TestMethodName = GetTestMethodName(descriptor),
                    TestFilePath = descriptor.TestFilePath,
                    TestLineNumber = descriptor.TestLineNumber,
                    Exception = ex
                });
            }
        }
        
        return new DiscoveryResult
        {
            TestDefinitions = allDefinitions,
            DiscoveryFailures = failures
        };
    }
    
    private static string GetTestMethodName(ITestDescriptor descriptor)
    {
        return descriptor switch
        {
            StaticTestDefinition staticDef => staticDef.TestMethodInfo.Name,
            DynamicTestMetadata dynamicMeta => dynamicMeta.MethodMetadata.Name,
            _ => "Unknown"
        };
    }
    
    private static MethodMetadata CreateMethodMetadata(StaticTestDefinition staticDef)
    {
        return new MethodMetadata
        {
            Name = staticDef.TestMethodInfo.Name,
            ReflectionInformation = staticDef.TestMethodInfo,
            Parameters = Array.Empty<ParameterMetadata>(),
            GenericTypeCount = 0,
            Class = new ClassMetadata 
            { 
                Type = staticDef.TestClassType,
                Name = staticDef.TestClassType.Name,
                Namespace = staticDef.TestClassType.Namespace ?? string.Empty,
                TypeReference = TypeReference.CreateConcrete(staticDef.TestClassType.AssemblyQualifiedName!),
                Assembly = new AssemblyMetadata
                {
                    Name = staticDef.TestClassType.Assembly.GetName().Name ?? string.Empty,
                    Attributes = Array.Empty<AttributeMetadata>()
                },
                Parameters = Array.Empty<ParameterMetadata>(),
                Properties = Array.Empty<PropertyMetadata>(),
                Parent = null,
                Attributes = Array.Empty<AttributeMetadata>()
            },
            ReturnTypeReference = TypeReference.CreateConcrete(staticDef.TestMethodInfo.ReturnType.AssemblyQualifiedName!),
            ReturnType = staticDef.TestMethodInfo.ReturnType,
            TypeReference = TypeReference.CreateConcrete(staticDef.TestClassType.AssemblyQualifiedName!),
            Type = staticDef.TestClassType,
            Attributes = Array.Empty<AttributeMetadata>()
        };
    }
    
    private static MethodMetadata GetMethodMetadata(ITestDescriptor descriptor)
    {
        return descriptor switch
        {
            StaticTestDefinition staticDef => CreateMethodMetadata(staticDef),
            DynamicTestMetadata dynamicMeta => dynamicMeta.MethodMetadata,
            _ => throw new InvalidOperationException($"Unknown test descriptor type: {descriptor.GetType().Name}")
        };
    }
    
    private TestDefinition CreateSkippedTestDefinition(ITestDescriptor descriptor)
    {
        // Create a simple test definition for skipped tests
        return new TestDefinition
        {
            TestId = descriptor.TestId.Replace("{TestIndex}", "0").Replace("{RepeatIndex}", "0"),
            MethodMetadata = GetMethodMetadata(descriptor),
            TestFilePath = descriptor.TestFilePath,
            TestLineNumber = descriptor.TestLineNumber,
            TestClassFactory = () => throw new InvalidOperationException("Skipped test should not be instantiated"),
            TestMethodInvoker = (_, _) => throw new InvalidOperationException("Skipped test should not be invoked"),
            PropertiesProvider = () => new Dictionary<string, object?>()
        };
    }
}