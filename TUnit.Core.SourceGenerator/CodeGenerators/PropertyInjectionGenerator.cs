using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Generates property injection code with lifecycle management
/// </summary>
internal class PropertyInjectionGenerator
{
    private readonly StringBuilder _stringBuilder = new();
    private readonly Dictionary<ITypeSymbol, PropertyInjectionInfo> _propertyInjectionCache = new(SymbolEqualityComparer.Default);
    
    public string GeneratePropertyInjection(PropertyInjectionContext context)
    {
        _stringBuilder.Clear();
        
        var classSymbol = context.ClassSymbol;
        if (classSymbol == null)
            return string.Empty;
            
        var injectableProperties = GetInjectableProperties(classSymbol);
        if (!injectableProperties.Any())
            return string.Empty;
            
        // Build dependency graph
        var dependencyGraph = BuildDependencyGraph(injectableProperties);
        
        // Check for circular dependencies
        var circularDependency = DetectCircularDependencies(dependencyGraph);
        if (circularDependency != null)
        {
            context.DiagnosticContext?.ReportError(
                "TUNIT_PROP_001",
                "Circular Property Dependency",
                $"Circular dependency detected in property injection: {string.Join(" -> ", circularDependency)}",
                classSymbol.Locations.FirstOrDefault());
                
            return string.Empty;
        }
        
        // Topological sort for initialization order
        var sortedProperties = TopologicalSort(injectableProperties, dependencyGraph);
        
        GenerateInjectionMethod(context, sortedProperties);
        GenerateDisposalMethod(context, sortedProperties);
        
        return _stringBuilder.ToString();
    }
    
    private List<PropertyInjectionInfo> GetInjectableProperties(INamedTypeSymbol classSymbol)
    {
        var properties = new List<PropertyInjectionInfo>();
        
        foreach (var member in classSymbol.GetMembers())
        {
            if (member is not IPropertySymbol property)
                continue;
                
            // Check for [Inject] attribute
            var injectAttribute = property.GetAttributes()
                .FirstOrDefault(attr => attr.AttributeClass?.Name == "InjectAttribute");
                            
            if (injectAttribute == null)
                continue;
                
            // Extract attribute data
            var isRequired = GetAttributeValue<bool>(injectAttribute, "Required", true);
            var order = GetAttributeValue<int>(injectAttribute, "Order", 0);
            var serviceKey = GetAttributeValue<object?>(injectAttribute, "ServiceKey", null);
            
            var info = new PropertyInjectionInfo
            {
                Property = property,
                PropertyName = property.Name,
                PropertyType = property.Type,
                IsRequired = isRequired,
                Order = order,
                ServiceKey = serviceKey,
                IsAsyncInitializable = ImplementsInterface(property.Type, "IAsyncInitializable"),
                IsAsyncDisposable = ImplementsInterface(property.Type, "IAsyncDisposable") || 
                                   ImplementsInterface(property.Type, "IDisposable")
            };
            
            properties.Add(info);
        }
        
        return properties;
    }
    
    private T GetAttributeValue<T>(AttributeData attribute, string propertyName, T defaultValue)
    {
        var namedArgument = attribute.NamedArguments
            .FirstOrDefault(na => na.Key == propertyName);
            
        if (namedArgument.Key != null)
        {
            return (T)namedArgument.Value.Value!;
        }
        
        return defaultValue;
    }
    
    private Dictionary<PropertyInjectionInfo, List<PropertyInjectionInfo>> BuildDependencyGraph(
        List<PropertyInjectionInfo> properties)
    {
        var graph = new Dictionary<PropertyInjectionInfo, List<PropertyInjectionInfo>>();
        
        foreach (var property in properties)
        {
            graph[property] = new List<PropertyInjectionInfo>();
            
            // Analyze property type to find dependencies
            var dependencies = FindPropertyDependencies(property, properties);
            graph[property].AddRange(dependencies);
        }
        
        return graph;
    }
    
    private List<PropertyInjectionInfo> FindPropertyDependencies(
        PropertyInjectionInfo property, 
        List<PropertyInjectionInfo> allProperties)
    {
        var dependencies = new List<PropertyInjectionInfo>();
        
        // Check if property type has constructor parameters that match other properties
        var constructors = property.PropertyType.GetMembers()
            .OfType<IMethodSymbol>()
            .Where(m => m.MethodKind == MethodKind.Constructor && m.DeclaredAccessibility == Accessibility.Public);
            
        foreach (var ctor in constructors)
        {
            foreach (var param in ctor.Parameters)
            {
                var matchingProperty = allProperties.FirstOrDefault(p => 
                    SymbolEqualityComparer.Default.Equals(p.PropertyType, param.Type));
                    
                if (matchingProperty != null && matchingProperty != property)
                {
                    dependencies.Add(matchingProperty);
                }
            }
        }
        
        return dependencies;
    }
    
    private List<string>? DetectCircularDependencies(
        Dictionary<PropertyInjectionInfo, List<PropertyInjectionInfo>> graph)
    {
        var visited = new HashSet<PropertyInjectionInfo>();
        var recursionStack = new HashSet<PropertyInjectionInfo>();
        var path = new List<PropertyInjectionInfo>();
        
        foreach (var node in graph.Keys)
        {
            if (DetectCycle(node, graph, visited, recursionStack, path))
            {
                // Return the circular path
                var circularPath = new List<string>();
                var inCycle = false;
                
                for (int i = 0; i < path.Count; i++)
                {
                    if (path[i] == path[path.Count - 1] && i < path.Count - 1)
                        inCycle = true;
                        
                    if (inCycle)
                        circularPath.Add(path[i].PropertyName);
                }
                
                circularPath.Add(path[path.Count - 1].PropertyName);
                return circularPath;
            }
        }
        
        return null;
    }
    
    private bool DetectCycle(
        PropertyInjectionInfo node,
        Dictionary<PropertyInjectionInfo, List<PropertyInjectionInfo>> graph,
        HashSet<PropertyInjectionInfo> visited,
        HashSet<PropertyInjectionInfo> recursionStack,
        List<PropertyInjectionInfo> path)
    {
        visited.Add(node);
        recursionStack.Add(node);
        path.Add(node);
        
        foreach (var neighbor in graph[node])
        {
            if (!visited.Contains(neighbor))
            {
                if (DetectCycle(neighbor, graph, visited, recursionStack, path))
                    return true;
            }
            else if (recursionStack.Contains(neighbor))
            {
                path.Add(neighbor);
                return true;
            }
        }
        
        recursionStack.Remove(node);
        if (path.Count > 0 && path[path.Count - 1] == node)
            path.RemoveAt(path.Count - 1);
            
        return false;
    }
    
    private List<PropertyInjectionInfo> TopologicalSort(
        List<PropertyInjectionInfo> properties,
        Dictionary<PropertyInjectionInfo, List<PropertyInjectionInfo>> graph)
    {
        var sorted = new List<PropertyInjectionInfo>();
        var visited = new HashSet<PropertyInjectionInfo>();
        
        void Visit(PropertyInjectionInfo node)
        {
            if (visited.Contains(node))
                return;
                
            visited.Add(node);
            
            foreach (var dependency in graph[node])
            {
                Visit(dependency);
            }
            
            sorted.Add(node);
        }
        
        // Sort by Order first, then visit
        var orderedProperties = properties.OrderBy(p => p.Order).ToList();
        
        foreach (var property in orderedProperties)
        {
            Visit(property);
        }
        
        // Final sort respects both topological order and Order attribute
        return sorted.OrderBy(p => p.Order).ToList();
    }
    
    private void GenerateInjectionMethod(PropertyInjectionContext context, List<PropertyInjectionInfo> sortedProperties)
    {
        var className = context.ClassName;
        var safeClassName = context.SafeClassName;
        
        _stringBuilder.AppendLine($@"
    private static async Task {safeClassName}_InjectProperties(object? instance, HookContext context)
    {{
        var typedInstance = ({className})instance;
        var services = context.TestContext.ServiceProvider;");
        
        foreach (var property in sortedProperties)
        {
            var propertyName = property.PropertyName;
            var propertyType = property.PropertyType.ToDisplayString();
            
            // For nullable reference types, use the underlying type for typeof()
            var typeForServiceLookup = property.PropertyType.IsReferenceType && 
                                      property.PropertyType.NullableAnnotation == NullableAnnotation.Annotated
                ? property.PropertyType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()
                : propertyType;
            
            _stringBuilder.AppendLine($@"
        // Inject {propertyName}
        if (services != null)
        {{
            var {propertyName.ToLowerInvariant()} = services.GetService(typeof({typeForServiceLookup})) as {typeForServiceLookup};
            if ({propertyName.ToLowerInvariant()} != null)
            {{");
            
            if (property.IsAsyncInitializable)
            {
                _stringBuilder.AppendLine($@"
                if ({propertyName.ToLowerInvariant()} is IAsyncInitializable asyncInit)
                {{
                    await asyncInit.InitializeAsync();
                }}");
            }
            
            _stringBuilder.AppendLine($@"
                typedInstance.{propertyName} = {propertyName.ToLowerInvariant()};
            }}");
            
            if (property.IsRequired)
            {
                _stringBuilder.AppendLine($@"
            else
            {{
                throw new InvalidOperationException($""Required property '{propertyName}' of type '{propertyType}' could not be resolved from service provider."");
            }}");
            }
            
            _stringBuilder.AppendLine("        }");
        }
        
        _stringBuilder.AppendLine("    }");
    }
    
    private void GenerateDisposalMethod(PropertyInjectionContext context, List<PropertyInjectionInfo> sortedProperties)
    {
        var className = context.ClassName;
        var safeClassName = context.SafeClassName;
        
        // Dispose in reverse order
        var disposableProperties = sortedProperties
            .Where(p => p.IsAsyncDisposable)
            .Reverse()
            .ToList();
            
        if (!disposableProperties.Any())
            return;
            
        _stringBuilder.AppendLine($@"
    private static async Task {safeClassName}_DisposeProperties(object? instance, HookContext context)
    {{
        var typedInstance = ({className})instance;");
        
        foreach (var property in disposableProperties)
        {
            var propertyName = property.PropertyName;
            
            _stringBuilder.AppendLine($@"
        // Dispose {propertyName}
        if (typedInstance.{propertyName} != null)
        {{
            if (typedInstance.{propertyName} is IAsyncDisposable asyncDisposable)
            {{
                await asyncDisposable.DisposeAsync();
            }}
            else if (typedInstance.{propertyName} is IDisposable disposable)
            {{
                disposable.Dispose();
            }}
        }}");
        }
        
        _stringBuilder.AppendLine("    }");
    }
    
    private bool ImplementsInterface(ITypeSymbol type, string interfaceName)
    {
        return type.AllInterfaces.Any(i => i.Name == interfaceName);
    }
    
    private class PropertyInjectionInfo
    {
        public required IPropertySymbol Property { get; init; }
        public required string PropertyName { get; init; }
        public required ITypeSymbol PropertyType { get; init; }
        public required bool IsRequired { get; init; }
        public required int Order { get; init; }
        public required object? ServiceKey { get; init; }
        public required bool IsAsyncInitializable { get; init; }
        public required bool IsAsyncDisposable { get; init; }
    }
}