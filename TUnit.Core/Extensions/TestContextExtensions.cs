using System.Diagnostics.CodeAnalysis;
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

        // Optimize: Use array instead of LINQ Select to reduce allocations
        var args = context.TestDetails.TestClassArguments;
        var formattedArgs = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            formattedArgs[i] = ArgumentFormatter.Format(args[i], context.ArgumentDisplayFormatters);
        }

        return $"{context.TestDetails.ClassType.Name}({string.Join(", ", formattedArgs)})";
    }

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Dynamic test metadata creation uses reflection")]
    #endif
    public static async Task AddDynamicTest<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicFields
        | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(this TestContext context, DynamicTest<T> dynamicTest) where T : class
    {
        await context.GetService<ITestRegistry>()!.AddDynamicTest(context, dynamicTest);;
    }
}
