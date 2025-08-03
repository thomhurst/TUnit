using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Helper for handling generic test classes and methods in reflection mode
/// </summary>
internal static class GenericTestHelper
{
    /// <summary>
    /// Safely creates an instance of a test class, handling generic types
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067:Target type's member does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    public static object? CreateTestClassInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClass)
    {
        try
        {
            if (testClass.IsAbstract)
            {
                return null;
            }
            
            // For generic types, ensure they are constructed
            if (testClass.IsGenericTypeDefinition)
            {
                throw new InvalidOperationException($"Cannot create instance of generic type definition {testClass.Name}. Generic type must be constructed with specific type arguments.");
            }
            
            var classAttributes = testClass.GetCustomAttributes().ToArray();
            
            // Find a suitable constructor
            var constructor = ConstructorHelper.FindSuitableConstructor(testClass, classAttributes);
            
            var instance = ConstructorHelper.CreateTestClassInstanceWithConstructor(testClass, constructor);
            
            if (instance != null && ConstructorHelper.HasRequiredProperties(testClass))
            {
                ConstructorHelper.InitializeRequiredProperties(instance, testClass);
            }
            
            return instance;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot create instance of '{testClass.FullName}'. " +
                $"Error: {ex.Message}", ex);
        }
    }
    
    /// <summary>
    /// Gets the method on the actual implementation class, handling inherited generic methods
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2075:Target method return value does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target parameter argument does not satisfy annotation requirements", Justification = "BaseType access in reflection mode requires dynamic access")]
    public static MethodInfo? GetMethodOnImplementationType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type implementationType, string methodName, Type[] parameterTypes)
    {
        // First try exact match on implementation type
        var method = implementationType.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            null,
            parameterTypes,
            null);
            
        if (method != null)
        {
            return method;
        }
        
        // If not found, look for the method in base types
        var currentType = implementationType.BaseType;
        while (currentType != null)
        {
            method = GetMethodFromType(currentType, methodName, parameterTypes);
                
            if (method != null)
            {
                // If found in base type and it's virtual/abstract, get the implementation
                if (method is { IsVirtual: true, IsFinal: false })
                {
                    // Try to get the override in the implementation type
                    var overrideMethod = GetMethodFromType(implementationType, methodName, 
                        method.GetParameters().Select(p => p.ParameterType).ToArray());
                        
                    if (overrideMethod != null)
                    {
                        return overrideMethod;
                    }
                }
                
                return method;
            }
            
            currentType = currentType.BaseType;
        }
        
        return null;
    }
    
    /// <summary>
    /// Checks if a method is inherited from a generic base class
    /// </summary>
    public static bool IsInheritedFromGenericBase(MethodInfo method)
    {
        var declaringType = method.DeclaringType;
        if (declaringType == null)
        {
            return false;
        }

        // Check if any base type is generic
        var currentType = declaringType.BaseType;
        while (currentType != null)
        {
            if (currentType.IsGenericType)
            {
                return true;
            }
            currentType = currentType.BaseType;
        }
        
        return false;
    }
    
    /// <summary>
    /// Gets concrete type arguments for a generic base class from an implementation type
    /// </summary>
    public static Type[]? GetGenericArgumentsFromBase(Type implementationType, Type genericBaseType)
    {
        var currentType = implementationType;
        
        while (currentType != null)
        {
            if (currentType.IsGenericType)
            {
                var genericTypeDef = currentType.GetGenericTypeDefinition();
                if (genericTypeDef == genericBaseType)
                {
                    return currentType.GetGenericArguments();
                }
            }
            
            // Check if base type matches
            if (currentType.BaseType is { IsGenericType: true })
            {
                var baseGenericTypeDef = currentType.BaseType.GetGenericTypeDefinition();
                if (baseGenericTypeDef == genericBaseType)
                {
                    return currentType.BaseType.GetGenericArguments();
                }
            }
            
            currentType = currentType.BaseType;
        }
        
        return null;
    }
    
    /// <summary>
    /// Helper method to get method from type with proper AOT attribution
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    private static MethodInfo? GetMethodFromType(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type,
        string methodName,
        Type[] parameterTypes)
    {
        return type.GetMethod(
            methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
            null,
            parameterTypes,
            null);
    }
}