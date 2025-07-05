using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Generators.DataSources;

/// <summary>
/// Unified implementation of data source code generation
/// </summary>
public sealed class UnifiedDataSourceCodeGenerator : IDataSourceCodeGenerator
{
    /// <summary>
    /// Generates code for a data source instance
    /// </summary>
    public void GenerateDataSourceInstance(CodeWriter writer, ExtractedDataSource dataSource)
    {
        switch (dataSource.Type)
        {
            case DataSourceType.Arguments:
                GenerateArgumentsDataSource(writer, dataSource);
                break;

            case DataSourceType.MethodDataSource:
                if (dataSource.MethodSymbol != null)
                {
                    GenerateMethodDataSource(writer, dataSource);
                }
                else if (dataSource.PropertySymbol != null)
                {
                    GeneratePropertyDataSource(writer, dataSource);
                }
                break;

            case DataSourceType.AsyncDataSourceGenerator:
                GenerateAsyncDataSourceGeneratorDataSource(writer, dataSource);
                break;
        }
    }

    private void GenerateArgumentsDataSource(CodeWriter writer, ExtractedDataSource dataSource)
    {
        writer.Append("new StaticTestDataSource(");

        // The ArgumentsAttribute constructor takes params object?[] args
        var args = dataSource.Attribute.ConstructorArguments;

        if (args.IsDefaultOrEmpty)
        {
            writer.Append("new object?[][] { new object?[] { } }");
        }
        // Handle the params array - Roslyn always passes params as a single array argument
        else if (args.Length > 0)
        {
            var firstArg = args[0];

            if (firstArg.IsNull)
            {
                writer.Append("new object?[][] { new object?[] { null } }");
            }
            else if (firstArg.Kind == TypedConstantKind.Array)
            {
                // Extract the array values
                var arrayValues = firstArg.Values;

                // Special case: if the array is empty, it means [Arguments()] was used
                if (arrayValues.Length == 0)
                {
                    writer.Append("new object?[][] { new object?[] { } }");
                }
                else
                {
                    writer.Append("new object?[][] { new object?[] { ");
                    for (var i = 0; i < arrayValues.Length; i++)
                    {
                        writer.Append(FormatArgumentValue(arrayValues[i]));
                        if (i < arrayValues.Length - 1)
                        {
                            writer.Append(", ");
                        }
                    }
                    writer.Append(" } }");
                }
            }
            else
            {
                // Single non-array argument (shouldn't happen with params, but handle it)
                writer.Append("new object?[][] { new object?[] { ");
                writer.Append(FormatArgumentValue(firstArg));
                writer.Append(" } }");
            }
        }
        else
        {
            // No arguments provided - [Arguments()]
            writer.Append("new object?[][] { new object?[] { } }");
        }

        writer.AppendLine("),");
    }

    private void GenerateMethodDataSource(CodeWriter writer, ExtractedDataSource dataSource)
    {
        var methodSymbol = dataSource.MethodSymbol!;
        var methodArgs = GenerateMethodArguments(dataSource.Attribute);
        var className = dataSource.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var methodCall = $"{className}.{methodSymbol.Name}({methodArgs})";

        if (dataSource.IsAsync)
        {
            GenerateAsyncMethodDataSource(writer, dataSource, methodCall, methodArgs);
        }
        else
        {
            writer.AppendLine($"new DelegateDataSource(() => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays({methodCall})),");
        }
    }

    private void GenerateAsyncMethodDataSource(CodeWriter writer, ExtractedDataSource dataSource,
        string methodCall, string methodArgs)
    {
        var methodSymbol = dataSource.MethodSymbol!;
        var className = dataSource.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        // Check what type of async data source this is
        if (IsAsyncEnumerable(methodSymbol.ReturnType))
        {
            GenerateAsyncEnumerableDataSource(writer, dataSource, className, methodArgs);
        }
        else if (IsTaskOfEnumerable(methodSymbol.ReturnType))
        {
            GenerateTaskOfEnumerableDataSource(writer, methodCall, methodSymbol.ReturnType);
        }
        else if (IsValueTaskOfEnumerable(methodSymbol.ReturnType))
        {
            GenerateValueTaskOfEnumerableDataSource(writer, methodCall, methodSymbol.ReturnType);
        }
        else
        {
            // Other async types - use default handling
            writer.AppendLine($"new AsyncDelegateDataSource(async (ct) => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToAsyncEnumerableInternal(global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays(await {methodCall}), ct)),");
        }
    }

    private void GenerateAsyncEnumerableDataSource(CodeWriter writer, ExtractedDataSource dataSource,
        string className, string methodArgs)
    {
        var methodSymbol = dataSource.MethodSymbol!;
        var hasCancellationToken = methodSymbol.Parameters.Any(p => p.Type.Name == "CancellationToken");
        var ctParam = hasCancellationToken && string.IsNullOrEmpty(methodArgs) ? "ct" :
                     hasCancellationToken && !string.IsNullOrEmpty(methodArgs) ? $"{methodArgs}, ct" :
                     methodArgs;
        var methodCallWithCt = $"{className}.{methodSymbol.Name}({ctParam})";

        // Check if the async enumerable returns object?[]
        var asyncEnumType = methodSymbol.ReturnType as INamedTypeSymbol;
        if (asyncEnumType != null &&
            asyncEnumType.TypeArguments.Length > 0 &&
            IsObjectArrayType(asyncEnumType.TypeArguments[0]))
        {
            // Already returns IAsyncEnumerable<object?[]> - use directly
            writer.AppendLine($"new AsyncDelegateDataSource((ct) => {methodCallWithCt}),");
        }
        else
        {
            // Returns IAsyncEnumerable<T> where T is not object?[] - need to convert
            var elementType = asyncEnumType?.TypeArguments.Length > 0 ? asyncEnumType.TypeArguments[0] : null;
            if (elementType != null && IsTupleType(elementType))
            {
                // Convert tuple to object array
                var tupleElements = GetTupleElements(elementType);
                var helperMethod = tupleElements.Length switch
                {
                    2 => "ConvertAsyncEnumerableTuple2ToObjectArrays",
                    3 => "ConvertAsyncEnumerableTuple3ToObjectArrays",
                    4 => "ConvertAsyncEnumerableTuple4ToObjectArrays",
                    5 => "ConvertAsyncEnumerableTuple5ToObjectArrays",
                    _ => "ConvertAsyncEnumerableToObjectArrays"
                };
                writer.AppendLine($"new AsyncDelegateDataSource((ct) => global::TUnit.Core.Helpers.DataConversionHelper.{helperMethod}({methodCallWithCt})),");
            }
            else
            {
                // Single value - wrap in object array
                writer.AppendLine($"new AsyncDelegateDataSource((ct) => global::TUnit.Core.Helpers.DataConversionHelper.ConvertAsyncEnumerableToObjectArrays({methodCallWithCt})),");
            }
        }
    }

    private void GenerateTaskOfEnumerableDataSource(CodeWriter writer, string methodCall, ITypeSymbol returnType)
    {
        var taskType = returnType as INamedTypeSymbol;
        if (taskType != null && taskType.TypeArguments.Length > 0)
        {
            var innerType = taskType.TypeArguments[0] as INamedTypeSymbol;
            if (innerType != null && innerType.IsGenericType && innerType.TypeArguments.Length > 0 &&
                IsObjectArrayType(innerType.TypeArguments[0]))
            {
                // Already returns Task<IEnumerable<object?[]>>
                writer.AppendLine($"new TaskDelegateDataSource(() => {methodCall}),");
            }
            else
            {
                // Need to convert - wrap in async lambda that converts the result
                writer.AppendLine($"new TaskDelegateDataSource(async () => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays(await {methodCall})),");
            }
        }
        else
        {
            // Fallback
            writer.AppendLine($"new TaskDelegateDataSource(async () => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays(await {methodCall})),");
        }
    }

    private void GenerateValueTaskOfEnumerableDataSource(CodeWriter writer, string methodCall, ITypeSymbol returnType)
    {
        var valueTaskType = returnType as INamedTypeSymbol;
        if (valueTaskType != null && valueTaskType.TypeArguments.Length > 0)
        {
            var innerType = valueTaskType.TypeArguments[0] as INamedTypeSymbol;
            if (innerType != null && innerType.IsGenericType && innerType.TypeArguments.Length > 0 &&
                IsObjectArrayType(innerType.TypeArguments[0]))
            {
                // Already returns ValueTask<IEnumerable<object?[]>> - convert to Task
                writer.AppendLine($"new TaskDelegateDataSource(async () => await {methodCall}),");
            }
            else
            {
                // Need to convert - wrap in async lambda that converts the result
                writer.AppendLine($"new TaskDelegateDataSource(async () => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays(await {methodCall})),");
            }
        }
        else
        {
            // Fallback
            writer.AppendLine($"new TaskDelegateDataSource(async () => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays(await {methodCall})),");
        }
    }

    private void GeneratePropertyDataSource(CodeWriter writer, ExtractedDataSource dataSource)
    {
        var propertySymbol = dataSource.PropertySymbol!;
        var className = dataSource.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var propertyAccess = $"{className}.{propertySymbol.Name}";

        // Properties should only provide a single value, not multiple
        // The first value from the enumerable is used for property injection
        writer.AppendLine($"new DelegateDataSource(() => {{");
        writer.Indent();
        writer.AppendLine($"var values = global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays({propertyAccess}).ToArray();");
        writer.AppendLine("return System.Linq.Enumerable.Take(values, 1);");
        writer.Unindent();
        writer.AppendLine("}),");
    }

    private void GenerateAsyncDataSourceGeneratorDataSource(CodeWriter writer, ExtractedDataSource dataSource)
    {
        var attributeClass = dataSource.Attribute.AttributeClass;
        if (attributeClass == null)
        {
            return;
        }

        // Use the same ConvertToObjectArrays pattern as method data sources
        writer.AppendLine($"new DelegateDataSource(() => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays(");
        writer.Indent();

        // Generate the raw data - single value or array of values
        if (attributeClass.IsGenericType)
        {
            // Generic ClassDataSourceAttribute<T> - create instance of T
            var typeArg = attributeClass.TypeArguments[0];
            var className = typeArg.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            writer.AppendLine($"new {className}()");
        }
        else
        {
            // Non-generic ClassDataSourceAttribute - can take single Type or Type[] as constructor arguments
            var constructorArgs = dataSource.Attribute.ConstructorArguments;

            if (constructorArgs.Length > 0)
            {
                if (constructorArgs[0].Kind == TypedConstantKind.Array)
                {
                    // Multiple types passed as Type[] - create array of instances
                    var types = constructorArgs[0].Values;
                    writer.AppendLine("new object[] {");
                    writer.Indent();

                    for (var i = 0; i < types.Length; i++)
                    {
                        if (types[i].Value is INamedTypeSymbol typeSymbol)
                        {
                            var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                            writer.Append($"new {typeName}()");
                            if (i < types.Length - 1)
                            {
                                writer.AppendLine(",");
                            }
                            else
                            {
                                writer.AppendLine();
                            }
                        }
                    }

                    writer.Unindent();
                    writer.AppendLine("}");
                }
                else if (constructorArgs[0].Kind == TypedConstantKind.Type && constructorArgs[0].Value is INamedTypeSymbol singleType)
                {
                    // Single type passed
                    var typeName = singleType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                    writer.AppendLine($"new {typeName}()");
                }
                else
                {
                    // Fallback
                    writer.AppendLine("new object[0]");
                }
            }
            else
            {
                // No constructor arguments
                writer.AppendLine("new object[0]");
            }
        }

        writer.Unindent();
        writer.AppendLine(")),");
    }

    private string GenerateMethodArguments(AttributeData attribute)
    {
        // Extract Arguments property if present (for MethodDataSourceAttribute)
        var argumentsValue = attribute.NamedArguments
            .FirstOrDefault(na => na.Key == "Arguments")
            .Value;

        if (argumentsValue.Kind != TypedConstantKind.Array)
        {
            return string.Empty;
        }

        var args = new List<string>();
        foreach (var arg in argumentsValue.Values)
        {
            args.Add(FormatArgumentValue(arg));
        }

        return string.Join(", ", args);
    }

    private string FormatArgumentValue(TypedConstant arg)
    {
        if (arg.IsNull)
        {
            return "null";
        }

        switch (arg.Kind)
        {
            case TypedConstantKind.Primitive:
                if (arg.Type?.SpecialType == SpecialType.System_String)
                {
                    if (arg.Value == null)
                    {
                        return "null";
                    }

                    var value = arg.Value.ToString() ?? "";
                    // Escape backslashes first, then quotes
                    value = value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                    // Handle special characters
                    value = value.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
                    return $"\"{value}\"";
                }
                if (arg.Type?.SpecialType == SpecialType.System_Char)
                {
                    if (arg.Value == null)
                    {
                        return "null";
                    }
                    return $"'{arg.Value}'";
                }
                if (arg.Type?.SpecialType == SpecialType.System_Boolean)
                {
                    return arg.Value?.ToString()?.ToLowerInvariant() ?? "false";
                }
                return arg.Value?.ToString() ?? "null";

            case TypedConstantKind.Type:
                if (arg.Value == null)
                {
                    return "null";
                }
                var type = (ITypeSymbol)arg.Value;
                return $"typeof({type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)})";

            case TypedConstantKind.Array:
                var elements = arg.Values.Select(FormatArgumentValue);
                // For array types, we need to get the element type and add []
                var arrayType = arg.Type as IArrayTypeSymbol;
                var elementType = arrayType?.ElementType?.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat) ?? "object?";
                return $"new {elementType}[] {{ {string.Join(", ", elements)} }}";

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

    private bool IsObjectArrayType(ITypeSymbol type)
    {
        return type.ToDisplayString() == "object[]" || type.ToDisplayString() == "object?[]";
    }

    private bool IsTupleType(ITypeSymbol type)
    {
        return type.IsTupleType || type.ToDisplayString().StartsWith("(");
    }

    private ITypeSymbol[] GetTupleElements(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol namedType && namedType.IsTupleType)
        {
            return namedType.TupleElements.Select(e => e.Type).ToArray();
        }
        return Array.Empty<ITypeSymbol>();
    }

}
