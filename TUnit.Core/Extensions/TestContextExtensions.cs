using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Extensions;

/// <summary>
/// Simplified extension methods for TestContext
/// </summary>
public static class TestContextExtensions
{
    /// <summary>
    /// Gets a service from the test context
    /// </summary>
    public static T? GetService<T>(this TestContext context) where T : class
    {
        return context.GetService<T>();
    }

    /// <summary>
    /// Gets the class type name
    /// </summary>
    public static string GetClassTypeName(this TestContext context)
    {
        return context.TestDetails.ClassType.Name;
    }

    /// <summary>
    /// Gets the test display name
    /// </summary>
    public static string GetDisplayName(this TestContext context)
    {
        return context.DisplayName;
    }

    /// <summary>
    /// Adds a dynamic test to the test context
    /// </summary>
    [RequiresDynamicCode("Uses MakeGenericMethod for dynamic test registration")]
    public static async Task AddDynamicTest<T>(this TestContext context, DynamicTestInstance<T> dynamicTest) where T : class
    {
        // Try to use the test registry if available
        try
        {
            var registryType = Type.GetType("TUnit.Engine.Services.TestRegistry, TUnit.Engine");
            if (registryType != null)
            {
                var instanceProperty = registryType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                var registry = instanceProperty?.GetValue(null);
                if (registry != null)
                {
                    var addDynamicTestMethod = registryType.GetMethod("AddDynamicTest");
                    if (addDynamicTestMethod != null)
                    {
                        var genericMethod = addDynamicTestMethod.MakeGenericMethod(typeof(T));
                        await (Task) genericMethod.Invoke(registry, [context, dynamicTest])!;
                        return;
                    }
                }
            }
        }
        catch
        {
            // If registry is not available, we can't add dynamic tests
            // Log this for debugging if needed
        }
    }

    /// <summary>
    /// Adds a test to the test context (synonym for AddDynamicTest)
    /// </summary>
    [RequiresDynamicCode("Uses MakeGenericMethod for dynamic test registration")]
    public static void AddTest<T>(this TestContext context, DynamicTestInstance<T> dynamicTest) where T : class
    {
        // Fire and forget - don't wait for completion
        _ = AddDynamicTest(context, dynamicTest);
    }
}
