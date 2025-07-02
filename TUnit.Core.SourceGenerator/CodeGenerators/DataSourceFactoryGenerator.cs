using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Generates type-safe data source factories with full async and cancellation support
/// </summary>
internal class DataSourceFactoryGenerator
{
    private readonly StringBuilder _stringBuilder = new();
    
    public string GenerateDataSourceFactories(List<DataSourceInfo> dataSources)
    {
        _stringBuilder.Clear();
        
        if (!dataSources.Any())
            return string.Empty;
            
        _stringBuilder.AppendLine(@"
    #region Data Source Factories
");
        
        foreach (var dataSource in dataSources)
        {
            GenerateDataSourceFactory(dataSource);
        }
        
        // Generate registration method
        GenerateDataSourceRegistrations(dataSources);
        
        _stringBuilder.AppendLine(@"
    #endregion
");
        
        GenerateDataSourceHelpers();
        
        return _stringBuilder.ToString();
    }
    
    private void GenerateDataSourceFactory(DataSourceInfo dataSource)
    {
        var factoryName = $"{dataSource.SafeTypeName}_{dataSource.SafeMemberName}_Factory";
        
        if (dataSource.IsAsync)
        {
            GenerateAsyncDataSourceFactory(dataSource, factoryName);
        }
        else
        {
            GenerateSyncDataSourceFactory(dataSource, factoryName);
        }
    }
    
    private void GenerateSyncDataSourceFactory(DataSourceInfo dataSource, string factoryName)
    {
        _stringBuilder.AppendLine($@"
    private static Func<CancellationToken, IAsyncEnumerable<object?[]>> {factoryName} = async (ct) =>
    {{
        var rawData = {GetDataSourceAccess(dataSource)};
        var data = ConvertToObjectArrays(rawData);
        return ConvertToAsyncEnumerable(data, ct);
    }};");
    }
    
    private void GenerateAsyncDataSourceFactory(DataSourceInfo dataSource, string factoryName)
    {
        var returnType = dataSource.ReturnType;
        
        if (IsTaskOfEnumerable(returnType))
        {
            // Task<IEnumerable<T>>
            _stringBuilder.AppendLine($@"
    private static Func<CancellationToken, IAsyncEnumerable<object?[]>> {factoryName} = async (ct) =>
    {{
        var rawData = await {GetDataSourceAccess(dataSource)};
        var data = ConvertToObjectArrays(rawData);
        return ConvertToAsyncEnumerable(data, ct);
    }};");
        }
        else if (IsAsyncEnumerable(returnType))
        {
            // IAsyncEnumerable<T>
            _stringBuilder.AppendLine($@"
    private static Func<CancellationToken, IAsyncEnumerable<object?[]>> {factoryName} = (ct) =>
    {{
        var data = {GetDataSourceAccess(dataSource)};
        return ConvertAsyncEnumerableWithCancellation(data, ct);
    }};");
        }
        else if (IsValueTaskOfEnumerable(returnType))
        {
            // ValueTask<IEnumerable<T>>
            _stringBuilder.AppendLine($@"
    private static Func<CancellationToken, IAsyncEnumerable<object?[]>> {factoryName} = async (ct) =>
    {{
        var rawData = await {GetDataSourceAccess(dataSource)};
        var data = ConvertToObjectArrays(rawData);
        return ConvertToAsyncEnumerable(data, ct);
    }};");
        }
    }
    
    private void GenerateDataSourceRegistrations(List<DataSourceInfo> dataSources)
    {
        _stringBuilder.AppendLine(@"
    private static void RegisterDataSourceFactories()
    {");
        
        foreach (var dataSource in dataSources)
        {
            var factoryName = $"{dataSource.SafeTypeName}_{dataSource.SafeMemberName}_Factory";
            _stringBuilder.AppendLine($@"        global::TUnit.Core.TestDelegateStorage.RegisterDataSourceFactory(""{dataSource.FactoryKey}"", {factoryName});");
        }
        
        _stringBuilder.AppendLine(@"    }");
    }
    
    private string GetDataSourceAccess(DataSourceInfo dataSource)
    {
        if (dataSource.IsProperty)
        {
            return $"{dataSource.ContainingTypeName}.{dataSource.MemberName}";
        }
        else
        {
            // Method - handle parameters
            var parameters = BuildMethodParameters(dataSource);
            return $"{dataSource.ContainingTypeName}.{dataSource.MemberName}({parameters})";
        }
    }
    
    private string BuildMethodParameters(DataSourceInfo dataSource)
    {
        if (!dataSource.Parameters.Any())
            return string.Empty;
            
        var parameterList = new List<string>();
        
        foreach (var param in dataSource.Parameters)
        {
            if (param.HasDefaultValue)
            {
                // Use default value if available
                parameterList.Add(GetDefaultValueString(param));
            }
            else if (param.IsParams)
            {
                // Handle params array
                parameterList.Add($"Array.Empty<{param.ElementType}>()");
            }
            else
            {
                // This shouldn't happen if validation is correct
                parameterList.Add("null");
            }
        }
        
        return string.Join(", ", parameterList);
    }
    
    private string GetDefaultValueString(ParameterInfo param)
    {
        if (param.DefaultValue == null)
            return "null";
            
        var type = param.ParameterType;
        
        // Handle common default value scenarios
        if (type == "string")
            return $@"""{param.DefaultValue}""";
        else if (type == "char")
            return $"'{param.DefaultValue}'";
        else if (type == "bool")
            return param.DefaultValue.ToString()?.ToLowerInvariant() ?? "false";
        else if (IsNumericType(type))
            return param.DefaultValue.ToString() ?? "0";
        else
            return "default";
    }
    
    private void GenerateDataSourceHelpers()
    {
        _stringBuilder.AppendLine(@"
    #region Data Source Conversion Helpers
    
    private static IEnumerable<object?[]> ConvertToObjectArrays(object? rawData)
    {
        if (rawData is IEnumerable<object?[]> arrays)
            return arrays;
            
        if (rawData is System.Collections.IEnumerable enumerable)
        {
            var result = new List<object?[]>();
            foreach (var item in enumerable)
            {
                result.Add(ConvertToObjectArray(item));
            }
            return result;
        }
        
        // Single value
        return new[] { new object?[] { rawData } };
    }
    
    private static async IAsyncEnumerable<object?[]> ConvertToAsyncEnumerable(
        IEnumerable<object?[]> data, 
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        foreach (var item in data)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return item;
        }
        
        await Task.CompletedTask;
    }
    
    private static async IAsyncEnumerable<object?[]> ConvertAsyncEnumerableWithCancellation<T>(
        IAsyncEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            yield return ConvertToObjectArray(item);
        }
    }
    
    private static object?[] ConvertToObjectArray<T>(T item)
    {
        if (item is object?[] array)
            return array;
            
        // Handle tuples
        if (item is System.Runtime.CompilerServices.ITuple tuple)
        {
            var result = new object?[tuple.Length];
            for (int i = 0; i < tuple.Length; i++)
            {
                result[i] = tuple[i];
            }
            return result;
        }
        
        // Single item
        return new object?[] { item };
    }
    
    #endregion
");
    }
    
    private bool IsTaskOfEnumerable(ITypeSymbol type)
    {
        return type is INamedTypeSymbol namedType &&
               namedType.Name == "Task" &&
               namedType.TypeArguments.Length == 1 &&
               IsEnumerableType(namedType.TypeArguments[0]);
    }
    
    private bool IsValueTaskOfEnumerable(ITypeSymbol type)
    {
        return type is INamedTypeSymbol namedType &&
               namedType.Name == "ValueTask" &&
               namedType.TypeArguments.Length == 1 &&
               IsEnumerableType(namedType.TypeArguments[0]);
    }
    
    private bool IsAsyncEnumerable(ITypeSymbol type)
    {
        return type is INamedTypeSymbol namedType &&
               (namedType.Name == "IAsyncEnumerable" || 
                namedType.AllInterfaces.Any(i => i.Name == "IAsyncEnumerable"));
    }
    
    private bool IsEnumerableType(ITypeSymbol type)
    {
        return type is INamedTypeSymbol namedType &&
               (namedType.Name == "IEnumerable" || 
                namedType.AllInterfaces.Any(i => i.Name == "IEnumerable"));
    }
    
    private bool IsNumericType(string typeName)
    {
        return typeName is "int" or "long" or "short" or "byte" or 
               "uint" or "ulong" or "ushort" or "sbyte" or
               "float" or "double" or "decimal";
    }
}

/// <summary>
/// Information about a data source
/// </summary>
internal class DataSourceInfo
{
    public required string ContainingTypeName { get; init; }
    public required string SafeTypeName { get; init; }
    public required string MemberName { get; init; }
    public required string SafeMemberName { get; init; }
    public required bool IsProperty { get; init; }
    public required bool IsAsync { get; init; }
    public required ITypeSymbol ReturnType { get; init; }
    public required string FactoryKey { get; init; }
    public List<ParameterInfo> Parameters { get; init; } = new();
}

/// <summary>
/// Information about a method parameter
/// </summary>
internal class ParameterInfo
{
    public required string ParameterName { get; init; }
    public required string ParameterType { get; init; }
    public required bool HasDefaultValue { get; init; }
    public object? DefaultValue { get; init; }
    public required bool IsParams { get; init; }
    public string? ElementType { get; init; }
}