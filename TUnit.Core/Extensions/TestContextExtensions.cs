using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Interfaces;

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

    public static async Task AddDynamicTest<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicFields
        | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(this TestContext context, DynamicTestInstance<T> dynamicTest) where T : class
    {
        await context.GetService<ITestRegistry>()!.AddDynamicTest(context, dynamicTest);;
    }
}
