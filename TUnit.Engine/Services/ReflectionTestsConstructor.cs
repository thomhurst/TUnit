using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions;
using Microsoft.Testing.Platform.Extensions.TestFramework;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Helpers;
using TUnit.Engine.Services.Reflection;
#pragma warning disable TUnitWIP0001

namespace TUnit.Engine.Services;

[UnconditionalSuppressMessage("Trimming", "IL2075")]
[UnconditionalSuppressMessage("Trimming", "IL2072")]
[UnconditionalSuppressMessage("Trimming", "IL2067")]
[UnconditionalSuppressMessage("AOT", "IL3050")]
[UnconditionalSuppressMessage("Trimming", "IL2026")]
[UnconditionalSuppressMessage("Trimming", "IL2070")]
[UnconditionalSuppressMessage("Trimming", "IL2055")]
[UnconditionalSuppressMessage("Trimming", "IL2060")]
[UnconditionalSuppressMessage("Trimming", "IL2111")]
internal class ReflectionTestsConstructor(
    IExtension extension,
    DependencyCollector dependencyCollector,
    ContextManager contextManager,
    IServiceProvider serviceProvider) : BaseTestsConstructor(extension, dependencyCollector)
{
    private readonly TestBuilder _testBuilder = serviceProvider.GetService(typeof(TestBuilder)) as TestBuilder 
        ?? throw new InvalidOperationException("TestBuilder not found in service provider");

    protected override async Task<DiscoveredTest[]> DiscoverTestsAsync(ExecuteRequestContext context)
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException("Reflection tests are not supported with AOT or trimming enabled.");
        }
#endif

        var allTypes = ReflectionScanner.GetTypes();

        var discoveryResult = await DiscoverStandardTestsAsync(allTypes);
        var dynamicTests = await DiscoverDynamicTestsAsync(allTypes);

        var discoveredTests = new List<DiscoveredTest>();

        // Process standard tests
        var (tests, failures) = _testBuilder.BuildTests(discoveryResult);
        discoveredTests.AddRange(tests);

        // Log discovery failures
        foreach (var failure in failures)
        {
            Console.WriteLine($"Test discovery failed: {failure.TestClassName}.{failure.TestMethodName} - {failure.Reason}");
        }

        // Process dynamic tests
        foreach (var dynamicTest in dynamicTests)
        {
            discoveredTests.AddRange(_testBuilder.BuildTests(dynamicTest));
        }

        return discoveredTests.ToArray();
    }

    private async Task<DiscoveryResult> DiscoverStandardTestsAsync(HashSet<Type> allTypes)
    {
        return await Task.Run(async () =>
        {
            var allDefinitions = new List<ITestDefinition>();
            var allFailures = new List<DiscoveryFailure>();
            var testClasses = allTypes.Where(IsTestClass);

            foreach (var testClass in testClasses)
            {
                var testMethods = GetTestMethods(testClass);
                if (testMethods.Length == 0)
                {
                    continue;
                }

                var classInformation = ReflectionToSourceModelHelpers.GenerateClass(testClass);
                var methodInformations = testMethods
                    .Select(method => ReflectionToSourceModelHelpers.BuildTestMethod(classInformation, method, method.Name))
                    .ToArray();

                var testBuilder = new ReflectionTestConstructionBuilder();
                var result = await testBuilder.BuildTestsAsync(classInformation, methodInformations);
                allDefinitions.AddRange(result.TestDefinitions);
                allFailures.AddRange(result.DiscoveryFailures);
            }

            return new DiscoveryResult
            {
                TestDefinitions = allDefinitions,
                DiscoveryFailures = allFailures
            };
        });
    }

    private static bool IsTestClass(Type type)
    {
        if (!type.IsClass || type.IsAbstract)
        {
            return false;
        }

        // A test class must have at least one test method
        return GetTestMethods(type).Length > 0;
    }

    private static MethodInfo[] GetTestMethods(Type type)
    {
        return type.GetMethods()
            .Where(method => !method.IsAbstract && method.HasExactAttribute<TestAttribute>())
            .ToArray();
    }

    private async Task<List<DynamicTest>> DiscoverDynamicTestsAsync(HashSet<Type> allTypes)
    {
        return await Task.Run(() =>
        {
            var results = new List<DynamicTest>();

            foreach (var type in allTypes)
            {
                foreach (var method in type.GetMethods())
                {
                    var dynamicTestBuilderAttribute = method
                        .GetCustomAttributes<DynamicTestBuilderAttribute>()
                        .FirstOrDefault();

                    if (dynamicTestBuilderAttribute is null)
                    {
                        continue;
                    }

                    foreach (var test in BuildDynamicTests(type, method, dynamicTestBuilderAttribute))
                    {
                        results.Add(test);
                    }
                }
            }

            return results;
        });
    }

    private static IEnumerable<DynamicTest> BuildDynamicTests(
        Type type,
        MethodInfo method,
        DynamicTestBuilderAttribute attribute)
    {
        var context = new DynamicTestBuilderContext(attribute.File, attribute.Line);
        var instance = Activator.CreateInstance(type)!;

        method.Invoke(instance, [context]);

        return context.Tests;
    }
}
