using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Analyzers.Extensions;

public static class CompilationExtensions
{
    public static bool HasImplicitConversionOrGenericParameter(this Compilation compilation, ITypeSymbol? argumentType,
        ITypeSymbol? parameterType)
    {
        if (parameterType == null && argumentType != null)
        {
            return false;
        }
        if (argumentType != null && parameterType != null && 
            argumentType.ToDisplayString() == parameterType.ToDisplayString())
        {
            return true;
        }

        if (parameterType?.IsGenericDefinition() == false)
        {
            if (argumentType is null)
            {
                return false;
            }

            var conversion = compilation.ClassifyConversion(argumentType, parameterType);
            return conversion.IsImplicit || conversion.IsNumeric;
        }

        if (parameterType is INamedTypeSymbol { IsGenericType: true, TypeArguments: [{ TypeKind: TypeKind.TypeParameter }] } namedType)
        {
            // `IEnumerable<>`
            if (argumentType is IArrayTypeSymbol { ElementType: { } elementType })
            {
                var specializedSuper = namedType.OriginalDefinition.Construct(elementType);
                return compilation.HasImplicitConversion(argumentType, specializedSuper);
            }

            if (argumentType is INamedTypeSymbol { IsGenericType: true, TypeArguments: [{ } genericArgument] })
            {
                var specializedSuper = namedType.OriginalDefinition.Construct(genericArgument);
                return compilation.HasImplicitConversion(argumentType, specializedSuper);
            }
        }

        return compilation.HasImplicitConversion(argumentType?.OriginalDefinition, parameterType?.OriginalDefinition);
    }
    /// <summary>
    /// Whether a data source value of <paramref name="argumentType"/> can bind to a test parameter of
    /// <paramref name="parameterType"/>. A bare type-parameter target accepts any argument type, because the
    /// type argument is inferred from the data (or supplied by <c>[GenerateGenericTest]</c>). The only exception
    /// is an argument that can never satisfy the type parameter's constraints, which is still a mismatch.
    /// With <c>[GenerateGenericTest]</c>, the data is instead only rejected when it can never convert to a type
    /// argument the attribute supplies.
    /// </summary>
    /// <param name="inferredTypeArguments">
    /// Type arguments inferred from the other parameters of the same data row (see
    /// <see cref="InferExactTypeArguments"/>), used to check constraints that mention another type parameter.
    /// </param>
    public static bool CanBindToTestParameter(this Compilation compilation, ITypeSymbol? argumentType,
        ITypeSymbol? parameterType,
        IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol>? inferredTypeArguments = null)
    {
        if (parameterType is ITypeParameterSymbol typeParameter && argumentType is not null)
        {
            var generatedTypeArguments = GetGenerateGenericTestTypeArguments(typeParameter);
            if (generatedTypeArguments is not null)
            {
                return CanConvertToGeneratedTypeArguments(compilation, argumentType, generatedTypeArguments);
            }

            return compilation.CanSatisfyConstraints(argumentType, typeParameter, inferredTypeArguments);
        }

        return compilation.HasImplicitConversionOrGenericParameter(argumentType, parameterType);
    }

    /// <summary>
    /// Maps each type parameter that is a bare test parameter type to the data type bound to it, when that mapping
    /// is certain: every bare occurrence of the type parameter receives the same data type, and that type is
    /// exact (a non-nullable value type or a sealed class), so the run-time type argument cannot differ from it.
    /// Uncertain type parameters are left out, and constraints that mention them are skipped.
    /// </summary>
    public static Dictionary<ITypeParameterSymbol, ITypeSymbol> InferExactTypeArguments(
        IReadOnlyList<ITypeSymbol?> argumentTypes, IReadOnlyList<ITypeSymbol?> parameterTypes)
    {
        var inferred = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        var conflicting = new HashSet<ITypeParameterSymbol>(SymbolEqualityComparer.Default);

        for (var i = 0; i < argumentTypes.Count && i < parameterTypes.Count; i++)
        {
            if (parameterTypes[i] is not ITypeParameterSymbol typeParameter || conflicting.Contains(typeParameter))
            {
                continue;
            }

            var argumentType = argumentTypes[i];
            if (argumentType is null || !IsExactType(argumentType)
                || (inferred.TryGetValue(typeParameter, out var existing)
                    && !SymbolEqualityComparer.Default.Equals(existing, argumentType)))
            {
                conflicting.Add(typeParameter);
                inferred.Remove(typeParameter);
                continue;
            }

            inferred[typeParameter] = argumentType;
        }

        return inferred;
    }

    private static bool IsExactType(ITypeSymbol type)
    {
        if (type is ITypeParameterSymbol or IErrorTypeSymbol || type.TypeKind == TypeKind.Dynamic
            || ContainsTypeParameter(type))
        {
            return false;
        }

        if (type.IsValueType)
        {
            return type.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T;
        }

        return type is INamedTypeSymbol { TypeKind: TypeKind.Class or TypeKind.Delegate, IsSealed: true };
    }

    /// <summary>
    /// The type argument each <c>[GenerateGenericTest]</c> on the declaring member supplies for
    /// <paramref name="typeParameter"/>, or <see langword="null"/> when there is no such attribute. An entry is
    /// <see langword="null"/> when that attribute's type argument cannot be resolved to a closed type.
    /// </summary>
    private static List<ITypeSymbol?>? GetGenerateGenericTestTypeArguments(ITypeParameterSymbol typeParameter)
    {
        var declaringSymbol = (ISymbol?) typeParameter.DeclaringMethod ?? typeParameter.DeclaringType;
        if (declaringSymbol is null)
        {
            return null;
        }

        var typeParameterCount = declaringSymbol is IMethodSymbol method
            ? method.TypeParameters.Length
            : ((INamedTypeSymbol) declaringSymbol).TypeParameters.Length;

        List<ITypeSymbol?>? result = null;

        foreach (var attribute in declaringSymbol.GetAttributes())
        {
            if (attribute.AttributeClass is not { Name: "GenerateGenericTestAttribute" } attributeClass
                || attributeClass.ContainingNamespace?.ToDisplayString() != "TUnit.Core")
            {
                continue;
            }

            result ??= [];

            ITypeSymbol? typeArgument = null;
            if (attribute.ConstructorArguments is [{ Kind: TypedConstantKind.Array } typeArguments]
                && typeArguments.Values.Length == typeParameterCount
                && typeArguments.Values[typeParameter.Ordinal].Value is ITypeSymbol supplied
                && supplied is not IErrorTypeSymbol
                && supplied is not INamedTypeSymbol { IsUnboundGenericType: true }
                && !ContainsTypeParameter(supplied))
            {
                typeArgument = supplied;
            }

            result.Add(typeArgument);
        }

        return result;
    }

    private static bool CanConvertToGeneratedTypeArguments(Compilation compilation, ITypeSymbol argumentType,
        List<ITypeSymbol?> generatedTypeArguments)
    {
        // Each attribute closes the test over its type argument, and the generated call converts every data value
        // to it with CastHelper. That conversion goes well beyond C# implicit conversions: it uses the value's
        // run-time type (which can be narrower than the declared one), IConvertible, string parsing, single-element
        // enumerable unwrapping and user-defined explicit operators. Only report data that can never convert:
        // an exact type (its run-time type cannot be narrower) that takes none of those paths and has no
        // conversion of any kind to the type argument. The attribute's own type arguments are what must satisfy
        // the constraints, not the data type.
        if (!IsExactType(argumentType) || IsRuntimeConvertible(compilation, argumentType))
        {
            return true;
        }

        foreach (var generatedTypeArgument in generatedTypeArguments)
        {
            if (generatedTypeArgument is null or IArrayTypeSymbol || generatedTypeArgument.TypeKind == TypeKind.Dynamic)
            {
                continue;
            }

            if (!compilation.ClassifyConversion(argumentType, generatedTypeArgument).Exists)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Whether CastHelper may convert a value of <paramref name="type"/> through a path that compile-time
    /// conversions do not model: IConvertible, string parsing or single-element enumerable unwrapping.
    /// </summary>
    private static bool IsRuntimeConvertible(Compilation compilation, ITypeSymbol type)
    {
        if (type.SpecialType == SpecialType.System_String)
        {
            return true;
        }

        var convertible = compilation.GetTypeByMetadataName("System.IConvertible");
        var enumerable = compilation.GetSpecialType(SpecialType.System_Collections_IEnumerable);

        return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, convertible)
            || SymbolEqualityComparer.Default.Equals(i, enumerable));
    }

    private static bool CanSatisfyConstraints(this Compilation compilation, ITypeSymbol argumentType,
        ITypeParameterSymbol typeParameter, IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol>? inferredTypeArguments)
    {
        // Only report what is certain. Open or unresolved argument types are accepted.
        if (argumentType is ITypeParameterSymbol or IErrorTypeSymbol || argumentType.TypeKind == TypeKind.Dynamic)
        {
            return true;
        }

        var isNullableValueType = argumentType.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;

        if (typeParameter.HasReferenceTypeConstraint && !argumentType.IsReferenceType)
        {
            return false;
        }

        if (typeParameter.HasValueTypeConstraint && (!argumentType.IsValueType || isNullableValueType))
        {
            return false;
        }

        if (typeParameter.HasUnmanagedTypeConstraint && !argumentType.IsUnmanagedType)
        {
            return false;
        }

        if (typeParameter.HasConstructorConstraint && !HasPublicParameterlessConstructor(argumentType))
        {
            return false;
        }

        foreach (var declaredConstraintType in typeParameter.ConstraintTypes)
        {
            // Self-referencing constraints such as `where T : IComparable<T>` are checked with the argument type
            // substituted for T. Other type parameters are substituted with the type arguments inferred from the
            // rest of the data row when those are certain. Constraints still mentioning a type parameter depend on
            // type arguments that are not known here, so they are skipped rather than guessed.
            var constraintType = SubstituteTypeParameters(compilation, declaredConstraintType, typeParameter,
                argumentType, inferredTypeArguments);
            if (constraintType is null || ContainsTypeParameter(constraintType))
            {
                continue;
            }

            // A nullable value type never satisfies a constraint type (C# spec: interface and base class
            // constraints both exclude Nullable<T>), even though its boxing conversion is implicit.
            if (isNullableValueType)
            {
                return false;
            }

            // Constraints are satisfied by identity, implicit reference or boxing conversions only.
            // User-defined conversions do not count.
            var conversion = compilation.ClassifyConversion(argumentType, constraintType);
            if (!conversion.IsIdentity && !(conversion.IsImplicit && (conversion.IsReference || conversion.IsBoxing)))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasPublicParameterlessConstructor(ITypeSymbol type)
    {
        if (type.IsValueType)
        {
            return true;
        }

        return type is INamedTypeSymbol { TypeKind: TypeKind.Class, IsAbstract: false } namedType
            && namedType.InstanceConstructors.Any(c =>
                c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
    }

    /// <summary>
    /// Replaces <paramref name="typeParameter"/> with <paramref name="replacement"/>, and any type parameter in
    /// <paramref name="otherReplacements"/> with its mapped type, inside <paramref name="type"/>.
    /// Returns <see langword="null"/> when the type has a shape the substitution does not handle.
    /// </summary>
    private static ITypeSymbol? SubstituteTypeParameters(Compilation compilation, ITypeSymbol type,
        ITypeParameterSymbol typeParameter, ITypeSymbol replacement,
        IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol>? otherReplacements)
    {
        switch (type)
        {
            case ITypeParameterSymbol otherTypeParameter:
                if (SymbolEqualityComparer.Default.Equals(type, typeParameter))
                {
                    return replacement;
                }

                return otherReplacements is not null
                    && otherReplacements.TryGetValue(otherTypeParameter, out var otherReplacement)
                        ? otherReplacement
                        : type;
            case IArrayTypeSymbol arrayType:
                var elementType = SubstituteTypeParameters(compilation, arrayType.ElementType, typeParameter,
                    replacement, otherReplacements);
                return elementType is null ? null : compilation.CreateArrayTypeSymbol(elementType, arrayType.Rank);
            case INamedTypeSymbol namedType when !ContainsTypeParameter(namedType):
                return namedType;
            case INamedTypeSymbol { IsGenericType: true } namedType
                when namedType.ContainingType is null || !ContainsTypeParameter(namedType.ContainingType):
                var typeArguments = new ITypeSymbol[namedType.TypeArguments.Length];
                for (var i = 0; i < typeArguments.Length; i++)
                {
                    var typeArgument = SubstituteTypeParameters(compilation, namedType.TypeArguments[i], typeParameter,
                        replacement, otherReplacements);
                    if (typeArgument is null)
                    {
                        return null;
                    }

                    typeArguments[i] = typeArgument;
                }

                return namedType.OriginalDefinition.Construct(typeArguments);
            default:
                return null;
        }
    }

    private static bool ContainsTypeParameter(ITypeSymbol type)
    {
        return type switch
        {
            ITypeParameterSymbol => true,
            IArrayTypeSymbol arrayType => ContainsTypeParameter(arrayType.ElementType),
            IPointerTypeSymbol pointerType => ContainsTypeParameter(pointerType.PointedAtType),
            INamedTypeSymbol namedType => namedType.TypeArguments.Any(ContainsTypeParameter)
                || (namedType.ContainingType is { } containingType && ContainsTypeParameter(containingType)),
            _ => false,
        };
    }
}
