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
        // Data source registrations are no longer needed - using inline delegates instead
        // var dataSources = ExtractAllDataSources(testMethods);
        // 
        // foreach (var dataSource in dataSources)
        // {
        //     GenerateDataSourceFactory(writer, dataSource);
        // }
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

    /// <summary>
    /// Generates class-level data source metadata
    /// </summary>
    public void GenerateClassDataSourceMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var classDataSources = ExtractClassDataSources(testInfo);
        
        if (!classDataSources.Any())
        {
            writer.AppendLine("ClassDataSources = Array.Empty<TestDataSource>(),");
            return;
        }

        writer.AppendLine("ClassDataSources = new TestDataSource[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var dataSource in classDataSources)
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

    /// <summary>
    /// Extracts class-level data sources (Arguments and data source generator attributes)
    /// </summary>
    private IEnumerable<DataSourceInfo> ExtractClassDataSources(TestMethodMetadata testInfo)
    {
        var dataSources = new List<DataSourceInfo>();
        
        // Get all class-level attributes
        var classAttributes = testInfo.TypeSymbol.GetAttributes()
            .Where(a => IsClassDataSourceAttribute(a))
            .ToList();

        foreach (var attribute in classAttributes)
        {
            var dataSource = ExtractClassDataSourceFromAttribute(attribute, testInfo);
            if (dataSource != null)
            {
                dataSources.Add(dataSource);
            }
        }

        return dataSources;
    }

    /// <summary>
    /// Checks if an attribute is a class-level data source
    /// </summary>
    private bool IsClassDataSourceAttribute(AttributeData attribute)
    {
        if (attribute.AttributeClass == null)
            return false;

        // Check for Arguments attribute
        if (attribute.AttributeClass.Name == "ArgumentsAttribute")
            return true;

        // Check if it's an AsyncDataSourceGeneratorAttribute
        return IsAsyncDataSourceGeneratorAttributeType(attribute.AttributeClass);
    }

    /// <summary>
    /// Extracts data source info from a class-level attribute
    /// </summary>
    private DataSourceInfo? ExtractClassDataSourceFromAttribute(AttributeData attribute, TestMethodMetadata testInfo)
    {
        var attributeClass = attribute.AttributeClass;
        if (attributeClass == null)
            return null;

        // For Arguments attribute, create a simple data source
        if (attributeClass.Name == "ArgumentsAttribute")
        {
            return new DataSourceInfo
            {
                FactoryKey = $"{testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.ClassArguments_{attribute.GetHashCode()}",
                SourceType = testInfo.TypeSymbol,
                IsAsync = false,
                AttributeData = attribute,
                IsClassLevel = true
            };
        }

        // For other data source attributes
        return new DataSourceInfo
        {
            FactoryKey = $"{testInfo.TypeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)}.ClassDataSource_{attributeClass.Name}_{attribute.GetHashCode()}",
            SourceType = testInfo.TypeSymbol,
            IsAsync = IsAsyncDataSourceAttribute(attributeClass),
            AttributeData = attribute,
            IsClassLevel = true
        };
    }

    /// <summary>
    /// Checks if an attribute class is an async data source
    /// </summary>
    private bool IsAsyncDataSourceAttribute(INamedTypeSymbol attributeClass)
    {
        // Check if it implements IAsyncDataSourceGeneratorAttribute
        if (attributeClass.AllInterfaces.Any(i => 
            i.ToDisplayString() == "TUnit.Core.IAsyncDataSourceGeneratorAttribute"))
            return true;

        // Check base types
        var baseType = attributeClass.BaseType;
        while (baseType != null)
        {
            var baseTypeName = baseType.ToDisplayString();
            if (baseTypeName.StartsWith("TUnit.Core.AsyncDataSourceGeneratorAttribute") ||
                baseTypeName.StartsWith("TUnit.Core.AsyncUntypedDataSourceGeneratorAttribute"))
            {
                return true;
            }
            baseType = baseType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Checks if an attribute class is an AsyncDataSourceGeneratorAttribute by checking inheritance
    /// </summary>
    private bool IsAsyncDataSourceGeneratorAttributeType(INamedTypeSymbol attributeClass)
    {
        // Check if it implements IAsyncDataSourceGeneratorAttribute
        if (attributeClass.AllInterfaces.Any(i => 
            i.ToDisplayString() == "TUnit.Core.IAsyncDataSourceGeneratorAttribute"))
            return true;

        // Check base types for AsyncDataSourceGeneratorAttribute
        var baseType = attributeClass.BaseType;
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
                writer.AppendLine("await foreach (var item in global::TUnit.Core.Helpers.DataConversionHelper.ConvertToAsyncEnumerableInternal(global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays(result), ct))");
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
                writer.AppendLine("await foreach (var item in global::TUnit.Core.Helpers.DataConversionHelper.ConvertToAsyncEnumerableInternal(global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays(result), ct))");
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
                writer.AppendLine($"return global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays({methodCall});");
            }
        }
    }

    private void GeneratePropertyDataSourceFactory(CodeWriter writer, DataSourceInfo dataSource)
    {
        var propertySymbol = dataSource.PropertySymbol!;
        var className = dataSource.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        
        writer.AppendLine($"return {className}.{propertySymbol.Name};");
    }

    private void GenerateUnifiedDataSourceDelegate(CodeWriter writer, DataSourceInfo dataSource)
    {
        var attributeClass = dataSource.AttributeData?.AttributeClass;
        if (attributeClass == null)
            return;

        // Check if this is an async data source generator attribute
        if (IsAsyncDataSourceGeneratorAttributeType(attributeClass))
        {
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
                var constructorArgs = dataSource.AttributeData?.ConstructorArguments ?? ImmutableArray<TypedConstant>.Empty;
                
                if (constructorArgs.Length > 0)
                {
                    if (constructorArgs[0].Kind == TypedConstantKind.Array)
                    {
                        // Multiple types passed as Type[] - create array of instances
                        var types = constructorArgs[0].Values;
                        writer.AppendLine("new object[] {");
                        writer.Indent();
                        
                        for (int i = 0; i < types.Length; i++)
                        {
                            if (types[i].Value is INamedTypeSymbol typeSymbol)
                            {
                                var typeName = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                                writer.Append($"new {typeName}()");
                                if (i < types.Length - 1)
                                    writer.AppendLine(",");
                                else
                                    writer.AppendLine();
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
        else
        {
            // For other data source attributes that don't inherit from AsyncDataSourceGeneratorAttribute
            writer.AppendLine($"// TODO: Handle other data source attribute: {attributeClass.Name}");
            writer.AppendLine($"new StaticTestDataSource(new object?[][] {{ }}),");
        }
    }



    private void GenerateDataSourceInstance(CodeWriter writer, DataSourceInfo dataSource)
    {
        // Handle Arguments attribute directly with inline data
        if (dataSource.AttributeData?.AttributeClass?.Name == "ArgumentsAttribute")
        {
            writer.Append("new StaticTestDataSource(");
            
            // The ArgumentsAttribute constructor takes params object?[] args
            // So the first constructor argument IS the array we need
            var args = dataSource.AttributeData.ConstructorArguments;
            if (args.Length > 0 && args[0].Kind == TypedConstantKind.Array)
            {
                // The arguments are already wrapped in an array by the params parameter
                writer.Append(FormatArgumentValue(args[0]));
            }
            else
            {
                // Fallback for unexpected format
                writer.Append("new object?[] { ");
                for (int i = 0; i < args.Length; i++)
                {
                    writer.Append(FormatArgumentValue(args[i]));
                    if (i < args.Length - 1)
                        writer.Append(", ");
                }
                writer.Append(" }");
            }
            writer.AppendLine("),");
        }
        else if (dataSource.AttributeData != null)
        {
            // Generate unified delegate for ClassDataSourceAttribute and other data source attributes
            GenerateUnifiedDataSourceDelegate(writer, dataSource);
        }
        else if (dataSource.MethodSymbol != null)
        {
            // Generate inline delegate for method data source
            var methodArgs = GenerateMethodArguments(dataSource);
            var className = dataSource.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var methodCall = $"{className}.{dataSource.MethodSymbol.Name}({methodArgs})";
            
            if (dataSource.IsAsync)
            {
                // Check what type of async data source this is
                if (IsAsyncEnumerable(dataSource.MethodSymbol.ReturnType))
                {
                    // IAsyncEnumerable - check if it returns object?[] or needs conversion
                    var hasCancellationToken = dataSource.MethodSymbol.Parameters.Any(p => p.Type.Name == "CancellationToken");
                    var ctParam = hasCancellationToken && string.IsNullOrEmpty(methodArgs) ? "ct" : 
                                 hasCancellationToken && !string.IsNullOrEmpty(methodArgs) ? $"{methodArgs}, ct" : 
                                 methodArgs;
                    var methodCallWithCt = $"{className}.{dataSource.MethodSymbol.Name}({ctParam})";
                    
                    // Check if the async enumerable returns object?[]
                    var asyncEnumType = dataSource.MethodSymbol.ReturnType as INamedTypeSymbol;
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
                        // We'll use a wrapper method to convert the items
                        var elementType = asyncEnumType?.TypeArguments.Length > 0 ? asyncEnumType.TypeArguments[0] : null;
                        if (elementType != null && IsTupleType(elementType))
                        {
                            // Convert tuple to object array
                            var tupleElements = GetTupleElements(elementType);
                            if (tupleElements.Length == 2)
                            {
                                writer.AppendLine($"new AsyncDelegateDataSource((ct) => global::TUnit.Core.Helpers.DataConversionHelper.ConvertAsyncEnumerableTuple2ToObjectArrays({methodCallWithCt})),");
                            }
                            else if (tupleElements.Length == 3)
                            {
                                writer.AppendLine($"new AsyncDelegateDataSource((ct) => global::TUnit.Core.Helpers.DataConversionHelper.ConvertAsyncEnumerableTuple3ToObjectArrays({methodCallWithCt})),");
                            }
                            else
                            {
                                // For other tuple sizes, we need a different approach
                                writer.AppendLine($"new AsyncDelegateDataSource((ct) => global::TUnit.Core.Helpers.DataConversionHelper.ConvertAsyncEnumerableToObjectArrays({methodCallWithCt})),");
                            }
                        }
                        else
                        {
                            // Single value - wrap in object array
                            writer.AppendLine($"new AsyncDelegateDataSource((ct) => global::TUnit.Core.Helpers.DataConversionHelper.ConvertAsyncEnumerableToObjectArrays({methodCallWithCt})),");
                        }
                    }
                }
                else if (IsTaskOfEnumerable(dataSource.MethodSymbol.ReturnType))
                {
                    // Task<IEnumerable> - check if it already returns object arrays
                    var taskType = dataSource.MethodSymbol.ReturnType as INamedTypeSymbol;
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
                else if (IsValueTaskOfEnumerable(dataSource.MethodSymbol.ReturnType))
                {
                    // ValueTask<IEnumerable> - need to convert to Task
                    var valueTaskType = dataSource.MethodSymbol.ReturnType as INamedTypeSymbol;
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
                else
                {
                    // Other async types - use default handling
                    writer.AppendLine($"new AsyncDelegateDataSource(async (ct) => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToAsyncEnumerableInternal(global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays(await {methodCall}), ct)),");
                }
            }
            else
            {
                writer.AppendLine($"new DelegateDataSource(() => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays({methodCall})),");
            }
        }
        else if (dataSource.PropertySymbol != null)
        {
            // Generate inline delegate for property data source
            var className = dataSource.SourceType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            var propertyAccess = $"{className}.{dataSource.PropertySymbol.Name}";
            
            if (dataSource.IsAsync)
            {
                writer.AppendLine($"new AsyncDelegateDataSource(async (ct) => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToAsyncEnumerableInternal(global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays(await {propertyAccess}), ct)),");
            }
            else
            {
                writer.AppendLine($"new DelegateDataSource(() => global::TUnit.Core.Helpers.DataConversionHelper.ConvertToObjectArrays({propertyAccess})),");
            }
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
        public AttributeData? AttributeData { get; init; }
        public bool IsClassLevel { get; init; }
    }
}