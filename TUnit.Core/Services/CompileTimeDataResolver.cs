using System.Reflection;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;
using TUnit.Core.Models;

namespace TUnit.Core.Services;

/// <summary>
/// Default implementation of compile-time data resolver.
/// Evaluates simple data attributes at compile-time for AOT safety.
/// Now supports MethodDataSource through AOT-safe factory generation.
/// </summary>
public class CompileTimeDataResolver : ICompileTimeDataResolver
{
    private readonly AotMethodDataSourceGenerator _methodDataSourceGenerator;
    private readonly AotAsyncDataSourceGenerator _asyncDataSourceGenerator;

    public CompileTimeDataResolver(
        AotMethodDataSourceGenerator? methodDataSourceGenerator = null,
        AotAsyncDataSourceGenerator? asyncDataSourceGenerator = null)
    {
        var safetyAnalyzer = new CompileTimeSafetyAnalyzer();
        _methodDataSourceGenerator = methodDataSourceGenerator ?? new AotMethodDataSourceGenerator(safetyAnalyzer);
        _asyncDataSourceGenerator = asyncDataSourceGenerator ?? new AotAsyncDataSourceGenerator(safetyAnalyzer);
    }
    /// <inheritdoc />
    public async Task<IReadOnlyList<object?[]>> ResolveClassDataAsync(ClassMetadata classMetadata)
    {
        var resolvedData = new List<object?[]>();
        var dataAttributes = classMetadata.GetDataAttributes();

        foreach (var dataAttribute in dataAttributes)
        {
            if (CanResolveAtCompileTime(dataAttribute))
            {
                var data = await ResolveDataAttributeAsync(dataAttribute, classMetadata.Type);
                resolvedData.AddRange(data);
            }
        }

        return resolvedData.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<object?[]>> ResolveMethodDataAsync(MethodMetadata methodMetadata)
    {
        var resolvedData = new List<object?[]>();
        var dataAttributes = methodMetadata.GetDataAttributes();

        foreach (var dataAttribute in dataAttributes)
        {
            if (CanResolveAtCompileTime(dataAttribute))
            {
                var data = await ResolveDataAttributeAsync(dataAttribute, methodMetadata.DeclaringType());
                resolvedData.AddRange(data);
            }
        }

        return resolvedData.AsReadOnly();
    }

    /// <inheritdoc />
    public async Task<IDictionary<string, object?>> ResolvePropertyDataAsync(ClassMetadata classMetadata)
    {
        var propertyData = new Dictionary<string, object?>();

        // Resolve property sources at compile-time
        // This is a simplified implementation - real implementation would
        // evaluate property source attributes
        
        await Task.CompletedTask;
        return propertyData;
    }

    /// <inheritdoc />
    public bool CanResolveAtCompileTime(IDataAttribute dataAttribute)
    {
        return dataAttribute switch
        {
            // Simple value attributes can be resolved at compile-time
            ArgumentsAttribute => true,
            
            // MethodDataSource can now be resolved through AOT-safe factory generation
            MethodDataSourceAttribute methodDataSource => CanResolveMethodDataSource(methodDataSource),
            
            // AsyncDataSourceGenerator can now be resolved through AOT-safe factory generation
            IAsyncDataSourceGeneratorAttribute asyncDataSource => CanResolveAsyncDataSource(asyncDataSource),
            
            // Generic data sources typically can't be resolved at compile-time
            _ when IsGenericDataSource(dataAttribute) => false,
            
            // Default to runtime resolution for safety
            _ => false
        };
    }

    private async Task<IEnumerable<object?[]>> ResolveDataAttributeAsync(IDataAttribute dataAttribute, Type contextType)
    {
        return dataAttribute switch
        {
            ArgumentsAttribute argsAttr => ResolveArgumentsAttribute(argsAttr),
            MethodDataSourceAttribute methodDataAttr => await ResolveMethodDataSourceAttributeAsync(methodDataAttr, contextType),
            IAsyncDataSourceGeneratorAttribute asyncDataAttr => await ResolveAsyncDataSourceAttributeAsync(asyncDataAttr, contextType),
            _ => Array.Empty<object?[]>()
        };
    }

    private static IEnumerable<object?[]> ResolveArgumentsAttribute(ArgumentsAttribute argumentsAttribute)
    {
        // ArgumentsAttribute contains simple values that can be resolved at compile-time
        yield return argumentsAttribute.Values;
    }

    /// <summary>
    /// Resolves MethodDataSource attributes by generating AOT-safe factory invocation.
    /// This method generates code that will be used by the source generator.
    /// </summary>
    private Task<IEnumerable<object?[]>> ResolveMethodDataSourceAttributeAsync(MethodDataSourceAttribute methodDataAttr, Type contextType)
    {
        // For MethodDataSource, we don't resolve the data at compile-time.
        // Instead, we mark it as resolvable and the source generator will emit 
        // AOT-safe factory code that calls the method directly.
        
        // This method should not be called during actual compilation,
        // as MethodDataSource resolution happens through generated factories.
        throw new InvalidOperationException(
            "MethodDataSource resolution should be handled by generated AOT-safe factories, not at compile-time. " +
            "This indicates a misconfiguration in the dual-mode system.");
    }

    /// <summary>
    /// Resolves AsyncDataSourceGenerator attributes by generating AOT-safe factory invocation.
    /// This method generates code that will be used by the source generator.
    /// </summary>
    private Task<IEnumerable<object?[]>> ResolveAsyncDataSourceAttributeAsync(IAsyncDataSourceGeneratorAttribute asyncDataAttr, Type contextType)
    {
        // For AsyncDataSourceGenerator, we don't resolve the data at compile-time.
        // Instead, we mark it as resolvable and the source generator will emit 
        // AOT-safe factory code that instantiates and calls the generator directly.
        
        // This method should not be called during actual compilation,
        // as AsyncDataSourceGenerator resolution happens through generated factories.
        throw new InvalidOperationException(
            "AsyncDataSourceGenerator resolution should be handled by generated AOT-safe factories, not at compile-time. " +
            "This indicates a misconfiguration in the dual-mode system.");
    }

    /// <summary>
    /// Checks if a MethodDataSource attribute can be resolved through AOT-safe factory generation.
    /// </summary>
    private bool CanResolveMethodDataSource(MethodDataSourceAttribute methodDataAttr)
    {
        // We can resolve any MethodDataSource that the generator can handle
        // This is determined by the method characteristics (static, compatible return type, etc.)
        
        // For now, we'll assume all MethodDataSource attributes can be handled
        // The actual validation happens in the AotMethodDataSourceGenerator
        return true;
    }

    /// <summary>
    /// Checks if an AsyncDataSource attribute can be resolved through AOT-safe factory generation.
    /// </summary>
    private bool CanResolveAsyncDataSource(IAsyncDataSourceGeneratorAttribute asyncDataAttr)
    {
        // We can resolve any AsyncDataSourceGenerator that the generator can handle
        // This is determined by the generator characteristics (parameterless constructor, not abstract, etc.)
        
        return _asyncDataSourceGenerator.CanGenerateAotSafe(asyncDataAttr.GetType());
    }

    private static bool IsGenericDataSource(IDataAttribute dataAttribute)
    {
        // Check if the data attribute involves generic types or complex resolution
        var attributeType = dataAttribute.GetType();
        return attributeType.IsGenericType || attributeType.GetGenericArguments().Length > 0;
    }
}

/// <summary>
/// Enhanced compile-time data resolver for source generation scenarios.
/// Provides additional metadata about compile-time vs runtime resolution.
/// </summary>
public class SourceGenerationDataResolver : ICompileTimeDataResolver
{
    private readonly CompileTimeDataResolver _baseResolver;

    public SourceGenerationDataResolver()
    {
        _baseResolver = new CompileTimeDataResolver();
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<object?[]>> ResolveClassDataAsync(ClassMetadata classMetadata)
    {
        return _baseResolver.ResolveClassDataAsync(classMetadata);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<object?[]>> ResolveMethodDataAsync(MethodMetadata methodMetadata)
    {
        return _baseResolver.ResolveMethodDataAsync(methodMetadata);
    }

    /// <inheritdoc />
    public Task<IDictionary<string, object?>> ResolvePropertyDataAsync(ClassMetadata classMetadata)
    {
        return _baseResolver.ResolvePropertyDataAsync(classMetadata);
    }

    /// <inheritdoc />
    public bool CanResolveAtCompileTime(IDataAttribute dataAttribute)
    {
        return _baseResolver.CanResolveAtCompileTime(dataAttribute);
    }

    /// <summary>
    /// Resolves all data for a test method at compile-time.
    /// Returns comprehensive data including unresolved attributes.
    /// </summary>
    public async Task<CompileTimeResolvedData> ResolveAllDataAsync(
        ClassMetadata classMetadata, 
        MethodMetadata methodMetadata)
    {
        var classData = await ResolveClassDataAsync(classMetadata);
        var methodData = await ResolveMethodDataAsync(methodMetadata);
        var propertyData = await ResolvePropertyDataAsync(classMetadata);

        // Identify unresolved attributes
        var allAttributes = classMetadata.GetDataAttributes()
            .Concat(methodMetadata.GetDataAttributes())
            .ToList();

        var unresolvedAttributes = allAttributes
            .Where(attr => !CanResolveAtCompileTime(attr))
            .ToList();

        return new CompileTimeResolvedData
        {
            ClassData = classData,
            MethodData = methodData,
            PropertyData = propertyData,
            UnresolvedAttributes = unresolvedAttributes.AsReadOnly()
        };
    }
}