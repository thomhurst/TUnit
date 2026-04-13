using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Generators;

internal static class AotConverterHelper
{
    /// <summary>
    /// Returns null if no conversions found; otherwise newline-separated Register calls.
    /// </summary>
    public static string? GenerateRegistrationCode(
        IReadOnlyList<IMethodSymbol> methods,
        INamedTypeSymbol containingType,
        Compilation compilation)
    {
        var typesToScan = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var method in methods)
        {
            foreach (var parameter in method.Parameters)
            {
                typesToScan.Add(parameter.Type);
                ScanAttributesForTypes(parameter.GetAttributes(), typesToScan);
            }

            ScanAttributesForTypes(method.GetAttributes(), typesToScan);
        }

        ScanAttributesForTypes(containingType.GetAttributes(), typesToScan);

        foreach (var constructor in containingType.Constructors)
        {
            if (constructor.IsImplicitlyDeclared)
            {
                continue;
            }

            foreach (var parameter in constructor.Parameters)
            {
                typesToScan.Add(parameter.Type);
                ScanAttributesForTypes(parameter.GetAttributes(), typesToScan);
            }
        }

        var seen = new HashSet<(string, string)>();
        var registrations = new List<string>();

        foreach (var type in typesToScan)
        {
            ScanTypeForConversions(type, seen, registrations, compilation);
        }

        return registrations.Count > 0 ? string.Join("\n", registrations) : null;
    }

    public static string? GenerateRegistrationCode(
        IMethodSymbol method,
        INamedTypeSymbol containingType,
        Compilation compilation)
    {
        return GenerateRegistrationCode([method], containingType, compilation);
    }

    private static void ScanTypeForConversions(
        ITypeSymbol type,
        HashSet<(string, string)> seen,
        List<string> registrations,
        Compilation compilation)
    {
        if (type is not INamedTypeSymbol namedType)
        {
            return;
        }

        if (!IsAccessibleType(namedType, compilation))
        {
            return;
        }

        foreach (var member in namedType.GetMembers())
        {
            if (member is IMethodSymbol { IsStatic: true, Parameters.Length: 1 } method &&
                (method.Name == "op_Implicit" || method.Name == "op_Explicit"))
            {
                var registration = TryGenerateRegistration(method, seen, compilation);
                if (registration != null)
                {
                    registrations.Add(registration);
                }
            }
        }

        if (namedType.IsGenericType)
        {
            foreach (var typeArg in namedType.TypeArguments)
            {
                ScanTypeForConversions(typeArg, seen, registrations, compilation);
            }
        }
    }

    private static string? TryGenerateRegistration(
        IMethodSymbol operatorMethod,
        HashSet<(string, string)> seen,
        Compilation compilation)
    {
        var sourceType = operatorMethod.Parameters[0].Type;
        var targetType = operatorMethod.ReturnType;

        if (sourceType.IsGenericDefinition() || targetType.IsGenericDefinition())
        {
            return null;
        }

        if (TypeContainsGenericTypeParameters(sourceType) || TypeContainsGenericTypeParameters(targetType))
        {
            return null;
        }

        if (sourceType.IsRefLikeType || targetType.IsRefLikeType)
        {
            return null;
        }

        if (sourceType.TypeKind == TypeKind.Pointer || targetType.TypeKind == TypeKind.Pointer ||
            sourceType.SpecialType == SpecialType.System_Void || targetType.SpecialType == SpecialType.System_Void)
        {
            return null;
        }

        var containingType = operatorMethod.ContainingType;
        if (containingType == null || !IsAccessibleType(containingType, compilation))
        {
            return null;
        }

        if (!IsAccessibleType(sourceType, compilation) || !IsAccessibleType(targetType, compilation))
        {
            return null;
        }

        if (SymbolEqualityComparer.Default.Equals(sourceType, targetType))
        {
            return null;
        }

        var sourceGlobal = sourceType.GloballyQualified();
        var targetGlobal = targetType.GloballyQualified();

        if (!seen.Add((sourceGlobal, targetGlobal)))
        {
            return null;
        }

        return $"global::TUnit.Core.Converters.AotConverterRegistry.Register<{sourceGlobal}, {targetGlobal}>(static source => ({targetGlobal})source);";
    }

    private static void ScanAttributesForTypes(ImmutableArray<AttributeData> attributes, HashSet<ITypeSymbol> typesToScan)
    {
        foreach (var attribute in attributes)
        {
            if (attribute.AttributeClass == null)
            {
                continue;
            }

            if (!DataSourceAttributeHelper.IsDataSourceAttribute(attribute.AttributeClass))
            {
                continue;
            }

            if (attribute.AttributeClass.IsGenericType)
            {
                foreach (var typeArg in attribute.AttributeClass.TypeArguments)
                {
                    typesToScan.Add(typeArg);
                }
            }

            foreach (var arg in attribute.ConstructorArguments)
            {
                ScanTypedConstantForTypes(arg, typesToScan);
            }

            foreach (var namedArg in attribute.NamedArguments)
            {
                ScanTypedConstantForTypes(namedArg.Value, typesToScan);
            }
        }
    }

    private static void ScanTypedConstantForTypes(TypedConstant constant, HashSet<ITypeSymbol> typesToScan)
    {
        if (constant.IsNull)
        {
            return;
        }

        if (constant is { Kind: TypedConstantKind.Type, Value: ITypeSymbol typeValue })
        {
            typesToScan.Add(typeValue);
        }
        else if (constant.Kind == TypedConstantKind.Array)
        {
            foreach (var element in constant.Values)
            {
                ScanTypedConstantForTypes(element, typesToScan);
            }
        }
        else if (constant is { Value: not null, Type: not null })
        {
            typesToScan.Add(constant.Type);
        }
    }

    private static bool IsAccessibleType(ITypeSymbol type, Compilation compilation)
    {
        if (type.SpecialType != SpecialType.None)
        {
            return true;
        }

        if (type.TypeKind == TypeKind.TypeParameter)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedType)
        {
            var typeAssembly = namedType.ContainingAssembly;
            var currentAssembly = compilation.Assembly;

            if (currentAssembly != null && SymbolEqualityComparer.Default.Equals(typeAssembly, currentAssembly))
            {
                return true;
            }

            if (namedType.DeclaredAccessibility == Accessibility.Public)
            {
                if (namedType.IsGenericType)
                {
                    foreach (var typeArg in namedType.TypeArguments)
                    {
                        if (!IsAccessibleType(typeArg, compilation))
                        {
                            return false;
                        }
                    }
                }

                if (namedType.ContainingType != null)
                {
                    return IsAccessibleType(namedType.ContainingType, compilation);
                }

                return true;
            }

            if (namedType.DeclaredAccessibility == Accessibility.Internal)
            {
                if (currentAssembly == null)
                {
                    return false;
                }

                if (typeAssembly != null && typeAssembly.GivesAccessTo(currentAssembly))
                {
                    return true;
                }

                return false;
            }

            if (namedType.IsGenericType)
            {
                foreach (var typeArg in namedType.TypeArguments)
                {
                    if (!IsAccessibleType(typeArg, compilation))
                    {
                        return false;
                    }
                }
            }

            if (namedType.ContainingType != null)
            {
                return IsAccessibleType(namedType.ContainingType, compilation);
            }

            return false;
        }

        if (type is IArrayTypeSymbol arrayType)
        {
            return IsAccessibleType(arrayType.ElementType, compilation);
        }

        if (type is IPointerTypeSymbol pointerType)
        {
            return IsAccessibleType(pointerType.PointedAtType, compilation);
        }

        return false;
    }

    private static bool TypeContainsGenericTypeParameters(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.TypeParameter)
        {
            return true;
        }

        if (type is INamedTypeSymbol namedTypeSymbol)
        {
            foreach (var typeArgument in namedTypeSymbol.TypeArguments)
            {
                if (TypeContainsGenericTypeParameters(typeArgument))
                {
                    return true;
                }
            }
        }

        if (type is IArrayTypeSymbol arrayTypeSymbol)
        {
            return TypeContainsGenericTypeParameters(arrayTypeSymbol.ElementType);
        }

        if (type is IPointerTypeSymbol pointerTypeSymbol)
        {
            return TypeContainsGenericTypeParameters(pointerTypeSymbol.PointedAtType);
        }

        return false;
    }
}
