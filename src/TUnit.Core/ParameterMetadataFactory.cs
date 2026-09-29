using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

/// <summary>
/// Factory for creating ParameterMetadata instances.
/// Replaces inline <c>new ParameterMetadata { ... }</c> object initializers in generated code,
/// reducing per-parameter IL size and JIT-compiled native code.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ParameterMetadataFactory
{
    internal const DynamicallyAccessedMemberTypes DeclaringTypeMembers =
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods
        | DynamicallyAccessedMemberTypes.NonPublicMethods;

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067",
        Justification = "Factory is only called from generated code that always passes concrete types")]
    public static ParameterMetadata Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
            | DynamicallyAccessedMemberTypes.NonPublicConstructors
            | DynamicallyAccessedMemberTypes.PublicProperties)]
        Type type,
        string name,
        TypeInfo typeInfo,
        bool isNullable,
        Func<ParameterInfo>? reflectionInfoFactory = null)
    {
        return new ParameterMetadata(type)
        {
            Name = name,
            TypeInfo = typeInfo,
            IsNullable = isNullable,
            ReflectionInfoFactory = reflectionInfoFactory,
        };
    }

    /// <summary>
    /// Attaches lazy <see cref="ParameterMetadata.ReflectionInfo"/> resolution to the parameters of a
    /// non-generic method. The method is looked up once, on first access, by name and the parameters' types.
    /// </summary>
    public static ParameterMetadata[] ForMethod(
        [DynamicallyAccessedMembers(DeclaringTypeMembers)] Type declaringType,
        string methodName,
        bool isStatic,
        params ParameterMetadata[] parameters)
    {
        return Attach(parameters, new ParameterInfoResolver(declaringType, methodName,
            isStatic ? ParameterInfoResolver.Kind.StaticMethod : ParameterInfoResolver.Kind.InstanceMethod, parameters));
    }

    /// <summary>
    /// Attaches lazy <see cref="ParameterMetadata.ReflectionInfo"/> resolution to the parameters of a
    /// generic method (or one whose parameters use type parameters). The method is looked up once, on first
    /// access, by name and parameter count.
    /// </summary>
    public static ParameterMetadata[] ForGenericMethod(
        [DynamicallyAccessedMembers(DeclaringTypeMembers)] Type declaringType,
        string methodName,
        params ParameterMetadata[] parameters)
    {
        return Attach(parameters, new ParameterInfoResolver(declaringType, methodName,
            ParameterInfoResolver.Kind.MethodByParameterCount, parameters));
    }

    /// <summary>
    /// Attaches lazy <see cref="ParameterMetadata.ReflectionInfo"/> resolution to the parameters of a public
    /// constructor. When <paramref name="matchByParameterCount"/> is true the constructor is matched by
    /// parameter count instead of parameter types.
    /// </summary>
    public static ParameterMetadata[] ForConstructor(
        [DynamicallyAccessedMembers(DeclaringTypeMembers)] Type declaringType,
        bool matchByParameterCount,
        params ParameterMetadata[] parameters)
    {
        return Attach(parameters, new ParameterInfoResolver(declaringType, null,
            matchByParameterCount ? ParameterInfoResolver.Kind.ConstructorByParameterCount : ParameterInfoResolver.Kind.Constructor, parameters));
    }

    private static ParameterMetadata[] Attach(ParameterMetadata[] parameters, ParameterInfoResolver resolver)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            parameters[i].ReflectionInfoResolver = resolver;
            parameters[i].ReflectionInfoIndex = i;
        }

        return parameters;
    }
}

/// <summary>
/// Lazily resolves the <see cref="ParameterInfo"/>s of one method or constructor and shares them
/// across all of its <see cref="ParameterMetadata"/> instances.
/// </summary>
internal sealed class ParameterInfoResolver
{
    internal enum Kind
    {
        InstanceMethod,
        StaticMethod,
        MethodByParameterCount,
        Constructor,
        ConstructorByParameterCount,
    }

    private const BindingFlags AllMethods = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    [DynamicallyAccessedMembers(ParameterMetadataFactory.DeclaringTypeMembers)]
    private readonly Type _declaringType;
    private readonly string? _methodName;
    private readonly Kind _kind;
    private readonly ParameterMetadata[] _parameters;
    private ParameterInfo[]? _resolved;

    public ParameterInfoResolver(
        [DynamicallyAccessedMembers(ParameterMetadataFactory.DeclaringTypeMembers)] Type declaringType,
        string? methodName,
        Kind kind,
        ParameterMetadata[] parameters)
    {
        _declaringType = declaringType;
        _methodName = methodName;
        _kind = kind;
        _parameters = parameters;
    }

    public ParameterInfo? Get(int index)
    {
        var resolved = _resolved ??= Resolve();
        return (uint) index < (uint) resolved.Length ? resolved[index] : null;
    }

    private ParameterInfo[] Resolve()
    {
        MethodBase? member = _kind switch
        {
            Kind.InstanceMethod => _declaringType.GetMethod(_methodName!, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance, null, GetParameterTypes(), null),
            Kind.StaticMethod => _declaringType.GetMethod(_methodName!, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, null, GetParameterTypes(), null),
            Kind.MethodByParameterCount => FindByParameterCount(_declaringType.GetMethods(AllMethods)),
            Kind.Constructor => _declaringType.GetConstructor(GetParameterTypes()),
            _ => FindByParameterCount(_declaringType.GetConstructors()),
        };

        return member?.GetParameters() ?? [];
    }

    private MethodBase? FindByParameterCount(MethodBase[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if ((_methodName is null || candidate.Name == _methodName)
                && candidate.GetParameters().Length == _parameters.Length)
            {
                return candidate;
            }
        }

        return null;
    }

    private Type[] GetParameterTypes()
    {
        var types = new Type[_parameters.Length];
        for (var i = 0; i < types.Length; i++)
        {
            types[i] = _parameters[i].Type;
        }

        return types;
    }
}
