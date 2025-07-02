using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

        // Extract Arguments property if present
        var argumentsValue = attribute.NamedArguments
            .FirstOrDefault(na => na.Key == "Arguments")
            .Value;
            
        var methodArguments = argumentsValue.Kind == TypedConstantKind.Array 
            ? argumentsValue.Values 
            : default(ImmutableArray<TypedConstant>);

        return new DataSourceInfo
        {
            FactoryKey = $"{sourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.{methodName}",
            MethodSymbol = methodSymbol,
            SourceType = sourceType,
            IsAsync = IsAsyncDataSource(methodSymbol),
            MethodArguments = methodArguments
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
        if (dataSource.IsAsync)
        {
            // Use unique factory method name to avoid conflicts
            var factoryMethodName = $"DataSourceFactory_{dataSource.FactoryKey.Replace(".", "_").Replace("::", "_")}";
            
            // Generate factory method
            writer.AppendLine($"async IAsyncEnumerable<object?[]> {factoryMethodName}([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)");
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
            writer.AppendLine("}");
            
            // Register the factory
            writer.AppendLine($"global::TUnit.Core.DataSourceFactoryStorage.RegisterFactory(\"{dataSource.FactoryKey}\", {factoryMethodName});");
        }
        else
        {
            writer.AppendLine($"global::TUnit.Core.TestDelegateStorage.RegisterDataSourceFactory(\"{dataSource.FactoryKey}\", () =>");
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
    }

    private void GenerateMethodDataSourceFactory(CodeWriter writer, DataSourceInfo dataSource)
    {
        var methodSymbol = dataSource.MethodSymbol!;
        var className = dataSource.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Generate method arguments if any
        var methodArgs = GenerateMethodArguments(dataSource);

        if (dataSource.IsAsync)
        {
            // For async methods, we need to generate an async enumerable
            var hasCancellationToken = methodSymbol.Parameters.Any(p => p.Type.Name == "CancellationToken");
            var ctParam = hasCancellationToken && string.IsNullOrEmpty(methodArgs) ? "ct" : 
                         hasCancellationToken && !string.IsNullOrEmpty(methodArgs) ? $"{methodArgs}, ct" : 
                         methodArgs;
            var methodCall = $"{className}.{methodSymbol.Name}({ctParam})";
            
            // Check if it returns IAsyncEnumerable
            if (IsAsyncEnumerable(methodSymbol.ReturnType))
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
                        var tupleElements = GetTupleElements(typeArg);
                        if (tupleElements.Length == 2)
                        {
                            writer.AppendLine("yield return new object?[] { item.Item1, item.Item2 };");
                        }
                        else if (tupleElements.Length == 3)
                        {
                            writer.AppendLine("yield return new object?[] { item.Item1, item.Item2, item.Item3 };");
                        }
                        else
                        {
                            // Generic tuple handling
                            writer.AppendLine("// Converting tuple to object array");
                            writer.AppendLine("yield return ConvertTupleToObjectArray(item);");
                        }
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
                    // Already returns IAsyncEnumerable<object?[]>
                    writer.AppendLine($"await foreach (var item in {methodCall})");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine("yield return item;");
                    writer.Unindent();
                    writer.AppendLine("}");
                }
            }
            else if (IsTaskOfEnumerable(methodSymbol.ReturnType))
            {
                // Task<IEnumerable<T>> - need to convert to IAsyncEnumerable
                writer.AppendLine($"var result = await {methodCall};");
                writer.AppendLine("await foreach (var item in ConvertToAsyncEnumerableInternal(ConvertToObjectArrays(result), ct))");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("yield return item;");
                writer.Unindent();
                writer.AppendLine("}");
            }
            else if (IsValueTaskOfEnumerable(methodSymbol.ReturnType))
            {
                // ValueTask<IEnumerable<T>> - need to convert to IAsyncEnumerable
                writer.AppendLine($"var result = await {methodCall};");
                writer.AppendLine("await foreach (var item in ConvertToAsyncEnumerableInternal(ConvertToObjectArrays(result), ct))");
                writer.AppendLine("{");
                writer.Indent();
                writer.AppendLine("yield return item;");
                writer.Unindent();
                writer.AppendLine("}");
            }
        }
        else
        {
            // Check the return type and generate appropriate conversion
            var returnType = methodSymbol.ReturnType;
            var methodCall = $"{className}.{methodSymbol.Name}({methodArgs})";
            
            if (IsObjectArrayEnumerable(returnType))
            {
                // Already returns IEnumerable<object?[]>
                writer.AppendLine($"return {methodCall};");
            }
            else
            {
                // Need to convert to IEnumerable<object?[]>
                writer.AppendLine($"return ConvertToObjectArrays({methodCall});");
            }
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
    
    private bool IsObjectArrayEnumerable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            var typeName = namedType.Name;
            if (typeName == "IEnumerable" || typeName == "IAsyncEnumerable" || 
                typeName == "List" || typeName == "Array")
            {
                var elementType = namedType.TypeArguments.FirstOrDefault();
                return elementType != null && IsObjectArrayType(elementType);
            }
        }
        return false;
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
    
    /// <summary>
    /// Generates a helper method that converts various data source return types to IEnumerable<object?[]>
    /// </summary>
    public void GenerateConversionHelpers(CodeWriter writer)
    {
        // Generate async enumerable conversion helper
        writer.AppendLine("private static async IAsyncEnumerable<object?[]> ConvertToAsyncEnumerableInternal(");
        writer.AppendLine("    IEnumerable<object?[]> data,");
        writer.AppendLine("    [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("await Task.Yield(); // Ensure async behavior");
        writer.AppendLine("foreach (var item in data)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("ct.ThrowIfCancellationRequested();");
        writer.AppendLine("yield return item;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        writer.AppendLine("private static IEnumerable<object?[]> ConvertToObjectArrays(object data)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine("switch (data)");
        writer.AppendLine("{");
        writer.Indent();
        
        // Handle IEnumerable<object?[]> - pass through
        writer.AppendLine("case IEnumerable<object?[]> objectArrays:");
        writer.Indent();
        writer.AppendLine("return objectArrays;");
        writer.Unindent();
        
        // Handle single values
        writer.AppendLine("case string str:");
        writer.AppendLine("case int i:");
        writer.AppendLine("case long l:");
        writer.AppendLine("case double d:");
        writer.AppendLine("case float f:");
        writer.AppendLine("case decimal dec:");
        writer.AppendLine("case bool b:");
        writer.AppendLine("case char c:");
        writer.AppendLine("case byte bt:");
        writer.AppendLine("case sbyte sb:");
        writer.AppendLine("case short s:");
        writer.AppendLine("case ushort us:");
        writer.AppendLine("case uint ui:");
        writer.AppendLine("case ulong ul:");
        writer.Indent();
        writer.AppendLine("return new[] { new object?[] { data } };");
        writer.Unindent();
        
        // Handle arrays of primitives
        writer.AppendLine("case int[] intArray:");
        writer.Indent();
        writer.AppendLine("return intArray.Select(x => new object?[] { x });");
        writer.Unindent();
        
        writer.AppendLine("case string[] stringArray:");
        writer.Indent();
        writer.AppendLine("return stringArray.Select(x => new object?[] { x });");
        writer.Unindent();
        
        // Handle IEnumerable<T> where T is not object[]
        writer.AppendLine("case System.Collections.IEnumerable enumerable:");
        writer.Indent();
        writer.AppendLine("var result = new List<object?[]>();");
        writer.AppendLine("foreach (var item in enumerable)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Handle tuples - check for common tuple types");
        writer.AppendLine("var itemType = item?.GetType();");
        writer.AppendLine("if (itemType != null && itemType.IsGenericType)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("var genericTypeDef = itemType.GetGenericTypeDefinition();");
        writer.AppendLine("var typeName = genericTypeDef.FullName;");
        writer.AppendLine("if (typeName != null && typeName.StartsWith(\"System.ValueTuple`\"))");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("// Extract tuple items using reflection");
        writer.AppendLine("var fields = itemType.GetFields();");
        writer.AppendLine("var tupleItems = new object?[fields.Length];");
        writer.AppendLine("for (int i = 0; i < fields.Length; i++)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("tupleItems[i] = fields[i].GetValue(item);");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("result.Add(tupleItems);");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("else if (item is object?[] objArray)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("result.Add(objArray);");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("else");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("result.Add(new object?[] { item });");
        writer.Unindent();
        writer.AppendLine("}");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("return result;");
        writer.Unindent();
        
        // Handle single Func values
        writer.AppendLine("case System.Func<object> func:");
        writer.AppendLine("case System.Delegate del:");
        writer.Indent();
        writer.AppendLine("return new[] { new object?[] { data } };");
        writer.Unindent();
        
        // Default case
        writer.AppendLine("default:");
        writer.Indent();
        writer.AppendLine("throw new InvalidOperationException($\"Cannot convert {data?.GetType()?.FullName ?? \"null\"} to IEnumerable<object?[]>\");");
        writer.Unindent();
        
        writer.Unindent();
        writer.AppendLine("}");
        
        writer.Unindent();
        writer.AppendLine("}");
    }
    
    private string GenerateMethodArguments(DataSourceInfo dataSource)
    {
        if (dataSource.MethodArguments == null || dataSource.MethodArguments.Length == 0)
            return string.Empty;
            
        var args = new List<string>();
        foreach (var arg in dataSource.MethodArguments)
        {
            args.Add(FormatArgumentValue(arg));
        }
        
        return string.Join(", ", args);
    }
    
    private string FormatArgumentValue(TypedConstant arg)
    {
        if (arg.IsNull)
            return "null";
            
        switch (arg.Kind)
        {
            case TypedConstantKind.Primitive:
                if (arg.Type?.SpecialType == SpecialType.System_String)
                    return $"\"{arg.Value}\"";
                if (arg.Type?.SpecialType == SpecialType.System_Char)
                    return $"'{arg.Value}'";
                if (arg.Type?.SpecialType == SpecialType.System_Boolean)
                    return arg.Value?.ToString()?.ToLowerInvariant() ?? "false";
                return arg.Value?.ToString() ?? "null";
                
            case TypedConstantKind.Type:
                var type = (ITypeSymbol)arg.Value!;
                return $"typeof({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";
                
            case TypedConstantKind.Array:
                var elements = arg.Values.Select(FormatArgumentValue);
                return $"new[] {{ {string.Join(", ", elements)} }}";
                
            default:
                return "null";
        }
    }
    
    private bool IsAsyncEnumerable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType)
        {
            return namedType.Name == "IAsyncEnumerable" || 
                   namedType.AllInterfaces.Any(i => i.Name == "IAsyncEnumerable");
        }
        return false;
    }
    
    private bool IsTaskOfEnumerable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.Name == "Task" && namedType.TypeArguments.Length == 1)
        {
            var innerType = namedType.TypeArguments[0];
            return innerType is INamedTypeSymbol innerNamed && 
                   (innerNamed.Name == "IEnumerable" || innerNamed.AllInterfaces.Any(i => i.Name == "IEnumerable"));
        }
        return false;
    }
    
    private bool IsValueTaskOfEnumerable(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.Name == "ValueTask" && namedType.TypeArguments.Length == 1)
        {
            var innerType = namedType.TypeArguments[0];
            return innerType is INamedTypeSymbol innerNamed && 
                   (innerNamed.Name == "IEnumerable" || innerNamed.AllInterfaces.Any(i => i.Name == "IEnumerable"));
        }
        return false;
    }
    
    private ITypeSymbol[] GetTupleElements(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsTupleType)
        {
            return namedType.TupleElements.Select(e => e.Type).ToArray();
        }
        return Array.Empty<ITypeSymbol>();
    }


    private class DataSourceInfo
    {
        public required string FactoryKey { get; init; }
        public IMethodSymbol? MethodSymbol { get; init; }
        public IPropertySymbol? PropertySymbol { get; init; }
        public required ITypeSymbol SourceType { get; init; }
        public required bool IsAsync { get; init; }
        public ImmutableArray<TypedConstant> MethodArguments { get; init; }
    }
}