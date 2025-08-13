using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core.Helpers;
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
        var parameters = context.TestDetails.MethodMetadata.Class.Parameters;

        if (parameters.Length == 0)
        {
            return context.TestDetails.ClassType.Name;
        }

        return $"{context.TestDetails.ClassType.Name}({string.Join(", ", context.TestDetails.TestClassArguments.Select(a => ArgumentFormatter.Format(a, context.ArgumentDisplayFormatters)))})";
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
