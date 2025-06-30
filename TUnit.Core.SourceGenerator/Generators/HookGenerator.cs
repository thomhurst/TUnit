using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator;

namespace TUnit.Core.SourceGenerator.Generators;

// Mirror of TUnit.Core.HookLevel
internal enum HookLevel
{
    Assembly,
    Class,
    Test
}

/// <summary>
/// Responsible for generating hook metadata and invokers
/// </summary>
internal sealed class HookGenerator
{
    private readonly PropertyInjectionAnalyzer _propertyInjectionAnalyzer;

    public HookGenerator()
    {
        _propertyInjectionAnalyzer = new PropertyInjectionAnalyzer();
    }

    /// <summary>
    /// Generates hook metadata for a test method
    /// </summary>
    public void GenerateHookMetadata(CodeWriter writer, TestMethodMetadata testInfo)
    {
        var hooks = FindAllHooks(testInfo);

        writer.AppendLine("Hooks = new TestHooks");
        writer.AppendLine("{");
        writer.Indent();

        GenerateHookArray(writer, "BeforeClass", hooks.BeforeClass);
        GenerateHookArray(writer, "BeforeTest", hooks.BeforeTest);
        GenerateHookArray(writer, "AfterTest", hooks.AfterTest);
        GenerateHookArray(writer, "AfterClass", hooks.AfterClass);

        writer.Unindent();
        writer.AppendLine("},");
    }

    /// <summary>
    /// Generates hook invoker methods for all test classes
    /// </summary>
    public void GenerateHookInvokers(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        var processedClasses = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var testInfo in testMethods)
        {
            if (processedClasses.Add(testInfo.TypeSymbol))
            {
                // Generate property injection hooks if needed
                if (_propertyInjectionAnalyzer.HasInjectableProperties(testInfo.TypeSymbol))
                {
                    GeneratePropertyInjectionHook(writer, testInfo.TypeSymbol);
                }
            }
        }
    }

    /// <summary>
    /// Registers hook delegates
    /// </summary>
    public void GenerateHookRegistrations(CodeWriter writer, IEnumerable<TestMethodMetadata> testMethods)
    {
        var processedClasses = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var testInfo in testMethods)
        {
            if (processedClasses.Add(testInfo.TypeSymbol))
            {
                if (_propertyInjectionAnalyzer.HasInjectableProperties(testInfo.TypeSymbol))
                {
                    var safeClassName = testInfo.TypeSymbol.Name.Replace(".", "_");
                    writer.AppendLine($"HookDelegateStorage.RegisterHook(\"{safeClassName}_InjectProperties\", {safeClassName}_InjectProperties);");
                }
            }
        }
    }

    private TestHooks FindAllHooks(TestMethodMetadata testInfo)
    {
        var beforeClass = new List<HookInfo>();
        var beforeTest = new List<HookInfo>();
        var afterTest = new List<HookInfo>();
        var afterClass = new List<HookInfo>();

        // Find hooks in class hierarchy
        FindHooksInHierarchy(testInfo.TypeSymbol, beforeClass, beforeTest, afterTest, afterClass);

        // Add property injection hook if needed
        if (_propertyInjectionAnalyzer.HasInjectableProperties(testInfo.TypeSymbol))
        {
            var safeClassName = testInfo.TypeSymbol.Name.Replace(".", "_");
            beforeTest.Insert(0, new HookInfo
            {
                Name = $"{safeClassName}_InjectProperties",
                Level = HookLevel.Test,
                Order = -1000,
                IsAsync = true,
                ReturnsValueTask = false
            });
        }

        return new TestHooks
        {
            BeforeClass = beforeClass.OrderBy(h => h.Order).ToList(),
            BeforeTest = beforeTest.OrderBy(h => h.Order).ToList(),
            AfterTest = afterTest.OrderBy(h => h.Order).ToList(),
            AfterClass = afterClass.OrderBy(h => h.Order).ToList()
        };
    }

    private void FindHooksInHierarchy(
        ITypeSymbol type,
        List<HookInfo> beforeClass,
        List<HookInfo> beforeTest,
        List<HookInfo> afterTest,
        List<HookInfo> afterClass)
    {
        // Process base class first
        if (type.BaseType != null && type.BaseType.SpecialType != SpecialType.System_Object)
        {
            FindHooksInHierarchy(type.BaseType, beforeClass, beforeTest, afterTest, afterClass);
        }

        // Find hooks in current type
        foreach (var member in type.GetMembers().OfType<IMethodSymbol>())
        {
            var hookAttribute = member.GetAttributes()
                .FirstOrDefault(a => IsHookAttribute(a.AttributeClass?.Name));

            if (hookAttribute != null)
            {
                var hookInfo = CreateHookInfo(member, hookAttribute);
                
                switch (hookAttribute.AttributeClass?.Name)
                {
                    case "BeforeClassAttribute":
                        beforeClass.Add(hookInfo);
                        break;
                    case "BeforeAttribute":
                    case "BeforeTestAttribute":
                        beforeTest.Add(hookInfo);
                        break;
                    case "AfterAttribute":
                    case "AfterTestAttribute":
                        afterTest.Add(hookInfo);
                        break;
                    case "AfterClassAttribute":
                        afterClass.Add(hookInfo);
                        break;
                }
            }
        }
    }

    private bool IsHookAttribute(string? attributeName)
    {
        return attributeName switch
        {
            "BeforeClassAttribute" => true,
            "BeforeAttribute" => true,
            "BeforeTestAttribute" => true,
            "AfterAttribute" => true,
            "AfterTestAttribute" => true,
            "AfterClassAttribute" => true,
            _ => false
        };
    }

    private HookInfo CreateHookInfo(IMethodSymbol method, AttributeData attribute)
    {
        var order = attribute.NamedArguments
            .FirstOrDefault(a => a.Key == "Order")
            .Value.Value as int? ?? 0;

        var level = attribute.AttributeClass?.Name switch
        {
            "BeforeClassAttribute" => HookLevel.Class,
            "AfterClassAttribute" => HookLevel.Class,
            _ => HookLevel.Test
        };

        return new HookInfo
        {
            Name = method.Name,
            Level = level,
            Order = order,
            IsAsync = method.IsAsync || method.ReturnType.Name == "Task" || method.ReturnType.Name == "ValueTask",
            ReturnsValueTask = method.ReturnType.Name == "ValueTask"
        };
    }

    private void GenerateHookArray(CodeWriter writer, string hookType, List<HookInfo> hooks)
    {
        writer.Append($"{hookType} = ");
        
        if (!hooks.Any())
        {
            writer.AppendLine("Array.Empty<HookMetadata>(),");
            return;
        }

        writer.AppendLine("new HookMetadata[]");
        writer.AppendLine("{");
        writer.Indent();

        foreach (var hook in hooks)
        {
            writer.AppendLine("new HookMetadata");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine($"Name = \"{hook.Name}\",");
            writer.AppendLine($"Level = HookLevel.{hook.Level},");
            writer.AppendLine($"Order = {hook.Order},");
            writer.AppendLine($"IsAsync = {hook.IsAsync.ToString().ToLower()},");
            writer.AppendLine($"ReturnsValueTask = {hook.ReturnsValueTask.ToString().ToLower()},");
            writer.AppendLine($"Invoker = HookDelegateStorage.GetHook(\"{hook.Name}\")");
            writer.Unindent();
            writer.AppendLine("},");
        }

        writer.Unindent();
        writer.AppendLine("},");
    }

    private void GeneratePropertyInjectionHook(CodeWriter writer, ITypeSymbol typeSymbol)
    {
        var className = typeSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        var safeClassName = typeSymbol.Name.Replace(".", "_");

        writer.AppendLine($"private static async Task {safeClassName}_InjectProperties(object? instance, HookContext context)");
        writer.AppendLine("{");
        writer.Indent();
        
        writer.AppendLine($"var typedInstance = ({className})instance;");
        writer.AppendLine("var services = context.TestContext.ServiceProvider;");
        writer.AppendLine();
        writer.AppendLine("if (services == null) return;");
        writer.AppendLine();

        // Generate property injection logic
        var injectableProperties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "InjectAttribute"))
            .OrderBy(p => GetPropertyOrder(p))
            .ToList();

        foreach (var property in injectableProperties)
        {
            GeneratePropertyInjection(writer, property, "typedInstance");
        }

        writer.Unindent();
        writer.AppendLine("}");
    }

    private void GeneratePropertyInjection(CodeWriter writer, IPropertySymbol property, string instanceName)
    {
        var attribute = property.GetAttributes()
            .First(a => a.AttributeClass?.Name == "InjectAttribute");

        var isRequired = attribute.NamedArguments
            .FirstOrDefault(a => a.Key == "Required")
            .Value.Value as bool? ?? true;

        var propertyType = property.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        if (isRequired)
        {
            writer.AppendLine($"{instanceName}.{property.Name} = services.GetRequiredService<{propertyType}>();");
        }
        else
        {
            writer.AppendLine($"{instanceName}.{property.Name} = services.GetService<{propertyType}>();");
        }

        // Handle IAsyncInitializable
        writer.AppendLine($"if ({instanceName}.{property.Name} is IAsyncInitializable asyncInit_{property.Name})");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"await asyncInit_{property.Name}.InitializeAsync();");
        writer.Unindent();
        writer.AppendLine("}");
    }

    private int GetPropertyOrder(IPropertySymbol property)
    {
        var attribute = property.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "InjectAttribute");

        return attribute?.NamedArguments
            .FirstOrDefault(a => a.Key == "Order")
            .Value.Value as int? ?? 0;
    }

    private class HookInfo
    {
        public required string Name { get; init; }
        public required HookLevel Level { get; init; }
        public required int Order { get; init; }
        public required bool IsAsync { get; init; }
        public required bool ReturnsValueTask { get; init; }
    }

    private class TestHooks
    {
        public required List<HookInfo> BeforeClass { get; init; }
        public required List<HookInfo> BeforeTest { get; init; }
        public required List<HookInfo> AfterTest { get; init; }
        public required List<HookInfo> AfterClass { get; init; }
    }

    private class PropertyInjectionAnalyzer
    {
        public bool HasInjectableProperties(ITypeSymbol typeSymbol)
        {
            return typeSymbol.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(p => p.GetAttributes().Any(a => a.AttributeClass?.Name == "InjectAttribute"));
        }
    }
}