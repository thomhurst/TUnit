using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using TUnit.Core.Extensions;

namespace TUnit.Core.Helpers;

[RequiresDynamicCode("Reflection")]
[RequiresUnreferencedCode("Reflection")]
internal class ReflectionToSourceModelHelpers
{
    private static readonly ConcurrentDictionary<Assembly, AssemblyMetadata> _assemblyCache = new();
    private static readonly ConcurrentDictionary<Type, ClassMetadata> _classCache = new();
    private static readonly ConcurrentDictionary<MethodInfo, MethodMetadata> _methodCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, PropertyMetadata> _propertyCache = new();
    private static readonly ConcurrentDictionary<ParameterInfo, ParameterMetadata> _parameterCache = new();
    private static readonly ConcurrentDictionary<ConstructorInfo, ConstructorMetadata> _constructorCache = new();

    // Track types currently being processed to prevent infinite recursion
    [ThreadStatic]
    private static HashSet<Type>? _typesBeingProcessed;

    public static MethodMetadata BuildTestMethod([DynamicallyAccessedMembers(
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClassType, MethodInfo methodInfo, string? testName)
    {
        return BuildTestMethod(GenerateClass(testClassType), methodInfo, testName);
    }

    public static MethodMetadata BuildTestMethod(ClassMetadata classInformation, MethodInfo methodInfo, string? testName)
    {
        return _methodCache.GetOrAdd(methodInfo, _ => new MethodMetadata
        {
            Attributes = methodInfo.GetCustomAttributesSafe().ToArray(),
            Class = classInformation,
            Name = testName ?? methodInfo.Name,
            GenericTypeCount = methodInfo.IsGenericMethod ? methodInfo.GetGenericArguments().Length : 0,
            Parameters = GetParameters(methodInfo.GetParameters()),
            Type = classInformation.Type,
            ReflectionInformation = methodInfo,
            ReturnType = methodInfo.ReturnType
        });
    }

    public static ClassMetadata? GetParent(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.PublicMethods
            | DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
    {
        if (type.DeclaringType is null)
        {
            return null;
        }

        return GenerateClass(type.DeclaringType);
    }

    public static ClassMetadata GenerateClass(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)] Type testClassType)
    {
        return _classCache.GetOrAdd(testClassType, _ =>
        {
            // Initialize thread-local set if needed
            _typesBeingProcessed ??= new HashSet<Type>();

            // Check if we're already processing this type
            if (!_typesBeingProcessed.Add(testClassType))
            {
                // Return a minimal metadata to break the cycle
                return new ClassMetadata
                {
                    Parent = null,
                    Assembly = GenerateAssembly(testClassType),
                    Attributes = [],
                    Name = testClassType.GetFormattedName(),
                    Namespace = testClassType.Namespace,
                    Parameters = [],
                    Properties = [], // Empty to prevent recursion
                    Constructors = [], // Empty to prevent recursion
                    Type = testClassType
                };
            }

            try
            {
                return new ClassMetadata
                {
                    Parent = GetParent(testClassType),
                    Assembly = GenerateAssembly(testClassType),
                    Attributes = testClassType.GetCustomAttributesSafe().ToArray(),
                    Name = testClassType.GetFormattedName(),
                    Namespace = testClassType.Namespace,
                    Parameters = GetParameters(testClassType.GetConstructors().FirstOrDefault()?.GetParameters() ?? []).ToArray(),
                    Properties = testClassType.GetProperties().Select(GenerateProperty).ToArray(),
                    Constructors = GetConstructors(testClassType),
                    Type = testClassType
                };
            }
            finally
            {
                _typesBeingProcessed.Remove(testClassType);
            }
        });
    }

    public static AssemblyMetadata GenerateAssembly(Type testClassType)
    {
        return _assemblyCache.GetOrAdd(testClassType.Assembly, _ => new AssemblyMetadata
        {
            Attributes = testClassType.Assembly.GetCustomAttributesSafe().ToArray(),
            Name = testClassType.Assembly.GetName().Name ??
                   testClassType.Assembly.GetName().FullName,
        });
    }

    public static PropertyMetadata GenerateProperty(PropertyInfo property)
    {
        return _propertyCache.GetOrAdd(property, _ => new PropertyMetadata
        {
            Attributes = property.GetCustomAttributesSafe().ToArray(),
            ReflectionInfo = property,
            Name = property.Name,
            Type = property.PropertyType,
            IsStatic = property.GetMethod?.IsStatic is true
                || property.SetMethod?.IsStatic is true,
            Getter = property.GetValue,
            ClassMetadata = ShouldGenerateClassMetadataForType(property.PropertyType, property.GetCustomAttributesSafe().ToArray())
                ? GenerateClass(property.PropertyType)
                : null
        });
    }

    private static bool ShouldGenerateClassMetadataForType(Type type, Attribute[] propertyAttributes)
    {
        // Check if the type itself implements IDataAttribute
        if (typeof(IDataAttribute).IsAssignableFrom(type))
        {
            return true;
        }

        // Check if any of the property's attributes implement IDataAttribute
        if (propertyAttributes.Any(attr => attr is IDataAttribute))
        {
            return true;
        }

        return false;
    }

    public static ParameterMetadata[] GetParameters(ParameterInfo[] parameters)
    {
        return parameters.Select(GenerateParameter).ToArray();
    }

    public static ParameterMetadata GenerateParameter(ParameterInfo parameter)
    {
        return _parameterCache.GetOrAdd(parameter, _ => new ParameterMetadata(parameter.ParameterType)
        {
            Attributes = parameter.GetCustomAttributesSafe().ToArray(),
            Name = parameter.Name ?? string.Empty,
            ReflectionInfo = parameter,
        });
    }

    public static ConstructorMetadata[] GetConstructors(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
    {
        var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        return type.GetConstructors(bindingFlags).Select(GenerateConstructor).ToArray();
    }

    public static ConstructorMetadata GenerateConstructor(ConstructorInfo constructor)
    {
        return _constructorCache.GetOrAdd(constructor, _ => new ConstructorMetadata
        {
            Attributes = constructor.GetCustomAttributesSafe().ToArray(),
            Name = ".ctor",
            Type = constructor.DeclaringType!,
            Parameters = GetParameters(constructor.GetParameters()),
            IsStatic = constructor.IsStatic,
            IsPublic = constructor.IsPublic,
            IsPrivate = constructor.IsPrivate,
            IsProtected = constructor.IsFamily,
            IsInternal = constructor.IsAssembly,
            ReflectionInformation = constructor
        });
    }
}
