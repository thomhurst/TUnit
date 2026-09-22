using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core.Helpers;

/// <summary>
/// Helper class for handling ClassConstructor attribute logic in a centralized way
/// </summary>
public static class ClassConstructorHelper
{
    /// <summary>
    /// Checks if a ClassConstructor attribute is present and uses it to create an instance
    /// </summary>
    /// <param name="attributes">The attributes to check</param>
    /// <param name="testClassType">The type of the test class to create</param>
    /// <param name="testSessionId">The test session ID</param>
    /// <param name="testContext">The test context</param>
    /// <returns>The created instance, or null if no ClassConstructor attribute is found</returns>
    public static async Task<object?> TryCreateInstanceWithClassConstructor(
        IReadOnlyList<Attribute> attributes,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type testClassType,
        string testSessionId,
        TestContext testContext)
    {
        return await TryCreateInstanceWithClassConstructor(
            attributes,
            testClassType,
            TestBuilderContext.FromTestContext(testContext, GetDataSourceAttribute(attributes)),
            testSessionId);
    }

    /// <summary>
    /// Checks if a ClassConstructor attribute is present and uses it to create an instance
    /// </summary>
    /// <param name="attributes">The attributes to check</param>
    /// <param name="testClassType">The type of the test class to create</param>
    /// <param name="testSessionId">The test session ID</param>
    /// <param name="testBuilderContext">The testBuilderContext</param>
    /// <returns>The created instance, or null if no ClassConstructor attribute is found</returns>
    public static async Task<object?> TryCreateInstanceWithClassConstructor(
        IReadOnlyList<Attribute> attributes,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type testClassType,
        TestBuilderContext testBuilderContext,
        string testSessionId)
    {
        var classConstructorAttribute = GetClassConstructorAttribute(attributes);

        if (classConstructorAttribute == null)
        {
            return null;
        }

        // Reuse existing ClassConstructor if already set, otherwise create new instance
        var classConstructor = testBuilderContext.ClassConstructor
            ?? (IClassConstructor)Activator.CreateInstance(classConstructorAttribute.ClassConstructorType)!;

        testBuilderContext.ClassConstructor = classConstructor;

        var classConstructorMetadata = new ClassConstructorMetadata
        {
            TestSessionId = testSessionId,
            TestBuilderContext = testBuilderContext
        };

        return await classConstructor.Create(testClassType, classConstructorMetadata);
    }

    /// <summary>
    /// Checks if the given attributes contain a ClassConstructor attribute
    /// </summary>
    public static bool HasClassConstructorAttribute(Attribute[] attributes)
    {
        foreach (Attribute attribute in attributes)
        {
            if (attribute is ClassConstructorAttribute)
            {
                return true;
            }
        }

        return false;
    }

    private static IDataSourceAttribute? GetDataSourceAttribute(IReadOnlyList<Attribute> attributes)
    {
        foreach (Attribute attribute in attributes)
        {
            if (attribute is IDataSourceAttribute dataSourceAttribute)
            {
                return dataSourceAttribute;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the ClassConstructor attribute from the given attributes, if present
    /// </summary>
    public static ClassConstructorAttribute? GetClassConstructorAttribute(Attribute[] attributes)
    {
        return GetClassConstructorAttribute((IReadOnlyList<ClassConstructorAttribute>)attributes);
    }

    private static ClassConstructorAttribute? GetClassConstructorAttribute(IReadOnlyList<Attribute> attributes)
    {
        // Indexed loop: foreach over the interface would box an enumerator on every instance creation.
        for (var i = 0; i < attributes.Count; i++)
        {
            if (attributes[i] is ClassConstructorAttribute classAttribute)
            {
                return classAttribute;
            }
        }

        return null;
    }
}
