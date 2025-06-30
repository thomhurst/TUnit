using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

/// <summary>
/// Enhances the hook system to support ValueTask and property injection
/// </summary>
internal class HookSystemEnhancer
{
    private readonly StringBuilder _stringBuilder = new();
    
    public HookMetadataResult GenerateHooks(INamedTypeSymbol classSymbol, bool hasPropertyInjection)
    {
        var result = new HookMetadataResult();
        
        // Find all hook methods
        var beforeClassHooks = FindHooksInHierarchy(classSymbol, "BeforeAttribute", isStatic: true);
        var afterClassHooks = FindHooksInHierarchy(classSymbol, "AfterAttribute", isStatic: true);
        var beforeTestHooks = FindHooksInHierarchy(classSymbol, "BeforeAttribute", isStatic: false);
        var afterTestHooks = FindHooksInHierarchy(classSymbol, "AfterAttribute", isStatic: false);
        
        // Add property injection hooks if needed
        if (hasPropertyInjection)
        {
            var safeClassName = classSymbol.Name.Replace(".", "_");
            
            // Add property injection as a before test hook
            result.BeforeTestHooks.Add(new HookInfo
            {
                Name = $"{safeClassName}_InjectProperties",
                IsAsync = true,
                ReturnsValueTask = false,
                IsStatic = false,
                Order = -1000 // Run before user hooks
            });
            
            // Add property disposal as an after test hook
            result.AfterTestHooks.Add(new HookInfo
            {
                Name = $"{safeClassName}_DisposeProperties",
                IsAsync = true,
                ReturnsValueTask = false,
                IsStatic = false,
                Order = 1000 // Run after user hooks
            });
        }
        
        // Process user hooks
        ProcessHooks(beforeClassHooks, result.BeforeClassHooks);
        ProcessHooks(afterClassHooks, result.AfterClassHooks);
        ProcessHooks(beforeTestHooks, result.BeforeTestHooks);
        ProcessHooks(afterTestHooks, result.AfterTestHooks);
        
        // Generate hook metadata code
        GenerateHookMetadataCode(result);
        
        // Generate hook invoker delegates
        GenerateHookInvokers(classSymbol, result);
        
        return result;
    }
    
    private void ProcessHooks(List<IMethodSymbol> hooks, List<HookInfo> targetList)
    {
        foreach (var hook in hooks)
        {
            var hookInfo = new HookInfo
            {
                Name = hook.Name,
                MethodSymbol = hook,
                IsAsync = IsAsyncMethod(hook),
                ReturnsValueTask = ReturnsValueTask(hook),
                IsStatic = hook.IsStatic,
                Order = GetHookOrder(hook)
            };
            
            targetList.Add(hookInfo);
        }
        
        // Sort by order
        targetList.Sort((a, b) => a.Order.CompareTo(b.Order));
    }
    
    private bool IsAsyncMethod(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask";
    }
    
    private bool ReturnsValueTask(IMethodSymbol method)
    {
        return method.ReturnType.Name == "ValueTask";
    }
    
    private int GetHookOrder(IMethodSymbol method)
    {
        var orderAttribute = method.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "OrderAttribute");
            
        if (orderAttribute?.ConstructorArguments.Length > 0)
        {
            if (orderAttribute.ConstructorArguments[0].Value is int order)
                return order;
        }
        
        return 0;
    }
    
    private void GenerateHookMetadataCode(HookMetadataResult result)
    {
        _stringBuilder.Clear();
        
        _stringBuilder.AppendLine("Hooks = new TestHooks");
        _stringBuilder.AppendLine("{");
        
        GenerateHookArray("BeforeClass", result.BeforeClassHooks);
        GenerateHookArray("AfterClass", result.AfterClassHooks);
        GenerateHookArray("BeforeTest", result.BeforeTestHooks);
        GenerateHookArray("AfterTest", result.AfterTestHooks);
        
        _stringBuilder.AppendLine("},");
        
        result.HookMetadataCode = _stringBuilder.ToString();
    }
    
    private void GenerateHookArray(string name, List<HookInfo> hooks)
    {
        if (!hooks.Any())
        {
            _stringBuilder.AppendLine($"    {name} = Array.Empty<HookMetadata>(),");
            return;
        }
        
        _stringBuilder.AppendLine($"    {name} = new HookMetadata[]");
        _stringBuilder.AppendLine("    {");
        
        foreach (var hook in hooks)
        {
            _stringBuilder.AppendLine("        new HookMetadata");
            _stringBuilder.AppendLine("        {");
            _stringBuilder.AppendLine($"            Name = \"{hook.Name}\",");
            _stringBuilder.AppendLine($"            Order = {hook.Order},");
            _stringBuilder.AppendLine($"            IsAsync = {hook.IsAsync.ToString().ToLower()},");
            _stringBuilder.AppendLine($"            ReturnsValueTask = {hook.ReturnsValueTask.ToString().ToLower()},");
            
            if (hook.MethodSymbol != null)
            {
                var className = hook.MethodSymbol.ContainingType.ToDisplayString();
                _stringBuilder.AppendLine($"            HookInvoker = HookDelegateStorage.GetHook(\"{className}.{hook.Name}\")");
            }
            else
            {
                // For generated hooks like property injection
                _stringBuilder.AppendLine($"            HookInvoker = HookDelegateStorage.GetHook(\"{hook.Name}\")");
            }
            
            _stringBuilder.AppendLine("        },");
        }
        
        _stringBuilder.AppendLine("    },");
    }
    
    private void GenerateHookInvokers(INamedTypeSymbol classSymbol, HookMetadataResult result)
    {
        _stringBuilder.Clear();
        
        // Generate invokers for all hooks
        var allHooks = new List<(string category, List<HookInfo> hooks)>
        {
            ("BeforeClass", result.BeforeClassHooks),
            ("AfterClass", result.AfterClassHooks),
            ("BeforeTest", result.BeforeTestHooks),
            ("AfterTest", result.AfterTestHooks)
        };
        
        foreach (var (category, hooks) in allHooks)
        {
            foreach (var hook in hooks.Where(h => h.MethodSymbol != null))
            {
                GenerateHookInvoker(classSymbol, hook);
            }
        }
        
        result.HookInvokerCode = _stringBuilder.ToString();
    }
    
    private void GenerateHookInvoker(INamedTypeSymbol classSymbol, HookInfo hook)
    {
        var className = classSymbol.ToDisplayString();
        var safeClassName = className.Replace(".", "_");
        var invokerName = $"{safeClassName}_{hook.Name}_HookInvoker";
        
        _stringBuilder.AppendLine();
        
        if (hook.ReturnsValueTask)
        {
            _stringBuilder.AppendLine($"private static async Task {invokerName}(object instance, TestContext context)");
            _stringBuilder.AppendLine("{");
            
            if (hook.IsStatic)
            {
                _stringBuilder.AppendLine($"    await {className}.{hook.Name}(context).AsTask();");
            }
            else
            {
                _stringBuilder.AppendLine($"    var typedInstance = ({className})instance;");
                _stringBuilder.AppendLine($"    await typedInstance.{hook.Name}(context).AsTask();");
            }
            
            _stringBuilder.AppendLine("}");
        }
        else if (hook.IsAsync)
        {
            _stringBuilder.AppendLine($"private static async Task {invokerName}(object instance, TestContext context)");
            _stringBuilder.AppendLine("{");
            
            if (hook.IsStatic)
            {
                _stringBuilder.AppendLine($"    await {className}.{hook.Name}(context);");
            }
            else
            {
                _stringBuilder.AppendLine($"    var typedInstance = ({className})instance;");
                _stringBuilder.AppendLine($"    await typedInstance.{hook.Name}(context);");
            }
            
            _stringBuilder.AppendLine("}");
        }
        else
        {
            _stringBuilder.AppendLine($"private static Task {invokerName}(object instance, TestContext context)");
            _stringBuilder.AppendLine("{");
            
            if (hook.IsStatic)
            {
                _stringBuilder.AppendLine($"    {className}.{hook.Name}(context);");
            }
            else
            {
                _stringBuilder.AppendLine($"    var typedInstance = ({className})instance;");
                _stringBuilder.AppendLine($"    typedInstance.{hook.Name}(context);");
            }
            
            _stringBuilder.AppendLine("    return Task.CompletedTask;");
            _stringBuilder.AppendLine("}");
        }
        
        // Register the hook
        _stringBuilder.AppendLine($"HookDelegateStorage.RegisterHook(\"{className}.{hook.Name}\", {invokerName});");
    }
    
    private List<IMethodSymbol> FindHooksInHierarchy(INamedTypeSymbol type, string attributeName, bool isStatic)
    {
        var hooks = new List<IMethodSymbol>();
        var currentType = type;
        
        while (currentType != null)
        {
            var typeHooks = currentType.GetMembers()
                .OfType<IMethodSymbol>()
                .Where(m => m.IsStatic == isStatic &&
                           m.GetAttributes().Any(a => a.AttributeClass?.Name == attributeName))
                .ToList();
                
            hooks.AddRange(typeHooks);
            currentType = currentType.BaseType;
        }
        
        return hooks;
    }
}

/// <summary>
/// Result of hook generation
/// </summary>
internal class HookMetadataResult
{
    public List<HookInfo> BeforeClassHooks { get; } = new();
    public List<HookInfo> AfterClassHooks { get; } = new();
    public List<HookInfo> BeforeTestHooks { get; } = new();
    public List<HookInfo> AfterTestHooks { get; } = new();
    
    public string HookMetadataCode { get; set; } = "";
    public string HookInvokerCode { get; set; } = "";
}

/// <summary>
/// Information about a hook
/// </summary>
internal class HookInfo
{
    public required string Name { get; init; }
    public IMethodSymbol? MethodSymbol { get; init; }
    public required bool IsAsync { get; init; }
    public required bool ReturnsValueTask { get; init; }
    public required bool IsStatic { get; init; }
    public required int Order { get; init; }
}