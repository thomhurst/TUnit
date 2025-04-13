using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.Messages;
using Polyfills;
using TUnit.Core;
using TUnit.Core.Enums;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;
using TUnit.Engine.Extensions;
using TUnit.Engine.Helpers;

namespace TUnit.Engine.Services;

[SuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
[SuppressMessage("Trimming", "IL2070:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The parameter of method does not have matching annotations.")]
[SuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
[SuppressMessage("Trimming", "IL2075:\'this\' argument does not satisfy \'DynamicallyAccessedMembersAttribute\' in call to target method. The return value of the source method does not have matching annotations.")]
internal class TestsConstructor(IExtension extension, 
    TestsCollector testsCollector,
    DependencyCollector dependencyCollector, 
    IServiceProvider serviceProvider) : IDataProducer
{
    public DiscoveredTest[] GetTests(CancellationToken cancellationToken)
    {
        var discoveredTests = IsReflectionScannerEnabled()
            ? GetByReflectionScanner()
            : GetBySourceGenerationRegistration();
        
        dependencyCollector.ResolveDependencies(discoveredTests, cancellationToken);
        
        return discoveredTests;
    }

    private DiscoveredTest[] GetByReflectionScanner()
    {
        var allTypes = GetTypesByReflection();
        
        var testMethods = allTypes
            .SelectMany(x => x.GetMethods())
            .Where(x => x.GetCustomAttributes<TestAttribute>().Any())
            .ToArray();
        
        return Build(testMethods, allTypes)
            .SelectMany(ConstructTests)
            .ToArray();
    }

    private static IEnumerable<DynamicTest> Build(MethodInfo[] testMethods, Type[] allTypes)
    {
        foreach (var testMethod in testMethods)
        {
            var testAttribute = testMethod.GetCustomAttribute<TestAttribute>()!;

            var types = GetDerivedTypes(allTypes, testMethod.DeclaringType!);

            foreach (var type in types)
            {
                foreach (var typeDataAttribute in GetDataAttributes(type))
                {
                    foreach (var testDataAttribute in GetDataAttributes(testMethod))
                    {
                        foreach (var classInstanceArguments in GetArguments(type, testMethod, typeDataAttribute, DataGeneratorType.ClassParameters, null, null))
                        {
                            var instance = Activator.CreateInstance(type, classInstanceArguments);

                            foreach (var testArguments in GetArguments(type, testMethod, testDataAttribute, DataGeneratorType.TestParameters, instance, classInstanceArguments))
                            {
                                yield return new UntypedDynamicTest(type, testMethod)
                                {
                                    TestMethodArguments = testArguments,
                                    Attributes =
                                    [
                                        ..testMethod.GetCustomAttributes(),
                                        ..type.GetCustomAttributes(),
                                        ..type.Assembly.GetCustomAttributes()
                                    ],
                                    TestName = testMethod.Name,
                                    TestClassArguments = classInstanceArguments,
                                    TestFilePath = testAttribute.File,
                                    TestLineNumber = testAttribute.Line,
                                };
                            }
                        }
                    }
                }
            }
        }
    }

    private static IEnumerable<object?[]> 
        GetArguments([DynamicallyAccessedMembers(
            DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type, MethodInfo method, IDataAttribute testDataAttribute, DataGeneratorType dataGeneratorType, object? instance, object?[]? classInstanceArguments)
    {
        if (testDataAttribute is IDataSourceGeneratorAttribute dataSourceGeneratorAttribute)
        {
            var parameters = dataGeneratorType == DataGeneratorType.TestParameters
                ? method.GetParameters()
                : type.GetConstructors().FirstOrDefault(x => !x.IsStatic)?.GetParameters() ?? [];
            
            var invoke = dataSourceGeneratorAttribute.GetType().GetMethod("GenerateDataSources")!.Invoke(testDataAttribute, [
                new DataGeneratorMetadata
                {
                    Type = dataGeneratorType,
                    TestInformation = SourceModelHelpers.BuildTestMethod(type, method, [], method.Name), // TODO
                    ClassInstanceArguments = classInstanceArguments,
                    MembersToGenerate = parameters.Select(x => new SourceGeneratedParameterInformation(x.ParameterType)
                    {
                        Name = x.Name!,
                        Attributes = x.GetCustomAttributes().ToArray(),
                    }).ToArray<SourceGeneratedMemberInformation>(),
                    TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),
                    TestClassInstance = instance,
                    TestSessionId = string.Empty,
                }
            ]);

            var funcEnumerable = Unsafe.As<IEnumerable<Func<object>>>(invoke)?.ToArray() ?? []!;

            if (funcEnumerable.Length == 0)
            {
                yield return [];
                yield break;
            }
            
            foreach (var func in funcEnumerable)
            {
                var obj = func.Invoke();

                yield return obj as object[] ?? [obj];
            }
        }
        else if (testDataAttribute is ArgumentsAttribute argumentsAttribute)
        {
            yield return argumentsAttribute.Values;
        }
        else if (testDataAttribute is InstanceMethodDataSourceAttribute instanceMethodDataSourceAttribute)
        {
            var methodDataSourceType = instanceMethodDataSourceAttribute.ClassProvidingDataSource 
                ?? instance?.GetType()
                ?? type;

            var methodResult = methodDataSourceType.GetMethod(instanceMethodDataSourceAttribute.MethodNameProvidingDataSource)?.Invoke(instance, []);
            
            if (TupleHelper.TryParseTupleToObjectArray(methodResult, out var objectArray))
            {
                yield return objectArray;
                yield break;
            }
            
            yield return
            [
                methodResult
            ];
        }
        else if (testDataAttribute is MethodDataSourceAttribute methodDataSourceAttribute)
        {
            var methodDataSourceType = methodDataSourceAttribute.ClassProvidingDataSource 
                ?? instance?.GetType()
                ?? type;
            
            var methodResult = methodDataSourceType.GetMethod(methodDataSourceAttribute.MethodNameProvidingDataSource)?.Invoke(instance, []) ?? Array.Empty<object>();

            if (TupleHelper.TryParseTupleToObjectArray(methodResult, out var objectArray))
            {
                yield return objectArray;
                yield break;
            }
            
            yield return
            [
                methodResult
            ];
        }
        else if (testDataAttribute is NoOpDataAttribute)
        {
            yield return [];
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(testDataAttribute));
        }
    }

    private static IDataAttribute[] GetDataAttributes(MemberInfo memberInfo)
    {
        var dataAttributes = memberInfo.GetCustomAttributes()
            .OfType<IDataAttribute>()
            .ToArray();

        if (dataAttributes.Length == 0)
        {
            return NoOpDataAttribute.Array;
        }
        
        return dataAttributes;
    }

    public static Type[] GetDerivedTypes(Type[] allTypes, Type baseType)
    {
        return allTypes
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(baseType))
            .ToArray();
    }

    private static Type[] GetTypesByReflection()
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(assembly =>
            {
                try
                {
                    return assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    return e.Types.OfType<Type>().ToArray();
                }
            })
            .ToArray();
    }

    private DiscoveredTest[] GetBySourceGenerationRegistration()
    {
        var testMetadatas = testsCollector.GetTests();
        
        var dynamicTests = testsCollector.GetDynamicTests();

        var discoveredTests = testMetadatas
            .Select(ConstructTest)
            .Concat(dynamicTests.SelectMany(ConstructTests))
            .ToArray();
        
        return discoveredTests;
    }

    private static bool IsReflectionScannerEnabled()
    {
        return Assembly.GetEntryAssembly()?
            .GetCustomAttributes()
            .OfType<AssemblyMetadataAttribute>()
            .FirstOrDefault(x => x.Key == "TUnit.ReflectionScanner")
            ?.Value == "true";
    }

    public DiscoveredTest ConstructTest(TestMetadata testMetadata)
    {
        var testDetails = testMetadata.BuildTestDetails();

        var testContext = new TestContext(serviceProvider, testDetails, testMetadata);

        if (testMetadata.DiscoveryException is not null)
        {
            testContext.SetResult(testMetadata.DiscoveryException);
        }

        RunOnTestDiscoveryAttributeHooks([..testDetails.DataAttributes, ..testDetails.Attributes], testContext);

        var discoveredTest = testMetadata.BuildDiscoveredTest(testContext);

        testContext.InternalDiscoveredTest = discoveredTest;

        return discoveredTest;
    }

    public IEnumerable<DiscoveredTest> ConstructTests(DynamicTest dynamicTest)
    {
        return dynamicTest.BuildTestMetadatas().Select(ConstructTest);
    }

    private static void RunOnTestDiscoveryAttributeHooks(IEnumerable<Attribute> attributes, TestContext testContext)
    {
        DiscoveredTestContext? discoveredTestContext = null;
        foreach (var onTestDiscoveryAttribute in attributes.OfType<ITestDiscoveryEventReceiver>().Reverse()) // Reverse to run assembly, then class, then method
        {
            onTestDiscoveryAttribute.OnTestDiscovery(discoveredTestContext ??= new DiscoveredTestContext(testContext));
        }
    }
    
    
    public Task<bool> IsEnabledAsync()
    {
        return extension.IsEnabledAsync();
    }

    public string Uid => extension.Uid;
    public string Version => extension.Version;
    public string DisplayName => extension.DisplayName;
    public string Description => extension.Description;
    public Type[] DataTypesProduced => [typeof(TestNodeUpdateMessage)];
}