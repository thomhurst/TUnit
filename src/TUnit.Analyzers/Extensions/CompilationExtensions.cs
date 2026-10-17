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
    /// </summary>
    public static bool CanBindToTestParameter(this Compilation compilation, ITypeSymbol? argumentType,
        ITypeSymbol? parameterType)
    {
        if (parameterType is ITypeParameterSymbol typeParameter && argumentType is not null)
        {
            return HasGenerateGenericTestAttribute(typeParameter)
                || compilation.CanSatisfyConstraints(argumentType, typeParameter);
        }

        return compilation.HasImplicitConversionOrGenericParameter(argumentType, parameterType);
    }

    private static bool HasGenerateGenericTestAttribute(ITypeParameterSymbol typeParameter)
    {
        // With [GenerateGenericTest] the type argument is fixed by the attribute and the data converts to it at
        // run time, so the data type does not have to satisfy the constraints itself.
        var declaringSymbol = (ISymbol?) typeParameter.DeclaringMethod ?? typeParameter.DeclaringType;

        return declaringSymbol?.GetAttributes().Any(a =>
            a.AttributeClass is { Name: "GenerateGenericTestAttribute" } attributeClass
            && attributeClass.ContainingNamespace?.ToDisplayString() == "TUnit.Core") == true;
    }

    private static bool CanSatisfyConstraints(this Compilation compilation, ITypeSymbol argumentType,
        ITypeParameterSymbol typeParameter)
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

        foreach (var constraintType in typeParameter.ConstraintTypes)
        {
            // Constraints such as `where T : IComparable<T>` depend on the inferred type arguments; skip them
            // rather than guess.
            if (ContainsTypeParameter(constraintType))
            {
                continue;
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
