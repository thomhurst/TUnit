using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Resolves generic types at compile time for AOT compatibility
/// </summary>
public class GenericTypeResolver
{
    private readonly Dictionary<string, List<GenericInstantiation>> _genericInstantiations = new();
    private readonly int _maxDepth;

    public GenericTypeResolver(int maxDepth = 5)
    {
        _maxDepth = maxDepth;
    }

    public void ResolveGenericTypes(INamedTypeSymbol typeSymbol, List<TestMethodMetadata> testMethods)
    {
        // Check for explicit [GenerateGenericTest] attributes
        var explicitInstantiations = GetExplicitInstantiations(typeSymbol);
        if (explicitInstantiations.Any())
        {
            foreach (var instantiation in explicitInstantiations)
            {
                AddInstantiation(typeSymbol, instantiation);
            }
            return;
        }

        // Analyze actual usage in the codebase
        var usedInstantiations = AnalyzeUsedInstantiations(typeSymbol, testMethods);
        foreach (var instantiation in usedInstantiations)
        {
            AddInstantiation(typeSymbol, instantiation);
        }
    }

    private List<GenericInstantiation> GetExplicitInstantiations(INamedTypeSymbol typeSymbol)
    {
        var instantiations = new List<GenericInstantiation>();

        // Check class-level attributes
        var classAttributes = typeSymbol.GetAttributes()
            .Where(attr => attr.AttributeClass?.Name == "GenerateGenericTestAttribute");

        foreach (var attr in classAttributes)
        {
            var typeArgs = ExtractTypeArguments(attr);
            if (typeArgs != null && ValidateTypeArguments(typeSymbol, typeArgs))
            {
                instantiations.Add(new GenericInstantiation
                {
                    TypeSymbol = typeSymbol,
                    TypeArguments = typeArgs,
                    Source = InstantiationSource.ExplicitAttribute
                });
            }
        }

        return instantiations;
    }

    private ITypeSymbol[]? ExtractTypeArguments(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length == 0)
        {
            return null;
        }

        // For params array attributes, the types are passed as individual arguments
        var types = new List<ITypeSymbol>();

        foreach (var arg in attribute.ConstructorArguments)
        {
            if (arg.Kind == TypedConstantKind.Type && arg.Value is ITypeSymbol typeSymbol)
            {
                types.Add(typeSymbol);
            }
            else if (arg.Kind == TypedConstantKind.Array)
            {
                // Handle array of types
                foreach (var typeConstant in arg.Values)
                {
                    if (typeConstant.Value is ITypeSymbol arrayTypeSymbol)
                    {
                        types.Add(arrayTypeSymbol);
                    }
                }
            }
        }

        return types.Count > 0 ? types.ToArray() : null;
    }

    private bool ValidateTypeArguments(INamedTypeSymbol genericType, ITypeSymbol[] typeArguments)
    {
        if (genericType.TypeParameters.Length != typeArguments.Length)
        {
            return false;
        }

        // Validate constraints
        for (var i = 0; i < genericType.TypeParameters.Length; i++)
        {
            var param = genericType.TypeParameters[i];
            var arg = typeArguments[i];

            if (!ValidateConstraints(param, arg))
            {
                return false;
            }
        }

        return true;
    }

    private bool ValidateConstraints(ITypeParameterSymbol parameter, ITypeSymbol argumentType)
    {
        // Check new() constraint
        if (parameter.HasConstructorConstraint)
        {
            // Check if type has accessible parameterless constructor
            if (argumentType is INamedTypeSymbol namedType)
            {
                var hasDefaultCtor = namedType.Constructors.Any(c =>
                    c.Parameters.Length == 0 &&
                    c.DeclaredAccessibility == Accessibility.Public);
                if (!hasDefaultCtor)
                {
                    return false;
                }
            }
        }

        // Check class/struct constraints
        if (parameter.HasReferenceTypeConstraint && argumentType.IsValueType)
        {
            return false;
        }

        if (parameter.HasValueTypeConstraint && !argumentType.IsValueType)
        {
            return false;
        }

        // Check base type constraints
        foreach (var constraintType in parameter.ConstraintTypes)
        {
            // Check if argumentType satisfies the constraint
            if (!SatisfiesConstraint(argumentType, constraintType))
            {
                return false;
            }
        }

        return true;
    }

    private bool SatisfiesConstraint(ITypeSymbol argumentType, ITypeSymbol constraintType)
    {
        // Check direct match
        if (SymbolEqualityComparer.Default.Equals(argumentType, constraintType))
        {
            return true;
        }

        // Check if argumentType inherits from or implements constraintType
        if (constraintType.TypeKind == TypeKind.Interface)
        {
            return argumentType.AllInterfaces.Any(i =>
                SymbolEqualityComparer.Default.Equals(i, constraintType));
        }
        else if (constraintType.TypeKind == TypeKind.Class)
        {
            var baseType = argumentType.BaseType;
            while (baseType != null)
            {
                if (SymbolEqualityComparer.Default.Equals(baseType, constraintType))
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
        }

        return false;
    }

    private List<GenericInstantiation> AnalyzeUsedInstantiations(
        INamedTypeSymbol typeSymbol,
        List<TestMethodMetadata> testMethods)
    {
        var instantiations = new List<GenericInstantiation>();

        // Analyze test methods for actual usage patterns
        foreach (var testMethod in testMethods)
        {
            if (testMethod.MethodSymbol.IsGenericMethod)
            {
                // Look for concrete calls to this generic method
                var usages = FindMethodUsages(testMethod.MethodSymbol);
                foreach (var usage in usages)
                {
                    instantiations.Add(usage);
                }
            }
        }

        return instantiations.Take(_maxDepth).ToList();
    }

    private List<GenericInstantiation> FindMethodUsages(IMethodSymbol method)
    {
        // This would require semantic analysis of the compilation
        // For now, return empty list
        return new List<GenericInstantiation>();
    }

    private void AddInstantiation(INamedTypeSymbol typeSymbol, GenericInstantiation instantiation)
    {
        var key = typeSymbol.ToDisplayString();
        if (!_genericInstantiations.ContainsKey(key))
        {
            _genericInstantiations[key] = new List<GenericInstantiation>();
        }

        _genericInstantiations[key].Add(instantiation);
    }

    public string GenerateGenericInstantiations()
    {
        var sb = new StringBuilder();

        foreach (var kvp in _genericInstantiations)
        {
            var typeName = kvp.Key;
            var instantiations = kvp.Value;

            sb.AppendLine($"    // Generic instantiations for {typeName}");

            foreach (var instantiation in instantiations)
            {
                GenerateInstantiation(sb, instantiation);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private void GenerateInstantiation(StringBuilder sb, GenericInstantiation instantiation)
    {
        // Generate type map entry
        var typeArgs = string.Join(", ", instantiation.TypeArguments.Select(t =>
            $"typeof({t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})"));
        var key = $"new Type[] {{ {typeArgs} }}";

        var genericArgs = string.Join(", ", instantiation.TypeArguments.Select(t =>
            t.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

        sb.AppendLine($@"    GenericTypeMap.Add({key}, () => new {instantiation.TypeSymbol.ToDisplayString()}<{genericArgs}>());");
    }
}

/// <summary>
/// Represents a generic type instantiation
/// </summary>
public class GenericInstantiation
{
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required ITypeSymbol[] TypeArguments { get; init; }
    public required InstantiationSource Source { get; init; }
}

/// <summary>
/// Source of generic instantiation
/// </summary>
public enum InstantiationSource
{
    ExplicitAttribute,
    UsageAnalysis,
    Configuration
}
