using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Analyzers.Helpers;
using ArrayBindings = System.Collections.Immutable.ImmutableDictionary<Microsoft.CodeAnalysis.ITypeParameterSymbol, System.Collections.Immutable.ImmutableArray<(Microsoft.CodeAnalysis.ITypeSymbol Type, Microsoft.CodeAnalysis.VarianceKind Variance)>>;

namespace TUnit.Analyzers.Extensions;

public static class CompilationExtensions
{
    private static readonly ArrayBindings EmptyArrayBindings = ImmutableDictionary.Create<ITypeParameterSymbol, ImmutableArray<(ITypeSymbol Type, VarianceKind Variance)>>(SymbolEqualityComparer.Default);

    public static bool HasImplicitConversionOrGenericParameter(this Compilation compilation, ITypeSymbol? argumentType,
        ITypeSymbol? parameterType)
    {
        var matcher = parameterType is not null && ContainsTypeParameter(parameterType) ? new OpenArrayMatcher() : null;
        var matches = HasImplicitConversionOrGenericParameter(compilation, argumentType, parameterType,
            EmptyArrayBindings, _ => true, matcher);
        return matcher?.IsExhausted == true || matches;
    }

    private static bool HasImplicitConversionOrGenericParameter(Compilation compilation, ITypeSymbol? argumentType,
        ITypeSymbol? parameterType, ArrayBindings bindings, Func<ArrayBindings, bool> next, OpenArrayMatcher? matcher)
    {
        if (parameterType == null && argumentType != null)
        {
            return false;
        }
        if (argumentType != null && parameterType != null && 
            argumentType.ToDisplayString() == parameterType.ToDisplayString())
        {
            return next(bindings);
        }

        // Preserve conversions Roslyn can already prove, including open array covariance.
        if (argumentType is not null && parameterType is not null
            && compilation.HasImplicitConversion(argumentType, parameterType))
        {
            return next(bindings);
        }

        // Open arrays have no Roslyn conversion until their element type arguments are inferred.
        // Recurse through tuple elements so a single tuple parameter keeps its row container.
        if (parameterType is IArrayTypeSymbol parameterArray && ContainsTypeParameter(parameterArray)
            && argumentType is IArrayTypeSymbol argumentArray)
        {
            return parameterArray.Rank == argumentArray.Rank
                && parameterArray.IsSZArray == argumentArray.IsSZArray
                && (matcher ??= new OpenArrayMatcher()).MatchesArrayElement(compilation, argumentArray.ElementType,
                    parameterArray.ElementType, bindings, next,
                    allowReferenceConversions: argumentArray.ElementType.IsReferenceType);
        }

        // Tuples already recognized as generic definitions retain the existing conversion path below.
        // This branch only fills the gap where type parameters are hidden inside arrays.
        if (parameterType is INamedTypeSymbol { IsTupleType: true } parameterTuple
            && ContainsTypeParameter(parameterTuple)
            && !parameterTuple.IsGenericDefinition()
            && argumentType is INamedTypeSymbol { IsTupleType: true } argumentTuple)
        {
            if (parameterTuple.TupleElements.Length != argumentTuple.TupleElements.Length)
            {
                return false;
            }

            bool MatchElements(int index, ArrayBindings candidateBindings)
            {
                if (index == parameterTuple.TupleElements.Length)
                {
                    return next(candidateBindings);
                }

                // Bare type parameters retain the existing generic path behavior; this shape check ignores constraints.
                return parameterTuple.TupleElements[index].Type is ITypeParameterSymbol
                    ? MatchElements(index + 1, candidateBindings)
                    : HasImplicitConversionOrGenericParameter(compilation,
                        argumentTuple.TupleElements[index].Type, parameterTuple.TupleElements[index].Type,
                        candidateBindings, candidate => MatchElements(index + 1, candidate), matcher);
            }

            return MatchElements(0, bindings);
        }

        if (parameterType?.IsGenericDefinition() == false)
        {
            if (argumentType is null)
            {
                return false;
            }

            var conversion = compilation.ClassifyConversion(argumentType, parameterType);
            return (conversion.IsImplicit || conversion.IsNumeric) && next(bindings);
        }

        if (parameterType is INamedTypeSymbol { IsGenericType: true, TypeArguments: [{ TypeKind: TypeKind.TypeParameter }] } namedType)
        {
            // `IEnumerable<>`
            if (argumentType is IArrayTypeSymbol { ElementType: { } elementType })
            {
                var specializedSuper = namedType.OriginalDefinition.Construct(elementType);
                return compilation.HasImplicitConversion(argumentType, specializedSuper) && next(bindings);
            }

            if (argumentType is INamedTypeSymbol { IsGenericType: true, TypeArguments: [{ } genericArgument] })
            {
                var specializedSuper = namedType.OriginalDefinition.Construct(genericArgument);
                return compilation.HasImplicitConversion(argumentType, specializedSuper) && next(bindings);
            }
        }

        return compilation.HasImplicitConversion(argumentType?.OriginalDefinition, parameterType?.OriginalDefinition) && next(bindings);
    }

    /// <summary>
    /// Whether a data source value of <paramref name="argumentType"/> can bind to a test parameter of
    /// <paramref name="parameterType"/>. A bare type-parameter target accepts any argument type, because the
    /// type argument is inferred from the data (or supplied by <c>[GenerateGenericTest]</c>) and the values are
    /// converted to it at run time. The only exception is a type argument that is certainly inferred from the data
    /// and can never satisfy the type parameter's constraints, which is still a mismatch.
    /// </summary>
    /// <param name="inferredTypeArguments">
    /// The type arguments inferred from the data row (see <see cref="InferTypeArguments"/>). A type parameter
    /// missing from it has no certain inference, so its constraints are not checked. When <see langword="null"/>,
    /// <paramref name="parameterType"/> is the only test parameter, so <paramref name="argumentType"/> is the
    /// inferred type argument.
    /// </param>
    public static bool CanBindToTestParameter(this Compilation compilation, ITypeSymbol? argumentType,
        ITypeSymbol? parameterType,
        IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol>? inferredTypeArguments = null)
    {
        if (parameterType is ITypeParameterSymbol typeParameter && argumentType is not null)
        {
            // With [GenerateGenericTest] the attribute supplies the type argument and the generated call converts
            // the data to it through CastHelper (run-time types, IConvertible, string parsing, conversion operators,
            // AotConverterRegistry, ...). None of that is visible here, so the data is never rejected.
            if (HasGenerateGenericTestAttribute(typeParameter))
            {
                return true;
            }

            // A class type parameter is resolved from the class data and constructor arguments, not from the
            // method's data row, so the row says nothing about which type argument it gets.
            if (typeParameter.DeclaringMethod is null)
            {
                return true;
            }

            if (inferredTypeArguments is not null && !inferredTypeArguments.ContainsKey(typeParameter))
            {
                return true;
            }

            return compilation.CanSatisfyConstraints(argumentType, typeParameter, inferredTypeArguments);
        }

        return compilation.HasImplicitConversionOrGenericParameter(argumentType, parameterType);
    }

    /// <summary>
    /// Maps each type parameter to the data type it is inferred as, when that is certain: every test parameter
    /// that mentions the type parameter is the bare type parameter, and all of them receive the same data type.
    /// Otherwise the inference depends on which occurrence wins (the generator takes the last one and converts the
    /// other values to it), so the type parameter is left out.
    /// </summary>
    public static Dictionary<ITypeParameterSymbol, ITypeSymbol> InferTypeArguments(
        IReadOnlyList<ITypeSymbol?> argumentTypes, IReadOnlyList<ITypeSymbol?> parameterTypes)
    {
        var inferred = new Dictionary<ITypeParameterSymbol, ITypeSymbol>(SymbolEqualityComparer.Default);
        var uncertain = new HashSet<ITypeParameterSymbol>(SymbolEqualityComparer.Default);

        for (var i = 0; i < parameterTypes.Count; i++)
        {
            var parameterType = parameterTypes[i];
            if (parameterType is null)
            {
                continue;
            }

            if (parameterType is not ITypeParameterSymbol typeParameter)
            {
                // A type parameter nested in a constructed parameter type can also drive the inference.
                CollectTypeParameters(parameterType, uncertain);
                continue;
            }

            // Class type parameters are not inferred from the method's data row.
            if (typeParameter.DeclaringMethod is null)
            {
                uncertain.Add(typeParameter);
                continue;
            }

            var argumentType = i < argumentTypes.Count ? argumentTypes[i] : null;
            if (argumentType is null
                || (inferred.TryGetValue(typeParameter, out var existing)
                    && !SymbolEqualityComparer.Default.Equals(existing, argumentType)))
            {
                uncertain.Add(typeParameter);
                continue;
            }

            inferred[typeParameter] = argumentType;
        }

        foreach (var typeParameter in uncertain)
        {
            inferred.Remove(typeParameter);
        }

        return inferred;
    }

    private static void CollectTypeParameters(ITypeSymbol type, HashSet<ITypeParameterSymbol> result)
    {
        switch (type)
        {
            case ITypeParameterSymbol typeParameter:
                result.Add(typeParameter);
                break;
            case IArrayTypeSymbol arrayType:
                CollectTypeParameters(arrayType.ElementType, result);
                break;
            case IPointerTypeSymbol pointerType:
                CollectTypeParameters(pointerType.PointedAtType, result);
                break;
            case INamedTypeSymbol namedType:
                foreach (var typeArgument in namedType.TypeArguments)
                {
                    CollectTypeParameters(typeArgument, result);
                }

                if (namedType.ContainingType is { } containingType)
                {
                    CollectTypeParameters(containingType, result);
                }

                break;
        }
    }

    /// <summary>
    /// Whether <paramref name="argumentType"/> is a data source's declared type for a method type parameter that a
    /// value of another run-time type could have, such as <c>object</c>, an interface, a non-sealed class or a
    /// nullable value type. Source-generated mode infers the type argument from the declared type itself (#7116).
    /// </summary>
    public static bool IsBroadDeclaredTypeArgument(ITypeSymbol? argumentType, ITypeSymbol? parameterType)
    {
        return parameterType is ITypeParameterSymbol { DeclaringMethod: not null }
            && argumentType is not null and not ITypeParameterSymbol and not IErrorTypeSymbol
            && argumentType.TypeKind != TypeKind.Dynamic
            && !ExactTypeExtensions.IsExactType(argumentType);
    }

    /// <summary>
    /// Whether the method or type that declares <paramref name="typeParameter"/> has <c>[GenerateGenericTest]</c>.
    /// </summary>
    private static bool HasGenerateGenericTestAttribute(ITypeParameterSymbol typeParameter)
    {
        var declaringSymbol = (ISymbol?) typeParameter.DeclaringMethod ?? typeParameter.DeclaringType;

        return declaringSymbol is not null && declaringSymbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.IsGloballyQualifiedNonGeneric(
                WellKnown.AttributeFullyQualifiedClasses.GenerateGenericTestAttribute.WithGlobalPrefix) == true);
    }

    // Keep in step with TestMetadataGenerator.ValidateTypeParameterConstraints in TUnit.Core.SourceGenerator,
    // which validates the same type arguments at generation time. This check fails open: any shape it cannot
    // decide for certain is accepted, so a gap here is a missed diagnostic rather than a false TUnit0001.
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
            // rest of the data row when those are certain and exact. Constraints still mentioning a type parameter
            // depend on type arguments that are not known here, so they are skipped rather than guessed.
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

                // A peer is only substituted when its data type is exact, so the run-time type argument cannot
                // differ from it.
                return otherReplacements is not null
                    && otherReplacements.TryGetValue(otherTypeParameter, out var otherReplacement)
                    && ExactTypeExtensions.IsExactType(otherReplacement)
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

    internal static bool ContainsTypeParameter(ITypeSymbol type)
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
