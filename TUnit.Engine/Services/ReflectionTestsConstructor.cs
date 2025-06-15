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
    IServiceProvider serviceProvider) : BaseTestsConstructor(extension, dependencyCollector, contextManager, serviceProvider)
{
    protected override DiscoveredTest[] DiscoverTests()
    {
#if NET
        if (!RuntimeFeature.IsDynamicCodeSupported)
        {
            throw new InvalidOperationException("Reflection tests are not supported with AOT or trimming enabled.");
        }
#endif

        var allTypes = ReflectionScanner.GetTypes();

        var standardTests = DiscoverStandardTests(allTypes).ToList();
        var dynamicTests = DiscoverDynamicTests(allTypes).ToList();

        var allDynamicTests = standardTests.Concat(dynamicTests).ToList();
        var discoveredTests = allDynamicTests.SelectMany(ConstructTests).ToArray();
        
        return discoveredTests;
    }

    private IEnumerable<DynamicTest> DiscoverStandardTests(HashSet<Type> allTypes)
    {
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
            foreach (var test in testBuilder.BuildTests(classInformation, methodInformations))
            {
                yield return test;
            }
        }
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

    private IEnumerable<DynamicTest> DiscoverDynamicTests(HashSet<Type> allTypes)
    {
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
                    yield return test;
                }
            }
        }
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
