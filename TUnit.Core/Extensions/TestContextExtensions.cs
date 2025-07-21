using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core.Extensions;

public static class TestContextExtensions
{
    public static T? GetService<T>(this TestContext context) where T : class
    {
        return context.GetService<T>();
    }

    public static string GetClassTypeName(this TestContext context)
    {
        return context.TestDetails.ClassType.Name;
    }

    [RequiresDynamicCode("Uses MakeGenericMethod for dynamic test registration")]
    public static async Task AddDynamicTest<T>(this TestContext context, DynamicTestInstance<T> dynamicTest) where T : class
    {
        // Try to use the test registry if available
        try
        {
            var registryType = Type.GetType("TUnit.Engine.Services.TestRegistry, TUnit.Engine");
            if (registryType != null)
            {
                var instanceProperty = registryType.GetProperty("Instance", BindingFlags.Public | BindingFlags.Static);
                var registry = instanceProperty?.GetValue(null);
                if (registry != null)
                {
                    var addDynamicTestMethod = registryType.GetMethod("AddDynamicTest");
                    if (addDynamicTestMethod != null)
                    {
                        var genericMethod = addDynamicTestMethod.MakeGenericMethod(typeof(T));
                        await (Task) genericMethod.Invoke(registry, [context, dynamicTest])!;
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

    [RequiresDynamicCode("Uses MakeGenericMethod for dynamic test registration")]
    public static void AddTest<T>(this TestContext context, DynamicTestInstance<T> dynamicTest) where T : class
    {
        // Fire and forget - don't wait for completion
        _ = AddDynamicTest(context, dynamicTest);
    }
}
