using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Discovery;

/// <summary>
/// Helper for handling constructor discovery and instantiation in reflection mode
/// </summary>
internal static class ConstructorHelper
{
    /// <summary>
    /// Finds a suitable constructor for a test class, preferring parameterless but handling class data sources
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    public static ConstructorInfo? FindSuitableConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type testClass,
        Attribute[] classAttributes)
    {
        var constructors = testClass.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        
        // First, try to find a parameterless constructor
        var parameterless = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
        if (parameterless != null)
        {
            return parameterless;
        }
        
        // If no parameterless constructor, look for one that matches class data sources
        var classDataSources = classAttributes.OfType<ClassDataSourceAttribute>().ToArray();
        if (classDataSources.Length > 0)
        {
            // Find constructor with parameters matching the class data sources
            foreach (var constructor in constructors)
            {
                var parameters = constructor.GetParameters();
                if (CanMatchClassDataSources(parameters, classDataSources))
                {
                    return constructor;
                }
            }
        }
        
        // If still no match, return the constructor with the fewest parameters
        return constructors.OrderBy(c => c.GetParameters().Length).FirstOrDefault();
    }
    
    /// <summary>
    /// Creates an instance of a test class with proper constructor parameter handling
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067:Target type's member does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target method return value does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    public static object? CreateTestClassInstanceWithConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type testClass,
        ConstructorInfo? constructor,
        object?[]? constructorArgs = null)
    {
        try
        {
            if (testClass.IsAbstract)
            {
                return null;
            }
            
            if (constructor == null)
            {
                // Try Activator.CreateInstance as last resort
                return Activator.CreateInstance(testClass);
            }
            
            var parameters = constructor.GetParameters();
            
            // If we have constructor args, use them
            if (constructorArgs != null && constructorArgs.Length == parameters.Length)
            {
                return constructor.Invoke(constructorArgs);
            }
            
            // If parameterless, invoke with no args
            if (parameters.Length == 0)
            {
                return constructor.Invoke(null);
            }
            
            // Try to create default values for parameters
            var defaultArgs = new object?[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (param.HasDefaultValue)
                {
                    defaultArgs[i] = param.DefaultValue;
                }
                else if (param.ParameterType.IsValueType)
                {
                    #pragma warning disable IL2072
                    defaultArgs[i] = Activator.CreateInstance(param.ParameterType);
                    #pragma warning restore IL2072
                }
                else
                {
                    // For reference types, try to create an instance
                    try
                    {
                        #pragma warning disable IL2072
                        defaultArgs[i] = Activator.CreateInstance(param.ParameterType);
                        #pragma warning restore IL2072
                    }
                    catch
                    {
                        defaultArgs[i] = null;
                    }
                }
            }
            
            return constructor.Invoke(defaultArgs);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot create instance of '{testClass.FullName}'. " +
                $"Constructor: {constructor?.ToString() ?? "none"}. " +
                $"Error: {ex.Message}", ex);
        }
    }
    
    private static bool CanMatchClassDataSources(ParameterInfo[] parameters, ClassDataSourceAttribute[] classDataSources)
    {
        // Simple check - if parameter count matches and types are compatible
        if (parameters.Length != classDataSources.Length)
        {
            return false;
        }
        
        for (int i = 0; i < parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            // Get the type from the generic attribute if possible
            var dataSourceType = GetClassDataSourceType(classDataSources[i]);
            
            if (dataSourceType != null && !paramType.IsAssignableFrom(dataSourceType))
            {
                return false;
            }
        }
        
        return true;
    }
    
    private static Type? GetClassDataSourceType(ClassDataSourceAttribute attr)
    {
        // Check if it's a generic ClassDataSourceAttribute<T>
        var attrType = attr.GetType();
        if (attrType.IsGenericType)
        {
            var genericArgs = attrType.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return genericArgs[0];
            }
        }
        
        // For non-generic ClassDataSourceAttribute, we can't determine the type
        return null;
    }
    
    /// <summary>
    /// Checks if a type has required properties that need initialization
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    public static bool HasRequiredProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        // Check if the type itself has RequiredMemberAttribute (indicates it has required properties)
        if (type.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null)
        {
            return true;
        }
        
        // Also check individual properties for RequiredAttribute (older approach)
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        return properties.Any(p => p.GetCustomAttributes().Any(a => a.GetType().Name == "RequiredAttribute"));
    }
    
    /// <summary>
    /// Tries to initialize required properties on an instance
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target method return value does not satisfy annotation requirements", Justification = "Reflection mode requires dynamic access")]
    public static void InitializeRequiredProperties(
        object instance,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        
        foreach (var property in properties)
        {
            // In C# 11+, required properties are marked with RequiredMemberAttribute at the member level
            bool isRequired = property.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null ||
                            property.GetCustomAttributes().Any(a => a.GetType().Name == "RequiredAttribute");
            
            // Also check if property is marked with 'required' modifier by checking if it's init-only and the type has RequiredMemberAttribute
            if (!isRequired && property is { CanWrite: true, SetMethod.IsSpecialName: true } && 
                property.SetMethod.Name.StartsWith("set_") && 
                type.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null)
            {
                // This is likely a required init property
                isRequired = true;
            }
            
            if (isRequired && property.CanWrite)
            {
                try
                {
                    // Check if property has a data source attribute
                    var dataSourceAttr = property.GetCustomAttributes()
                        .FirstOrDefault(a => a.GetType().Name.Contains("DataSource") || 
                                           a.GetType().Name.Contains("Arguments") ||
                                           a.GetType().Name.Contains("Generator"));
                    
                    if (dataSourceAttr != null)
                    {
                        // For properties with data source attributes, create placeholder values
                        if (property.PropertyType == typeof(string))
                        {
                            property.SetValue(instance, "<data>");
                        }
                        else if (property.PropertyType.IsValueType)
                        {
                            #pragma warning disable IL2072
                            property.SetValue(instance, Activator.CreateInstance(property.PropertyType));
                            #pragma warning restore IL2072
                        }
                        else
                        {
                            // For complex types, try to create an instance
                            try
                            {
                                #pragma warning disable IL2072
                                var value = Activator.CreateInstance(property.PropertyType);
                                #pragma warning restore IL2072
                                property.SetValue(instance, value);
                            }
                            catch
                            {
                                // If we can't create, skip - the data source will provide the value
                            }
                        }
                    }
                    else
                    {
                        // For regular required properties, set default values
                        if (property.PropertyType.IsValueType)
                        {
                            #pragma warning disable IL2072
                            property.SetValue(instance, Activator.CreateInstance(property.PropertyType));
                            #pragma warning restore IL2072
                        }
                        else if (property.PropertyType == typeof(string))
                        {
                            property.SetValue(instance, string.Empty);
                        }
                        else
                        {
                            // For reference types, try to create an instance or set null
                            try
                            {
                                #pragma warning disable IL2072
                                var value = Activator.CreateInstance(property.PropertyType);
                                #pragma warning restore IL2072
                                property.SetValue(instance, value);
                            }
                            catch
                            {
                                // If we can't create an instance, the property might accept null
                                property.SetValue(instance, null);
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore property initialization failures during discovery
                }
            }
        }
    }
}