using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Testing.Platform.Extensions;
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
    private readonly UnifiedTestBuilder _unifiedBuilder = new(contextManager, serviceProvider);
    
    protected override async Task<DiscoveredTest[]> DiscoverTestsAsync()
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException("Reflection tests are not supported with AOT or trimming enabled.");
        }
#endif

        var allTypes = ReflectionScanner.GetTypes();

        var standardTests = await DiscoverStandardTestsAsync(allTypes);
        var dynamicTests = await DiscoverDynamicTestsAsync(allTypes);

        var allDynamicTests = standardTests.Concat(dynamicTests).ToList();
        var discoveredTests = allDynamicTests.SelectMany(_unifiedBuilder.BuildTests).ToArray();
        
        return discoveredTests;
    }

    private async Task<List<DynamicTest>> DiscoverStandardTestsAsync(HashSet<Type> allTypes)
    {
        return await Task.Run(async () =>
        {
            var results = new List<DynamicTest>();
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

                var testBuilder = new TestBuilder();
                var tests = await testBuilder.BuildTestsAsync(classInformation, methodInformations);
                foreach (var test in tests)
                {
                    results.Add(test);
                }
            }
            
            return results;
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
