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
    /// <remarks>
    /// Deliberately limited to what <see cref="ClassMetadata.Type"/> already keeps for a test class. Requesting
    /// non-public methods here would keep every private helper of the class and report IL2111 for any helper
    /// with <see cref="DynamicallyAccessedMembersAttribute"/> parameters. Non-public test methods go through
    /// <see cref="ForNonPublicMethod"/>, whose generated root keeps only that method.
    /// </remarks>
    internal const DynamicallyAccessedMemberTypes DeclaringTypeMembers =
        DynamicallyAccessedMemberTypes.PublicConstructors
        | DynamicallyAccessedMemberTypes.PublicMethods;

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
    /// non-generic public method. The method is looked up once, on first access, by name and the parameters' types.
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
    /// generic public method (or one whose parameters use type parameters). The method is looked up once, on first
    /// access, by name and parameter shape; with overloads of the same shape the first match reflection returns wins.
    /// </summary>
    public static ParameterMetadata[] ForGenericMethod(
        [DynamicallyAccessedMembers(DeclaringTypeMembers)] Type declaringType,
        string methodName,
        params ParameterMetadata[] parameters)
    {
        return Attach(parameters, new ParameterInfoResolver(declaringType, methodName,
            ParameterInfoResolver.Kind.MethodByShape, parameters));
    }

    /// <summary>
    /// Attaches lazy <see cref="ParameterMetadata.ReflectionInfo"/> resolution to the parameters of a
    /// generic public method (or one whose parameters use type parameters). The method is looked up once, on first
    /// access, by name, static-ness, generic arity and parameter shape.
    /// </summary>
    public static ParameterMetadata[] ForGenericMethod(
        [DynamicallyAccessedMembers(DeclaringTypeMembers)] Type declaringType,
        string methodName,
        bool isStatic,
        int genericParameterCount,
        params ParameterMetadata[] parameters)
    {
        return Attach(parameters, new ParameterInfoResolver(declaringType, methodName,
            ParameterInfoResolver.Kind.MethodByShape, parameters, isStatic, genericParameterCount));
    }

    /// <summary>
    /// Attaches lazy <see cref="ParameterMetadata.ReflectionInfo"/> resolution to the parameters of a non-public
    /// method. The method is looked up once, on first access, by name, static-ness, generic arity and parameter shape.
    /// </summary>
    /// <param name="rootMethod">
    /// Never invoked. Generated code passes a no-op delegate carrying
    /// <see cref="DynamicDependencyAttribute"/> with the method's exact signature, which is what keeps that one
    /// method (and no other member) available to reflection under trimming.
    /// </param>
    public static ParameterMetadata[] ForNonPublicMethod(
        Type declaringType,
        string methodName,
        bool isStatic,
        int genericParameterCount,
        Action rootMethod,
        params ParameterMetadata[] parameters)
    {
        return Attach(parameters, new ParameterInfoResolver(declaringType, methodName, isStatic, genericParameterCount, parameters));
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

    /// <remarks>
    /// Mutates the supplied instances in place and binds each one to its position in <paramref name="parameters"/>.
    /// The array and its elements must belong to exactly one method or constructor and must not be reordered or
    /// reused afterwards; generated code always passes a fresh array per member.
    /// </remarks>
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
        MethodByShape,
        Constructor,
        ConstructorByParameterCount,
        NonPublicMethod,
    }

    private const BindingFlags PublicMethods = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
    private const BindingFlags NonPublicMethods = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

    [DynamicallyAccessedMembers(ParameterMetadataFactory.DeclaringTypeMembers)]
    private readonly Type? _declaringType;

    // Declaring type of a Kind.NonPublicMethod resolver. It carries no annotation: a generated
    // [DynamicDependency] keeps the method under trimming.
    private readonly Type? _nonPublicDeclaringType;
    private readonly string? _methodName;
    private readonly Kind _kind;
    private readonly bool? _isStatic;
    private readonly int _genericParameterCount;
    private readonly ParameterMetadata[] _parameters;
    private ParameterInfo[]? _resolved;

    public ParameterInfoResolver(
        [DynamicallyAccessedMembers(ParameterMetadataFactory.DeclaringTypeMembers)] Type declaringType,
        string? methodName,
        Kind kind,
        ParameterMetadata[] parameters,
        bool? isStatic = null,
        int genericParameterCount = -1)
    {
        _declaringType = declaringType;
        _methodName = methodName;
        _kind = kind;
        _parameters = parameters;
        _isStatic = isStatic;
        _genericParameterCount = genericParameterCount;
    }

    public ParameterInfoResolver(Type declaringType, string methodName, bool isStatic, int genericParameterCount,
        ParameterMetadata[] parameters)
    {
        _nonPublicDeclaringType = declaringType;
        _methodName = methodName;
        _kind = Kind.NonPublicMethod;
        _isStatic = isStatic;
        _genericParameterCount = genericParameterCount;
        _parameters = parameters;
    }

    public ParameterInfo? Get(int index)
    {
        // Benign race: concurrent first accesses may each run Resolve(), but the lookup is idempotent and
        // reference assignment is atomic, so every caller observes an equivalent ParameterInfo[].
        var resolved = _resolved ??= Resolve();
        return (uint) index < (uint) resolved.Length ? resolved[index] : null;
    }

    internal string Describe()
    {
        return _kind switch
        {
            Kind.InstanceMethod => $"instance method '{_declaringType!.FullName}.{_methodName}' with {_parameters.Length} parameter(s) matched by parameter types",
            Kind.StaticMethod => $"static method '{_declaringType!.FullName}.{_methodName}' with {_parameters.Length} parameter(s) matched by parameter types",
            Kind.MethodByShape => $"method '{_declaringType!.FullName}.{_methodName}' with {_parameters.Length} parameter(s) matched by parameter shape",
            Kind.Constructor => $"constructor of '{_declaringType!.FullName}' with {_parameters.Length} parameter(s) matched by parameter types",
            Kind.ConstructorByParameterCount => $"constructor of '{_declaringType!.FullName}' matched by parameter count ({_parameters.Length})",
            _ => $"{(_isStatic == true ? "static" : "instance")} method '{_nonPublicDeclaringType!.FullName}.{_methodName}' with {_parameters.Length} parameter(s)",
        };
    }

    private ParameterInfo[] Resolve()
    {
        MethodBase? member = _kind switch
        {
            Kind.InstanceMethod => _declaringType!.GetMethod(_methodName!, BindingFlags.Public | BindingFlags.Instance, null, GetParameterTypes(), null)
                ?? FindNonPublicMethod(),
            Kind.StaticMethod => _declaringType!.GetMethod(_methodName!, BindingFlags.Public | BindingFlags.Static, null, GetParameterTypes(), null)
                ?? FindNonPublicMethod(),
            Kind.MethodByShape => FindBestMatch(_declaringType!.GetMethods(PublicMethods)) ?? FindNonPublicMethod(),
            Kind.Constructor => _declaringType!.GetConstructor(GetParameterTypes()),
            Kind.ConstructorByParameterCount => FindBestMatch(_declaringType!.GetConstructors()),
            _ => FindDeclaredNonPublicMethod(),
        };

        return member?.GetParameters() ?? [];
    }

    [UnconditionalSuppressMessage("Trimming", "IL2080",
        Justification = "Generated code roots the method with [DynamicDependency] on the delegate passed to " +
                        "ForNonPublicMethod. Methods trimming removed are simply not candidates.")]
    private MethodBase? FindDeclaredNonPublicMethod()
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | (_isStatic == true ? BindingFlags.Static : BindingFlags.Instance);
        return FindBestMatch(_nonPublicDeclaringType!.GetMethods(flags));
    }

    /// <summary>
    /// Generated code routes non-public methods through <see cref="ParameterMetadataFactory.ForNonPublicMethod"/>,
    /// so this only serves direct callers of the public-method factories. Trimming does not keep non-public
    /// methods for them, so in a trimmed app this finds nothing and ReflectionInfo reports the failed lookup.
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2080",
        Justification = "Best-effort fallback for direct callers; trimmed apps get a descriptive lookup failure instead.")]
    private MethodBase? FindNonPublicMethod()
    {
        return _kind switch
        {
            Kind.InstanceMethod => _declaringType!.GetMethod(_methodName!, BindingFlags.NonPublic | BindingFlags.Instance, null, GetParameterTypes(), null),
            Kind.StaticMethod => _declaringType!.GetMethod(_methodName!, BindingFlags.NonPublic | BindingFlags.Static, null, GetParameterTypes(), null),
            _ => FindBestMatch(_declaringType!.GetMethods(NonPublicMethods)),
        };
    }

    /// <summary>
    /// Prefers a candidate whose static-ness, generic arity and parameter shape match. Otherwise falls back to the
    /// first candidate with the right name and parameter count, which was the only rule before shape matching.
    /// </summary>
    private MethodBase? FindBestMatch(MethodBase[] candidates)
    {
        MethodBase? byCount = null;

        foreach (var candidate in candidates)
        {
            if (_methodName is not null && candidate.Name != _methodName)
            {
                continue;
            }

            var candidateParameters = candidate.GetParameters();
            if (candidateParameters.Length != _parameters.Length)
            {
                continue;
            }

            if (MatchesSignature(candidate, candidateParameters))
            {
                return candidate;
            }

            byCount ??= candidate;
        }

        return byCount;
    }

    private bool MatchesSignature(MethodBase candidate, ParameterInfo[] candidateParameters)
    {
        if (_isStatic is { } isStatic && candidate.IsStatic != isStatic)
        {
            return false;
        }

        if (_genericParameterCount >= 0
            && (candidate.IsGenericMethodDefinition ? candidate.GetGenericArguments().Length : 0) != _genericParameterCount)
        {
            return false;
        }

        for (var i = 0; i < candidateParameters.Length; i++)
        {
            var parameterType = candidateParameters[i].ParameterType;

            // Generated metadata never describes a by-ref parameter, so a ref/in/out overload of a test method is
            // never an exact match (it can still be picked by the parameter-count fallback if nothing else fits).
            if (parameterType.IsByRef)
            {
                return false;
            }

            // Parameters that use type parameters are emitted as object, so only concrete types can be compared.
            if (!parameterType.ContainsGenericParameters && parameterType != _parameters[i].Type)
            {
                return false;
            }
        }

        return true;
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
