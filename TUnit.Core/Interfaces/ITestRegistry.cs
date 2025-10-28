using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Interface for registering and managing dynamically added tests during runtime execution.
/// </summary>
public interface ITestRegistry
{
    /// <summary>
    /// Adds a dynamic test to be executed during the current test session.
    /// </summary>
    /// <typeparam name="T">The test class type</typeparam>
    /// <param name="context">The current test context</param>
    /// <param name="dynamicTest">The dynamic test instance to add</param>
    /// <returns>A task that completes when the test has been queued for execution</returns>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Adding dynamic tests requires runtime compilation and reflection which are not supported in native AOT scenarios.")]
    #endif
    Task AddDynamicTest<[DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.NonPublicConstructors
        | DynamicallyAccessedMemberTypes.PublicProperties
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicFields
        | DynamicallyAccessedMemberTypes.NonPublicFields)] T>(TestContext context, DynamicTest<T> dynamicTest)
        where T : class;

    /// <summary>
    /// Creates a new test variant based on the current test's template.
    /// The new test is queued for execution and will appear as a distinct test in the test explorer.
    /// This is the primary mechanism for implementing property-based test shrinking and retry logic.
    /// </summary>
    /// <param name="currentContext">The current test context to base the variant on</param>
    /// <param name="arguments">Method arguments for the variant (null to reuse current arguments)</param>
    /// <param name="properties">Key-value pairs for user-defined metadata (e.g., attempt count, custom data)</param>
    /// <param name="relationship">The relationship category of this variant to its parent test</param>
    /// <param name="displayName">Optional user-facing display name for the variant (e.g., "Shrink Attempt", "Mutant")</param>
    /// <returns>A task that completes when the variant has been queued</returns>
    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Creating test variants requires runtime compilation and reflection which are not supported in native AOT scenarios.")]
    #endif
    Task CreateTestVariant(
        TestContext currentContext,
        object?[]? arguments,
        Dictionary<string, object?>? properties,
        Enums.TestRelationship relationship,
        string? displayName);
}
