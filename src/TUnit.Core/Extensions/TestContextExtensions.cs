using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Extensions;

public static class TestContextExtensions
{
    public static string GetClassTypeName(this TestContext context)
    {
        var type = context.Metadata.TestDetails.ClassType;
        var parameters = context.Metadata.TestDetails.MethodMetadata.Class.Parameters;

        var nestedPrefix = GetNestedTypePrefix(type);

        if (parameters.Length == 0)
        {
            return nestedPrefix != null
                ? $"{nestedPrefix}+{type.Name}"
                : type.Name;
        }

        var args = context.Metadata.TestDetails.TestClassArguments;
        var sb = StringBuilderPool.Get();
        try
        {
            if (nestedPrefix != null)
            {
                sb.Append(nestedPrefix);
                sb.Append('+');
            }

            sb.Append(type.Name);
            sb.Append('(');

            for (var i = 0; i < args.Length; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(ArgumentFormatter.Format(args[i], context.ArgumentDisplayFormatters));
            }

            sb.Append(')');
            return sb.ToString();
        }
        finally
        {
            StringBuilderPool.Return(sb);
        }
    }

    /// <summary>
    /// Gets the nested type prefix (outer class names joined by '+') for a type, or null if not nested.
    /// For example, for OuterClass+MiddleClass+InnerClass, returns "OuterClass+MiddleClass".
    /// </summary>
    internal static string? GetNestedTypePrefix(Type type)
    {
        if (type.DeclaringType == null)
        {
            return null;
        }

        // Walk the declaring type chain and build the hierarchy
        var hierarchy = new List<string>();
        var current = type.DeclaringType;
        while (current != null)
        {
            hierarchy.Add(current.Name);
            current = current.DeclaringType;
        }

        hierarchy.Reverse();
        return string.Join('+', hierarchy);
    }

    /// <summary>
    /// Gets the full nested type name with '+' separator (matching .NET Type.FullName convention for nested types).
    /// For example: OuterClass+InnerClass
    /// </summary>
    internal static string GetNestedTypeName(Type type)
    {
        var prefix = GetNestedTypePrefix(type);
        return prefix != null ? $"{prefix}+{type.Name}" : type.Name;
    }

    /// <summary>
    /// Gets whether this test is a variant created at runtime via <see cref="CreateTestVariant"/>.
    /// Use this to guard against infinite recursion when a test creates variants of itself.
    /// </summary>
    public static bool IsVariant(this Interfaces.ITestDependencies dependencies) => dependencies.ParentTestId != null;

    #if NET8_0_OR_GREATER
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
        await context.Services.GetService<ITestRegistry>()!.AddDynamicTest(context, dynamicTest);;
    }

    /// <summary>
    /// Creates a new test variant based on the current test's template.
    /// The new test is queued for execution and will appear as a distinct test in the test explorer.
    /// This is the primary mechanism for implementing property-based test shrinking and retry logic.
    /// </summary>
    /// <param name="context">The current test context</param>
    /// <param name="methodArguments">Method arguments for the variant (null to reuse current arguments)</param>
    /// <param name="classArguments">Constructor arguments for the variant's test class (null to reuse current arguments)</param>
    /// <param name="properties">Key-value pairs for user-defined metadata (e.g., attempt count, custom data)</param>
    /// <param name="relationship">The relationship category of this variant to its parent test (defaults to Derived)</param>
    /// <param name="displayName">Optional user-facing display name for the variant (e.g., "Shrink Attempt", "Mutant")</param>
    /// <returns>Information about the created test variant, including its TestId and DisplayName</returns>
    #if NET8_0_OR_GREATER
    [RequiresUnreferencedCode("Creating test variants requires runtime compilation and reflection")]
    #endif
    public static async Task<TestVariantInfo> CreateTestVariant(
        this TestContext context,
        object?[]? methodArguments = null,
        object?[]? classArguments = null,
        IReadOnlyDictionary<string, object?>? properties = null,
        Enums.TestRelationship relationship = Enums.TestRelationship.Derived,
        string? displayName = null)
    {
        return await context.Services.GetService<ITestRegistry>()!.CreateTestVariant(context, methodArguments, classArguments, properties, relationship, displayName);
    }
}
