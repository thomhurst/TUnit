using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
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
            testSessionId,
            testContext.Events,
            testContext.ObjectBag,
            testContext.TestDetails.MethodMetadata);
    }

    /// <summary>
    /// Checks if a ClassConstructor attribute is present and uses it to create an instance
    /// </summary>
    /// <param name="attributes">The attributes to check</param>
    /// <param name="testClassType">The type of the test class to create</param>
    /// <param name="testSessionId">The test session ID</param>
    /// <param name="events">The test context events</param>
    /// <param name="objectBag">The object bag</param>
    /// <param name="methodMetadata">The method metadata</param>
    /// <returns>The created instance, or null if no ClassConstructor attribute is found</returns>
    public static async Task<object?> TryCreateInstanceWithClassConstructor(
        IReadOnlyList<Attribute> attributes,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type testClassType,
        string testSessionId,
        TestContextEvents events,
        Dictionary<string, object?> objectBag,
        MethodMetadata methodMetadata)
    {
        var classConstructorAttribute = attributes.OfType<ClassConstructorAttribute>().FirstOrDefault();

        if (classConstructorAttribute == null)
        {
            return null;
        }

        // Use the ClassConstructor to create the instance
        var classConstructorType = classConstructorAttribute.ClassConstructorType;
        var classConstructor = (IClassConstructor)Activator.CreateInstance(classConstructorType)!;

        // Store the ClassConstructor instance in the ObjectBag so it can be used for event handling
        var objectBagKey = $"__ClassConstructor_{classConstructorType.FullName}";
        objectBag[objectBagKey] = classConstructor;

        var classConstructorMetadata = new ClassConstructorMetadata
        {
            TestSessionId = testSessionId,
            TestBuilderContext = new TestBuilderContext
            {
                Events = events,
                ObjectBag = objectBag,
                TestMetadata = methodMetadata
            }
        };

        return await classConstructor.Create(testClassType, classConstructorMetadata);
    }

    /// <summary>
    /// Checks if the given attributes contain a ClassConstructor attribute
    /// </summary>
    public static bool HasClassConstructorAttribute(Attribute[] attributes)
    {
        return attributes.OfType<ClassConstructorAttribute>().Any();
    }

    /// <summary>
    /// Gets the ClassConstructor attribute from the given attributes, if present
    /// </summary>
    public static ClassConstructorAttribute? GetClassConstructorAttribute(Attribute[] attributes)
    {
        return attributes.OfType<ClassConstructorAttribute>().FirstOrDefault();
    }
}
