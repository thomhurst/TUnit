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
    /// Finds a suitable constructor for a test class, preferring ones marked with [TestConstructor]
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements",
        Justification = "Constructor discovery is required for test instantiation. AOT scenarios should use source-generated test metadata.")]
    public static ConstructorInfo? FindSuitableConstructor(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type testClass,
        Attribute[] classAttributes)
    {
        var constructors = testClass.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

        // First, look for constructors marked with [TestConstructor]
        var testConstructorMarked = constructors.Where(c => c.GetCustomAttribute<TestConstructorAttribute>() != null).ToArray();
        
        if (testConstructorMarked.Length > 0)
        {
            return testConstructorMarked[0];
        }

        // If no [TestConstructor] attribute found, return the first instance constructor
        return constructors.FirstOrDefault();
    }

    /// <summary>
    /// Creates an instance of a test class with proper constructor parameter handling
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067:Target type's member does not satisfy annotation requirements",
        Justification = "Test class instantiation requires constructor access. AOT scenarios should use source-generated factories.")]
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements",
        Justification = "Dynamic property initialization is a fallback. AOT scenarios should use compile-time initialization.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target method return value does not satisfy annotation requirements",
        Justification = "Type flow in reflection mode cannot be statically analyzed. Use source generation for AOT.")]
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
                return CreateInstanceSafely(testClass);
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
            for (var i = 0; i < parameters.Length; i++)
            {
                var param = parameters[i];
                if (param.HasDefaultValue)
                {
                    defaultArgs[i] = param.DefaultValue;
                }
                else if (param.ParameterType.IsValueType)
                {
                    defaultArgs[i] = CreateInstanceSafely(param.ParameterType);
                }
                else
                {
                    // For reference types, try to create an instance
                    try
                    {
                        defaultArgs[i] = CreateInstanceSafely(param.ParameterType);
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


    /// <summary>
    /// Checks if a type has required properties that need initialization
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements",
        Justification = "Required property checking uses reflection. For AOT, ensure test classes don't use required properties or use source generation.")]
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
    [UnconditionalSuppressMessage("Trimming", "IL2070:Target method does not satisfy annotation requirements",
        Justification = "Required property initialization needs reflection. AOT scenarios should initialize properties in constructors.")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:Target method return value does not satisfy annotation requirements",
        Justification = "Property type information flows through reflection. Use explicit property initialization for AOT.")]
    public static void InitializeRequiredProperties(
        object instance,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            // In C# 11+, required properties are marked with RequiredMemberAttribute at the member level
            var isRequired = property.GetCustomAttribute<System.Runtime.CompilerServices.RequiredMemberAttribute>() != null ||
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
                            property.SetValue(instance, CreateInstanceSafely(property.PropertyType));
                        }
                        else
                        {
                            // For complex types, try to create an instance
                            try
                            {
                                var value = CreateInstanceSafely(property.PropertyType);
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
                            property.SetValue(instance, CreateInstanceSafely(property.PropertyType));
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
                                var value = CreateInstanceSafely(property.PropertyType);
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

    /// <summary>
    /// AOT-safe wrapper for Activator.CreateInstance with proper attribution
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2067:Target parameter does not satisfy annotation requirements",
        Justification = "Parameterless constructor invocation with preserved type. For full AOT support, use source-generated factories.")]
    private static object? CreateInstanceSafely([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
    {
        return Activator.CreateInstance(type);
    }
}
