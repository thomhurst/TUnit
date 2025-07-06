using Microsoft.CodeAnalysis;

namespace TUnit.Core.SourceGenerator.Generators.DataSources;

/// <summary>
/// Unified implementation of data source extraction that handles all attribute types
/// </summary>
public sealed class UnifiedDataSourceExtractor : IDataSourceExtractor
{
    /// <summary>
    /// Extracts data sources from a symbol at a specific level
    /// </summary>
    public IEnumerable<ExtractedDataSource> ExtractDataSources(ISymbol symbol, DataSourceLevel level, TestMethodMetadata testContext)
    {
        var attributes = GetAttributesFromSymbol(symbol, level);

        foreach (var attribute in attributes)
        {
            var dataSource = ExtractFromAttribute(attribute, symbol, level, testContext);
            if (dataSource != null)
            {
                yield return dataSource;
            }
        }
    }

    private IEnumerable<AttributeData> GetAttributesFromSymbol(ISymbol symbol, DataSourceLevel level)
    {
        return level switch
        {
            DataSourceLevel.Class => symbol.GetAttributes()
                .Where(a => IsDataSourceAttribute(a)),

            DataSourceLevel.Method => symbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "ArgumentsAttribute" ||
                           a.AttributeClass?.Name == "MethodDataSourceAttribute"),

            DataSourceLevel.Property => symbol.GetAttributes()
                .Where(a => a.AttributeClass?.Name == "DataSourceForAttribute"),

            _ => Enumerable.Empty<AttributeData>()
        };
    }

    private bool IsDataSourceAttribute(AttributeData attribute)
    {
        if (attribute.AttributeClass == null)
        {
            return false;
        }

        var className = attribute.AttributeClass.Name;

        // Direct attribute types
        if (className == "ArgumentsAttribute" ||
            className == "ClassDataSourceAttribute" ||
            className == "MethodDataSourceAttribute")
        {
            return true;
        }

        // Check if it inherits from AsyncDataSourceGeneratorAttribute
        return InheritsFromAsyncDataSourceGenerator(attribute.AttributeClass);
    }

    private bool InheritsFromAsyncDataSourceGenerator(INamedTypeSymbol type)
    {
        // Check interfaces
        if (type.AllInterfaces.Any(i => i.ToDisplayString() == "TUnit.Core.IAsyncDataSourceGeneratorAttribute"))
        {
            return true;
        }

        // Check base types
        var baseType = type.BaseType;
        while (baseType != null)
        {
            var baseTypeName = baseType.ToDisplayString();
            if (baseTypeName.StartsWith("TUnit.Core.AsyncDataSourceGeneratorAttribute") ||
                baseTypeName.StartsWith("TUnit.Core.DataSourceGeneratorAttribute") ||
                baseTypeName.StartsWith("TUnit.Core.UntypedDataSourceGeneratorAttribute") ||
                baseTypeName.StartsWith("TUnit.Core.AsyncUntypedDataSourceGeneratorAttribute"))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    private ExtractedDataSource? ExtractFromAttribute(AttributeData attribute, ISymbol symbol,
        DataSourceLevel level, TestMethodMetadata testContext)
    {
        if (attribute.AttributeClass == null)
        {
            return null;
        }

        var className = attribute.AttributeClass.Name;

        return className switch
        {
            "ArgumentsAttribute" => ExtractArgumentsAttribute(attribute, symbol, level, testContext),
            "MethodDataSourceAttribute" => ExtractMethodDataSourceAttribute(attribute, symbol, level, testContext),
            "DataSourceForAttribute" when symbol is IPropertySymbol property =>
                ExtractPropertyDataSource(property, level, testContext),
            _ when InheritsFromAsyncDataSourceGenerator(attribute.AttributeClass) =>
                ExtractAsyncDataSourceGeneratorAttribute(attribute, symbol, level, testContext),
            _ => null
        };
    }

    private ExtractedDataSource ExtractArgumentsAttribute(AttributeData attribute, ISymbol symbol,
        DataSourceLevel level, TestMethodMetadata testContext)
    {
        var key = GenerateKey(symbol, level, "Arguments", attribute.GetHashCode());

        return new ExtractedDataSource
        {
            Attribute = attribute,
            Type = DataSourceType.Arguments,
            IsAsync = false,
            Key = key,
            SourceType = GetSourceType(symbol, testContext),
            Level = level
        };
    }

    private ExtractedDataSource? ExtractMethodDataSourceAttribute(AttributeData attribute, ISymbol symbol,
        DataSourceLevel level, TestMethodMetadata testContext)
    {
        var methodName = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
        if (string.IsNullOrEmpty(methodName))
        {
            return null;
        }

        var sourceType = GetSourceType(symbol, testContext);
        var methodSymbol = sourceType.GetMembers(methodName!)
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (methodSymbol == null)
        {
            return null;
        }

        var key = GenerateKey(symbol, level, $"Method_{methodName}", 0);

        return new ExtractedDataSource
        {
            Attribute = attribute,
            Type = DataSourceType.MethodDataSource,
            IsAsync = IsAsyncMethod(methodSymbol),
            Key = key,
            MethodSymbol = methodSymbol,
            SourceType = sourceType,
            Level = level
        };
    }

    private ExtractedDataSource? ExtractPropertyDataSource(IPropertySymbol? property,
        DataSourceLevel level, TestMethodMetadata testContext)
    {
        if (property == null)
        {
            return null;
        }

        var dataSourceAttribute = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "DataSourceForAttribute");

        if (dataSourceAttribute == null)
        {
            return null;
        }

        var key = GenerateKey(property, level, $"Property_{property.Name}", 0);

        return new ExtractedDataSource
        {
            Attribute = dataSourceAttribute,
            Type = DataSourceType.MethodDataSource, // Properties are treated like methods
            IsAsync = IsAsyncType(property.Type),
            Key = key,
            PropertySymbol = property,
            SourceType = property.ContainingType,
            Level = level
        };
    }

    private ExtractedDataSource ExtractAsyncDataSourceGeneratorAttribute(AttributeData attribute, ISymbol symbol,
        DataSourceLevel level, TestMethodMetadata testContext)
    {
        var key = GenerateKey(symbol, level, $"AsyncDataSource_{attribute.AttributeClass!.Name}", attribute.GetHashCode());

        return new ExtractedDataSource
        {
            Attribute = attribute,
            Type = DataSourceType.AsyncDataSourceGenerator,
            IsAsync = true, // AsyncDataSourceGenerator attributes are always treated as async
            Key = key,
            SourceType = GetSourceType(symbol, testContext),
            Level = level
        };
    }

    private ITypeSymbol GetSourceType(ISymbol symbol, TestMethodMetadata testContext)
    {
        return symbol switch
        {
            ITypeSymbol type => type,
            IMethodSymbol method => method.ContainingType,
            IPropertySymbol property => property.ContainingType,
            _ => testContext.TypeSymbol
        };
    }

    private string GenerateKey(ISymbol symbol, DataSourceLevel level, string suffix, int hash)
    {
        var prefix = symbol switch
        {
            ITypeSymbol type => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            IMethodSymbol method => $"{method.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{method.Name}",
            IPropertySymbol property => $"{property.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{property.Name}",
            _ => "Unknown"
        };

        return hash != 0 ? $"{prefix}.{level}.{suffix}_{hash}" : $"{prefix}.{level}.{suffix}";
    }

    private bool IsAsyncMethod(IMethodSymbol method)
    {
        var returnType = method.ReturnType;

        if (returnType is INamedTypeSymbol namedType)
        {
            // Check for IAsyncEnumerable<T>
            if (namedType.IsGenericType && namedType.Name == "IAsyncEnumerable")
            {
                return true;
            }

            // Check for Task<IEnumerable<T>> or ValueTask<IEnumerable<T>>
            if ((namedType.Name == "Task" || namedType.Name == "ValueTask") && namedType.IsGenericType)
            {
                var typeArg = namedType.TypeArguments.FirstOrDefault();
                if (typeArg is INamedTypeSymbol innerType &&
                    innerType.IsGenericType &&
                    innerType.Name == "IEnumerable")
                {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsAsyncType(ITypeSymbol type)
    {
        return type.Name == "IAsyncEnumerable" ||
               (type is INamedTypeSymbol namedType &&
                namedType.IsGenericType &&
                namedType.Name == "IAsyncEnumerable");
    }
}
