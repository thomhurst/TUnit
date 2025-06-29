using System.Reflection;

namespace TUnit.Core.Interfaces;

/// <summary>
/// Factory for creating test class instances and invoking test methods.
/// Uses simple reflection without expression compilation for maintainability.
/// </summary>
public interface ITestInstanceFactory
{
    /// <summary>
    /// Creates an instance of the specified type with the given constructor arguments.
    /// </summary>
    /// <param name="type">The type to instantiate</param>
    /// <param name="args">Constructor arguments</param>
    /// <returns>The created instance</returns>
    Task<object> CreateInstanceAsync(Type type, object?[] args);

    /// <summary>
    /// Invokes a method on an instance with the given arguments.
    /// </summary>
    /// <param name="instance">The instance to invoke the method on</param>
    /// <param name="method">The method to invoke</param>
    /// <param name="args">Method arguments</param>
    /// <returns>The result of the method invocation</returns>
    Task<object?> InvokeMethodAsync(object instance, MethodInfo method, object?[] args);

    /// <summary>
    /// Sets a property value on an instance.
    /// </summary>
    /// <param name="instance">The instance to set the property on</param>
    /// <param name="property">The property to set</param>
    /// <param name="value">The value to set</param>
    Task SetPropertyAsync(object instance, PropertyInfo property, object? value);
}
