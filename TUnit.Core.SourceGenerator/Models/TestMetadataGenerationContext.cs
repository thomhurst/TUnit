using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Models;

/// <summary>
/// Encapsulates all the context needed for test metadata generation, avoiding long parameter lists
/// and making it easier to pass state between components.
/// </summary>
public class TestMetadataGenerationContext
{
    public required TestMethodMetadata TestInfo { get; init; }
    public required string ClassName { get; init; }
    public required string MethodName { get; init; }
    public required List<IPropertySymbol> RequiredProperties { get; init; }
    public required IMethodSymbol? ConstructorWithParameters { get; init; }
    public required bool HasParameterlessConstructor { get; init; }
    public required string SafeClassName { get; init; }
    public required string SafeMethodName { get; init; }
    public required string Guid { get; init; }
    public required bool CanUseStaticDefinition { get; init; }
    
    /// <summary>
    /// Creates a TestMetadataGenerationContext from the test method info
    /// </summary>
    public static TestMetadataGenerationContext Create(TestMethodMetadata testInfo)
    {
        if (testInfo?.TypeSymbol == null || testInfo.MethodSymbol == null)
        {
            throw new ArgumentNullException(nameof(testInfo), "TestInfo or its required properties cannot be null");
        }
        
        var className = GetFullTypeName(testInfo.TypeSymbol);
        var methodName = testInfo.MethodSymbol.Name;
        var safeClassName = SanitizeForFilename(className);
        var safeMethodName = SanitizeForFilename(methodName);
        
        var requiredProperties = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.IsRequired)
            .ToList();
            
        var constructors = testInfo.TypeSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public)
            .OrderBy(c => c.Parameters.Length)
            .ToList();
            
        var hasParameterlessConstructor = constructors.Any(c => c.Parameters.Length == 0);
        var constructorWithParameters = !hasParameterlessConstructor ? constructors.FirstOrDefault() : null;
        
        return new TestMetadataGenerationContext
        {
            TestInfo = testInfo,
            ClassName = className,
            MethodName = methodName,
            RequiredProperties = requiredProperties,
            ConstructorWithParameters = constructorWithParameters,
            HasParameterlessConstructor = hasParameterlessConstructor,
            SafeClassName = safeClassName,
            SafeMethodName = safeMethodName,
            Guid = System.Guid.NewGuid().ToString("N"),
            CanUseStaticDefinition = DetermineIfStaticTestDefinition(testInfo)
        };
    }
    
    private static string GetFullTypeName(ITypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat
            .WithGlobalNamespaceStyle(SymbolDisplayGlobalNamespaceStyle.Omitted));
    }
    
    private static string SanitizeForFilename(string name)
    {
        // Replace all invalid filename characters with underscores
        var invalid = System.IO.Path.GetInvalidFileNameChars()
            .Concat(['<', '>', '(', ')', '[', ']', '{', '}', ',', ' ', '`', '.'])
            .Distinct();

        var sanitized = name;
        foreach (var c in invalid)
        {
            sanitized = sanitized.Replace(c, '_');
        }

        return sanitized;
    }
    
    private static bool DetermineIfStaticTestDefinition(TestMethodMetadata testInfo)
    {
        // Can use static definition if:
        // 1. The type is not generic
        if (testInfo.TypeSymbol.IsGenericType)
            return false;

        // 2. The method is not generic
        if (testInfo.MethodSymbol.IsGenericMethod)
            return false;

        // 3. No data sources that require runtime resolution
        // Check class-level attributes for data sources
        foreach (var attr in testInfo.TypeSymbol.GetAttributes())
        {
            if (IsRuntimeDataSourceAttribute(attr, testInfo.TypeSymbol))
                return false;
        }
        
        // Check method-level attributes for data sources
        foreach (var attr in testInfo.MethodSymbol.GetAttributes())
        {
            if (IsRuntimeDataSourceAttribute(attr, testInfo.TypeSymbol))
                return false;
        }
        
        // Check method parameters for data attributes
        foreach (var param in testInfo.MethodSymbol.Parameters)
        {
            foreach (var attr in param.GetAttributes())
            {
                // Check if it's a data source attribute that requires runtime resolution
                if (IsRuntimeDataSourceAttribute(attr, testInfo.TypeSymbol))
                    return false;
            }
        }

        // Check class constructor parameters for data attributes
        var constructors = testInfo.TypeSymbol.Constructors
            .Where(c => !c.IsStatic && c.DeclaredAccessibility == Accessibility.Public);

        foreach (var constructor in constructors)
        {
            foreach (var param in constructor.Parameters)
            {
                foreach (var attr in param.GetAttributes())
                {
                    if (IsRuntimeDataSourceAttribute(attr, testInfo.TypeSymbol))
                        return false;
                }
            }
        }

        // Check properties for data attributes
        var properties = testInfo.TypeSymbol.GetMembers().OfType<IPropertySymbol>();
        foreach (var prop in properties)
        {
            foreach (var attr in prop.GetAttributes())
            {
                if (IsRuntimeDataSourceAttribute(attr, testInfo.TypeSymbol))
                    return false;
            }
        }

        // All checks passed - can use static definition
        return true;
    }
    
    private static bool IsRuntimeDataSourceAttribute(AttributeData attr, ITypeSymbol containingType)
    {
        var attrName = attr.AttributeClass?.Name;

        // These require runtime resolution:
        // - GeneratedDataAttribute (dynamic generation)
        // - Attributes inheriting from AsyncDataSourceGeneratorAttribute (async generation)
        // - Attributes inheriting from DataSourceGeneratorAttribute (dynamic generation)
        if (attrName is "GeneratedDataAttribute")
            return true;
            
        // Check if it inherits from async/sync data source generator attributes
        var baseType = attr.AttributeClass?.BaseType;
        while (baseType != null)
        {
            var baseName = baseType.Name;
            if (baseName is "AsyncDataSourceGeneratorAttribute" or "DataSourceGeneratorAttribute" 
                or "AsyncNonTypedDataSourceGeneratorAttribute" or "NonTypedDataSourceGeneratorAttribute")
                return true;
            baseType = baseType.BaseType;
        }
        
        // Check if MethodDataSourceAttribute refers to an instance method
        if (attrName == "MethodDataSourceAttribute")
        {
            var args = attr.ConstructorArguments;
            if (args.Length == 1)
            {
                // Single argument - method name on the same class
                var methodName = args[0].Value?.ToString();
                if (methodName != null && containingType != null)
                {
                    // Find the method on the containing type
                    var method = containingType.GetMembers(methodName)
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault();
                    
                    if (method != null && method.IsStatic)
                    {
                        // Static method - can be resolved at compile time
                        return false;
                    }
                }
                // If we can't find it or it's not static, requires runtime
                return true;
            }
            else if (args.Length == 2 && args[0].Value == null)
            {
                // Method on test class - need to check if it's static
                return true; // For now, assume it could be instance method
            }
        }
        
        return false;
    }
}