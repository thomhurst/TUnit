﻿using System.Diagnostics.CodeAnalysis;
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
        var parameters = context.Metadata.TestDetails.MethodMetadata.Class.Parameters;

        if (parameters.Length == 0)
        {
            return context.Metadata.TestDetails.ClassType.Name;
        }

        // Optimize: Use array instead of LINQ Select to reduce allocations
        var args = context.Metadata.TestDetails.TestClassArguments;
        var formattedArgs = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            formattedArgs[i] = ArgumentFormatter.Format(args[i], context.ArgumentDisplayFormatters);
        }

        return $"{context.Metadata.TestDetails.ClassType.Name}({string.Join(", ", formattedArgs)})";
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

    /// <summary>
    /// Creates a new test variant based on the current test's template.
    /// The new test is queued for execution and will appear as a distinct test in the test explorer.
    /// This is the primary mechanism for implementing property-based test shrinking and retry logic.
    /// </summary>
    /// <param name="context">The current test context</param>
    /// <param name="arguments">Method arguments for the variant (null to reuse current arguments)</param>
    /// <param name="properties">Key-value pairs for user-defined metadata (e.g., attempt count, custom data)</param>
    /// <param name="relationship">The relationship category of this variant to its parent test (defaults to Derived)</param>
    /// <param name="displayName">Optional user-facing display name for the variant (e.g., "Shrink Attempt", "Mutant")</param>
    /// <returns>A task that completes when the variant has been queued</returns>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Creating test variants requires runtime compilation and reflection")]
    #endif
    public static async Task CreateTestVariant(
        this TestContext context,
        object?[]? arguments = null,
        Dictionary<string, object?>? properties = null,
        Enums.TestRelationship relationship = Enums.TestRelationship.Derived,
        string? displayName = null)
    {
        await context.GetService<ITestRegistry>()!.CreateTestVariant(context, arguments, properties, relationship, displayName);
    }
}
