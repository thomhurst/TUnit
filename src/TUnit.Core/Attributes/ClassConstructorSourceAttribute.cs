using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.Core;

/// <summary>
/// Specifies a custom <see cref="IClassConstructor"/> to use when creating test class instances.
/// This enables dependency injection and custom object creation for test classes.
/// </summary>
/// <remarks>
/// <para>
/// Can be applied at the class level (affecting only that class) or at the assembly level
/// (affecting all test classes in the assembly).
/// </para>
/// <para>
/// The specified type must implement <see cref="IClassConstructor"/> and have a parameterless constructor.
/// For strongly-typed usage, prefer <see cref="ClassConstructorAttribute{T}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [ClassConstructor&lt;DependencyInjectionClassConstructor&gt;]
/// public class MyTests
/// {
///     private readonly IMyService _service;
///
///     public MyTests(IMyService service)
///     {
///         _service = service;
///     }
///
///     [Test]
///     public void TestWithInjectedDependency() { }
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public class ClassConstructorAttribute : TUnitAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ClassConstructorAttribute"/> class.
    /// </summary>
    /// <param name="classConstructorType">The type that implements <see cref="IClassConstructor"/>.</param>
    public ClassConstructorAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
        Type classConstructorType)
    {
        ClassConstructorType = classConstructorType;
    }

    /// <summary>
    /// Gets or sets the type that implements <see cref="IClassConstructor"/> and is used to create test class instances.
    /// </summary>
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)]
    public Type ClassConstructorType { get; init; }
}

/// <summary>
/// Specifies a custom <see cref="IClassConstructor"/> to use when creating test class instances.
/// Generic version that provides compile-time type safety.
/// </summary>
/// <typeparam name="T">The type that implements <see cref="IClassConstructor"/>.</typeparam>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class)]
public sealed class ClassConstructorAttribute<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicMethods)] T>()
    : ClassConstructorAttribute(typeof(T))
    where T : IClassConstructor, new();
