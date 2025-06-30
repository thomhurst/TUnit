using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Responsible for generating data source factories and registrations
/// </summary>
internal sealed class DataSourceGenerator
{
    /// <summary>
    /// Generates data source factory registrations
    /// </summary>
    public void GenerateDataSourceRegistrations(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        var dataSources = ExtractAllDataSources(testMethods);
        
        foreach (var dataSource in dataSources)
        {
            GenerateDataSourceFactory(writer, dataSource);
        }
    }

    /// <summary>
    /// Generates async data source wrapper methods for all test methods
    /// </summary>
    public void GenerateAsyncDataSourceWrappers(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        var dataSources = ExtractAllDataSources(testMethods)
            .Where(ds => ds.IsAsync && ds.MethodSymbol != null)
            .ToList();
        
        foreach (var dataSource in dataSources)
        {
            var methodSymbol = dataSource.MethodSymbol!;
            var className = dataSource.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var hasCancellationToken = methodSymbol.Parameters.Any(p => p.Type.Name == "CancellationToken");
            var methodCall = hasCancellationToken ? $"{className}.{methodSymbol.Name}(ct)" : $"{className}.{methodSymbol.Name}()";
            var methodName = $"AsyncDataSourceWrapper_{SafeMethodName(methodSymbol)}";
            
            GenerateAsyncDataSourceWrapper(writer, dataSource, methodName, methodCall);
        }
    }

    /// <summary>
    /// Generates data source metadata for a test method
    /// </summary>
    public void GenerateDataSourceMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var dataSources = ExtractDataSources(testInfo);
        
        if (!dataSources.Any())
        {
            writer.AppendLine("DataSources = Array.Empty<TestDataSource>(),");
            return;
        }

        writer.AppendLine("DataSources = new TestDataSource[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var dataSource in dataSources)
        {
            GenerateDataSourceInstance(writer, dataSource);
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private IEnumerable<DataSourceInfo> ExtractAllDataSources(IEnumerable<TestMethodMetadata> testMethods)
    {
        var dataSources = new Dictionary<string, DataSourceInfo>();

        foreach (var testInfo in testMethods)
        {
            foreach (var ds in ExtractDataSources(testInfo))
            {
                dataSources[ds.FactoryKey] = ds;
            }
        }

        return dataSources.Values;
    }

    private IEnumerable<DataSourceInfo> ExtractDataSources(TestMethodMetadata testInfo)
    {
        var dataSources = new List<DataSourceInfo>();

        // Method data sources
        var methodDataSources = testInfo.MethodSymbol.GetAttributes()
            .Where(a => a.AttributeClass?.Name == "MethodDataSourceAttribute")
            .Select(a => ExtractMethodDataSource(a, testInfo));
        
        dataSources.AddRange(methodDataSources.Where(ds => ds != null)!);

        // Property data sources
        var propertyDataSources = testInfo.TypeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "DataSourceForAttribute"))
            .Select(p => ExtractPropertyDataSource(p, testInfo));
        
        dataSources.AddRange(propertyDataSources.Where(ds => ds != null)!);

        return dataSources;
    }

    private DataSourceInfo? ExtractMethodDataSource(AttributeData attribute, TestMethodMetadata testInfo)
    {
        var methodName = attribute.ConstructorArguments.FirstOrDefault().Value?.ToString();
        if (string.IsNullOrEmpty(methodName))
            return null;

        var sourceType = testInfo.TypeSymbol;
        var methodSymbol = sourceType.GetMembers(methodName!)
            .OfType<IMethodSymbol>()
            .FirstOrDefault();

        if (methodSymbol == null)
            return null;

        return new DataSourceInfo
        {
            FactoryKey = $"{sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{methodName}",
            MethodSymbol = methodSymbol,
            SourceType = sourceType,
            IsAsync = IsAsyncDataSource(methodSymbol)
        };
    }

    private DataSourceInfo? ExtractPropertyDataSource(IPropertySymbol property, TestMethodMetadata testInfo)
    {
        return new DataSourceInfo
        {
            FactoryKey = $"{testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{property.Name}",
            PropertySymbol = property,
            SourceType = testInfo.TypeSymbol,
            IsAsync = IsAsyncDataSource(property.Type)
        };
    }

    private void GenerateDataSourceFactory(CodeWriter writer, DataSourceInfo dataSource)
    {
        writer.AppendLine($"DataSourceFactoryRegistry.Register(\"{dataSource.FactoryKey}\", ct =>");
        writer.AppendLine("{");
        writer.Indent();

        if (dataSource.MethodSymbol != null)
        {
            GenerateMethodDataSourceFactory(writer, dataSource);
        }
        else if (dataSource.PropertySymbol != null)
        {
            GeneratePropertyDataSourceFactory(writer, dataSource);
        }

        writer.Unindent();
        writer.AppendLine("});");
    }

    private void GenerateMethodDataSourceFactory(CodeWriter writer, DataSourceInfo dataSource)
    {
        var methodSymbol = dataSource.MethodSymbol!;
        var className = dataSource.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (dataSource.IsAsync)
        {
            // All async methods need to be converted to sync using ConvertToSync helper
            var hasCancellationToken = methodSymbol.Parameters.Any(p => p.Type.Name == "CancellationToken");
            var methodCall = hasCancellationToken ? $"{className}.{methodSymbol.Name}(ct)" : $"{className}.{methodSymbol.Name}()";
            
            // Generate a named async iterator method for this data source
            var methodName = $"AsyncDataSourceWrapper_{SafeMethodName(methodSymbol)}";
            
            // Generate the async wrapper method first
            GenerateAsyncDataSourceWrapper(writer, dataSource, methodName, methodCall);
            
            // Now use it in the factory
            writer.AppendLine($"return ConvertToSync({methodName});");
        }
        else
        {
            writer.AppendLine($"return {className}.{methodSymbol.Name}();");
        }
    }

    private void GeneratePropertyDataSourceFactory(CodeWriter writer, DataSourceInfo dataSource)
    {
        var propertySymbol = dataSource.PropertySymbol!;
        var className = dataSource.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        writer.AppendLine($"return {className}.{propertySymbol.Name};");
    }

    private void GenerateDataSourceInstance(CodeWriter writer, DataSourceInfo dataSource)
    {
        if (dataSource.IsAsync)
        {
            writer.AppendLine($"new AsyncDynamicTestDataSource {{ FactoryKey = \"{dataSource.FactoryKey}\" }},");
        }
        else
        {
            writer.AppendLine($"new DynamicTestDataSource(true) {{ FactoryKey = \"{dataSource.FactoryKey}\" }},");
        }
    }

    private bool IsAsyncDataSource(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        
        if (returnType is INamedTypeSymbol namedType)
        {
            // Check for IAsyncEnumerable<T>
            if (namedType.IsGenericType && namedType.Name == "IAsyncEnumerable")
                return true;
                
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

    private bool IsAsyncDataSource(ITypeSymbol type)
    {
        return type.Name == "IAsyncEnumerable" ||
               (type is INamedTypeSymbol namedType && 
                namedType.IsGenericType && 
                namedType.Name == "IAsyncEnumerable");
    }

    private bool IsObjectArrayType(ITypeSymbol type)
    {
        return type.ToDisplayString() == "object[]" || type.ToDisplayString() == "object?[]";
    }

    private bool IsTupleType(ITypeSymbol type)
    {
        return type.IsTupleType || type.ToDisplayString().StartsWith("(");
    }

    private string SafeMethodName(IMethodSymbol method)
    {
        return method.ContainingType.Name.Replace(".", "_") + "_" + method.Name;
    }

    private void GenerateAsyncDataSourceWrapper(CodeWriter writer, DataSourceInfo dataSource, string methodName, string methodCall)
    {
        var methodSymbol = dataSource.MethodSymbol!;
        
        writer.AppendLine($"static async IAsyncEnumerable<object?[]> {methodName}(CancellationToken ct)");
        writer.AppendLine("{");
        writer.Indent();
        
        // Handle different async return types
        if (methodSymbol.ReturnType.Name == "IAsyncEnumerable")
        {
            // Check if we need to convert the async enumerable to object?[]
            var returnType = methodSymbol.ReturnType as INamedTypeSymbol;
            var typeArg = returnType?.TypeArguments.FirstOrDefault();
            
            if (typeArg != null && !IsObjectArrayType(typeArg))
            {
                // Need to convert each item to object?[]
                writer.AppendLine($"await foreach (var item in {methodCall})");
                writer.AppendLine("{");
                writer.Indent();
                if (IsTupleType(typeArg))
                {
                    // Handle tuple types by converting to object array
                    writer.AppendLine("yield return new object?[] { item.Item1, item.Item2 };");
                }
                else
                {
                    writer.AppendLine("yield return new object?[] { item };");
                }
                writer.Unindent();
                writer.AppendLine("}");
            }
            else
            {
                writer.AppendLine($"await foreach (var item in {methodCall})");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("yield return item;");
                writer.Unindent();
                writer.AppendLine("}");
            }
        }
        else
        {
            // Task<IEnumerable<T>> or ValueTask<IEnumerable<T>>
            writer.AppendLine($"var result = await {methodCall};");
            writer.AppendLine("foreach (var item in result)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("yield return item;");
            writer.Unindent();
            writer.AppendLine("}");
        }
        
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
    }


    private class DataSourceInfo
    {
        public required string FactoryKey { get; init; }
        public IMethodSymbol? MethodSymbol { get; init; }
        public IPropertySymbol? PropertySymbol { get; init; }
        public required ITypeSymbol SourceType { get; init; }
        public required bool IsAsync { get; init; }
    }
}