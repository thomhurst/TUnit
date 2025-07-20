using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Source generator that emits unified hook metadata for AOT support
/// </summary>
[Generator]
public class UnifiedHookMetadataGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Find all test classes (classes that contain test methods)
        var testClasses = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.TestAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) =>
                {
                    if (ctx.TargetSymbol is IMethodSymbol methodSymbol)
                    {
                        return methodSymbol.ContainingType;
                    }
                    return null;
                })
            .Where(static t => t is not null)
            .Collect()
            .Select(static (types, _) => types.Distinct(SymbolEqualityComparer.Default).Cast<INamedTypeSymbol>().ToImmutableArray());

        // Find all methods with Before/After attributes
        var beforeHooks = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.BeforeAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetHookMethodMetadata(ctx, "Before"))
            .Where(static m => m is not null);

        var afterHooks = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.AfterAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetHookMethodMetadata(ctx, "After"))
            .Where(static m => m is not null);

        var beforeEveryHooks = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.BeforeEveryAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetHookMethodMetadata(ctx, "BeforeEvery"))
            .Where(static m => m is not null);

        var afterEveryHooks = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.AfterEveryAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => GetHookMethodMetadata(ctx, "AfterEvery"))
            .Where(static m => m is not null);

        // Collect all hooks separately and combine
        var beforeHooksCollected = beforeHooks.Collect();
        var afterHooksCollected = afterHooks.Collect();
        var beforeEveryHooksCollected = beforeEveryHooks.Collect();
        var afterEveryHooksCollected = afterEveryHooks.Collect();

        // Combine all collected hooks with test classes
        var allData = beforeHooksCollected
            .Combine(afterHooksCollected)
            .Combine(beforeEveryHooksCollected)
            .Combine(afterEveryHooksCollected)
            .Combine(testClasses);

        // Generate the hook registry
        context.RegisterSourceOutput(allData, (context, data) =>
        {
            var ((((beforeHooksList, afterHooksList), beforeEveryHooksList), afterEveryHooksList), testClassesList) = data;
            var directHooks = beforeHooksList
                .Concat(afterHooksList)
                .Concat(beforeEveryHooksList)
                .Concat(afterEveryHooksList)
                .Where(h => h != null)
                .Cast<HookMethodMetadata>()
                .ToList();

            // Discover hooks from base classes
            var allHooks = DiscoverAllHooks(directHooks, testClassesList);
            GenerateHookRegistry(context, allHooks.ToImmutableArray());
        });
    }

    private static List<HookMethodMetadata> DiscoverAllHooks(List<HookMethodMetadata> directHooks, ImmutableArray<INamedTypeSymbol> testClasses)
    {
        // Filter out hooks from abstract generic classes
        var validDirectHooks = directHooks.Where(h =>
            !(h.TypeSymbol is { IsAbstract: true, IsGenericType: true } &&
                h.TypeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter))).ToList();

        // Just return the direct hooks - no need to walk inheritance during registration
        // Inheritance will be handled at execution time in GetHooksForType
        return validDirectHooks
            .GroupBy(h => h, new HookEqualityComparer())
            .Select(g => g.First())
            .ToList();
    }

    private static AttributeData? GetHookAttribute(IMethodSymbol method)
    {
        return method.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name == "BeforeAttribute" ||
            a.AttributeClass?.Name == "AfterAttribute" ||
            a.AttributeClass?.Name == "BeforeEveryAttribute" ||
            a.AttributeClass?.Name == "AfterEveryAttribute");
    }

    private static string GetHookKindFromAttribute(AttributeData attribute)
    {
        return attribute.AttributeClass?.Name switch
        {
            "BeforeAttribute" => "Before",
            "AfterAttribute" => "After",
            "BeforeEveryAttribute" => "BeforeEvery",
            "AfterEveryAttribute" => "AfterEvery",
            _ => "Before"
        };
    }

    private class HookEqualityComparer : IEqualityComparer<HookMethodMetadata>
    {
        public bool Equals(HookMethodMetadata? x, HookMethodMetadata? y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }

            // Hooks are equal if they have the same method and are used by the same test class
            return SymbolEqualityComparer.Default.Equals(x.TypeSymbol, y.TypeSymbol) &&
                   SymbolEqualityComparer.Default.Equals(x.MethodSymbol, y.MethodSymbol);
        }

        public int GetHashCode(HookMethodMetadata obj)
        {
            if (obj == null)
            {
                return 0;
            }

            unchecked
            {
                var hash = 17;
                hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(obj.TypeSymbol);
                hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(obj.MethodSymbol);
                return hash;
            }
        }
    }

    private static HookMethodMetadata? GetHookMethodMetadata(GeneratorAttributeSyntaxContext context, string hookKind)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var typeSymbol = methodSymbol.ContainingType;
        if (typeSymbol == null || typeSymbol is not { })
        {
            return null;
        }

        // For generic type definitions (like BaseClass<T>), we'll generate metadata
        // using the open generic type (BaseClass<>) to avoid compilation errors

        // Skip non-public methods
        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        // Get hook type from attribute first
        var hookAttribute = context.Attributes[0];
        var hookType = GetHookType(hookAttribute);

        // Validate method signature with hook type
        if (!IsValidHookMethod(methodSymbol, hookType))
        {
            return null;
        }

        // Get location info
        var location = context.TargetNode.GetLocation();
        var filePath = location.SourceTree?.FilePath ?? "";
        var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;

        // Get order from attribute
        var order = GetHookOrder(hookAttribute);

        return new HookMethodMetadata
        {
            MethodSymbol = methodSymbol,
            TypeSymbol = typeSymbol,
            FilePath = filePath,
            LineNumber = lineNumber,
            HookAttribute = hookAttribute,
            HookKind = hookKind,
            HookType = hookType,
            Order = order,
            Context = context
        };
    }

    private static bool IsValidHookMethod(IMethodSymbol method, string hookType)
    {
        // Check return type - must be void, Task, or ValueTask
        var returnType = method.ReturnType;
        if (returnType.SpecialType != SpecialType.System_Void &&
            returnType.Name != "Task" &&
            returnType.Name != "ValueTask")
        {
            return false;
        }

        // Check parameters - can be:
        // 1. No parameters
        // 2. Single context parameter
        // 3. Single CancellationToken parameter
        // 4. Context parameter followed by CancellationToken parameter
        if (method.Parameters.Length > 2)
        {
            return false;
        }

        if (method.Parameters.Length >= 1)
        {
            var firstParam = method.Parameters[0];
            var firstParamType = firstParam.Type;
            var firstParamTypeName = firstParamType.Name;
            var firstParamNamespace = firstParamType.ContainingNamespace?.ToString();

            // Check if first parameter is CancellationToken
            if (firstParamTypeName == "CancellationToken" && firstParamNamespace == "System.Threading")
            {
                // If only parameter, it's valid
                return method.Parameters.Length == 1;
            }

            // Otherwise, first parameter should be the appropriate context type
            var expectedContextType = hookType switch
            {
                "Test" => "TestContext",
                "Class" => "ClassHookContext",
                "Assembly" => "AssemblyHookContext",
                "TestSession" => "TestSessionContext",
                "TestDiscovery" => "TestDiscoveryContext",
                _ => null
            };

            // For BeforeTestDiscovery, we need a special context type
            if (hookType == "TestDiscovery" && method.Name.Contains("Before"))
            {
                expectedContextType = "BeforeTestDiscoveryContext";
            }

            if (expectedContextType == null || firstParamNamespace != "TUnit.Core" || firstParamTypeName != expectedContextType)
            {
                return false;
            }

            // If there's a second parameter, it must be CancellationToken
            if (method.Parameters.Length == 2)
            {
                var secondParam = method.Parameters[1];
                var secondParamType = secondParam.Type;
                return secondParamType.Name == "CancellationToken" &&
                       secondParamType.ContainingNamespace?.ToString() == "System.Threading";
            }
        }

        return true;
    }

    private static string GetHookType(AttributeData attribute)
    {
        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is int hookTypeValue)
        {
            return hookTypeValue switch
            {
                0 => "Test",
                1 => "Class",
                2 => "Assembly",
                3 => "TestSession",
                4 => "TestDiscovery",
                _ => "Test"
            };
        }
        return "Test";
    }

    private static int GetHookOrder(AttributeData attribute)
    {
        var orderArg = attribute.NamedArguments.FirstOrDefault(a => a.Key == "Order");
        if (orderArg.Value.Value is int order)
        {
            return order;
        }
        return 0;
    }

    private static void GenerateHookRegistry(SourceProductionContext context, ImmutableArray<HookMethodMetadata> hooks)
    {
        try
        {
            var validHooks = hooks
                .Where(h => h != null)
                .ToList();

            if (!validHooks.Any())
            {
                return;
            }

            using var writer = new CodeWriter();

            // Write file header
            writer.AppendLine("#nullable enable");
            writer.AppendLine("#pragma warning disable CS9113 // Parameter is unread.");
            writer.AppendLine();
            writer.AppendLine("using System;");
            writer.AppendLine("using System.Collections.Generic;");
            writer.AppendLine("using System.Linq;");
            writer.AppendLine("using System.Reflection;");
            writer.AppendLine("using System.Runtime.CompilerServices;");
            writer.AppendLine("using System.Threading;");
            writer.AppendLine("using System.Threading.Tasks;");
            writer.AppendLine("using global::TUnit.Core;");
            writer.AppendLine("using global::TUnit.Core.Hooks;");
            writer.AppendLine("using global::TUnit.Core.Interfaces.SourceGenerator;");
            writer.AppendLine("using global::TUnit.Core.Models;");
            writer.AppendLine("using HookType = global::TUnit.Core.HookType;");
            writer.AppendLine();

            writer.AppendLine("namespace TUnit.Generated.Hooks;");
            writer.AppendLine();

            // Generate the hook source implementation
            var interfaces = new global::System.Collections.Generic.List<string>();
            if (validHooks.Any(h => h.HookType == "Test"))
            {
                interfaces.Add("ITestHookSource");
            }
            if (validHooks.Any(h => h.HookType == "Class"))
            {
                interfaces.Add("IClassHookSource");
            }
            if (validHooks.Any(h => h.HookType == "Assembly"))
            {
                interfaces.Add("IAssemblyHookSource");
            }
            if (validHooks.Any(h => h.HookType == "TestSession"))
            {
                interfaces.Add("ITestSessionHookSource");
            }
            if (validHooks.Any(h => h.HookType == "TestDiscovery"))
            {
                interfaces.Add("ITestDiscoveryHookSource");
            }

            var interfaceList = interfaces.Any() ? " : " + string.Join(", ", interfaces) : "";
            using (writer.BeginBlock($"public sealed class GeneratedHookSource{interfaceList}"))
            {
                // Generate storage fields
                GenerateStorageFields(writer, validHooks);

                // Generate static constructor
                GenerateStaticConstructor(writer, validHooks);

                // Generate interface implementations
                GenerateInterfaceImplementations(writer, validHooks);

                // Generate helper methods
                GenerateInitializeHookDictionaries(writer, validHooks);
                GenerateHookLookupMethods(writer);

                // Generate hook delegates
                GenerateHookDelegates(writer, validHooks);
            }

            writer.AppendLine();

            // Generate module initializer
            using (writer.BeginBlock("internal static class HookModuleInitializer"))
            {
                writer.AppendLine("[ModuleInitializer]");
                using (writer.BeginBlock("public static void Initialize()"))
                {
                    writer.AppendLine("var source = new GeneratedHookSource();");

                    // Register for all implemented interfaces based on hooks found
                    if (validHooks.Any(h => h.HookType == "Test"))
                    {
                        writer.AppendLine("global::TUnit.Core.SourceRegistrar.RegisterTestHookSource(source);");
                    }

                    if (validHooks.Any(h => h.HookType == "Class"))
                    {
                        writer.AppendLine("global::TUnit.Core.SourceRegistrar.RegisterClassHookSource(source);");
                    }

                    if (validHooks.Any(h => h.HookType == "Assembly"))
                    {
                        writer.AppendLine("global::TUnit.Core.SourceRegistrar.RegisterAssemblyHookSource(source);");
                    }

                    if (validHooks.Any(h => h.HookType == "TestSession"))
                    {
                        writer.AppendLine("global::TUnit.Core.SourceRegistrar.RegisterTestSessionHookSource(source);");
                    }

                    if (validHooks.Any(h => h.HookType == "TestDiscovery"))
                    {
                        writer.AppendLine("global::TUnit.Core.SourceRegistrar.RegisterTestDiscoveryHookSource(source);");
                    }
                }
            }

            context.AddSource("GeneratedHookSource.g.cs", writer.ToString());
        }
        catch (Exception ex)
        {
            var descriptor = new DiagnosticDescriptor(
                "THG001",
                "Hook metadata generation failed",
                "Failed to generate hook metadata: {0}",
                "TUnit",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, ex.ToString()));
        }
    }

    private static void GenerateStorageFields(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        // Generate type-safe hook storage using dictionaries
        writer.AppendLine("// Hook storage: Type -> HookType -> List of hook methods");
        writer.AppendLine("private static readonly global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Collections.Generic.Dictionary<global::TUnit.Core.HookType, global::System.Collections.Generic.List<global::TUnit.Core.Hooks.StaticHookMethod>>> _staticHooksByType = new();");
        writer.AppendLine("private static readonly global::System.Collections.Generic.Dictionary<global::System.Type, global::System.Collections.Generic.Dictionary<global::TUnit.Core.HookType, global::System.Collections.Generic.List<global::TUnit.Core.Hooks.InstanceHookMethod>>> _instanceHooksByType = new();");
        writer.AppendLine();

        // Also keep the existing fields for backward compatibility during refactoring
        var hookGroups = hooks.GroupBy(h => new { h.HookType, h.HookKind });

        foreach (var group in hookGroups)
        {
            var fieldName = GetFieldName(group.Key.HookType, group.Key.HookKind);
            var isStatic = IsStaticHook(group.Key.HookType, group.Key.HookKind);
            var hookClass = GetHookClass(group.Key.HookType, group.Key.HookKind, isStatic);

            writer.AppendLine($"private static readonly List<{hookClass}> {fieldName} = new();");
        }

        writer.AppendLine();
    }

    private static void GenerateStaticConstructor(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        using (writer.BeginBlock("static GeneratedHookSource()"))
        {
            writer.AppendLine("try");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("RegisterAllHookDelegates();");
            writer.AppendLine("InitializeAllHooks();");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("catch (Exception ex)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("throw new InvalidOperationException($\"Failed to initialize hook source: {ex.Message}\", ex);");
            writer.Unindent();
            writer.AppendLine("}");
        }

        writer.AppendLine();

        // Generate delegate registration method
        using (writer.BeginBlock("private static void RegisterAllHookDelegates()"))
        {
            // No longer registering delegates - using direct context passing
            writer.AppendLine("// Hook delegates are no longer used - direct context passing is used instead");
        }

        writer.AppendLine();

        // Generate hook initialization method
        using (writer.BeginBlock("private static void InitializeAllHooks()"))
        {
            writer.AppendLine("// Initialize dictionary-based storage");
            writer.AppendLine("InitializeHookDictionaries();");
            writer.AppendLine();

            var hookGroups = hooks.GroupBy(h => new { h.HookType, h.HookKind });

            foreach (var group in hookGroups)
            {
                var fieldName = GetFieldName(group.Key.HookType, group.Key.HookKind);

                foreach (var hook in group)
                {
                    GenerateHookInitialization(writer, hook, fieldName);
                }
            }
        }

        writer.AppendLine();
    }

    private static void GenerateInitializeHookDictionaries(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        using (writer.BeginBlock("private static void InitializeHookDictionaries()"))
        {
            // Group hooks by the type where they are declared
            var hooksByType = hooks.GroupBy(h => h.TypeSymbol, SymbolEqualityComparer.Default);

            foreach (var typeGroup in hooksByType)
            {
                var typeSymbol = (INamedTypeSymbol)typeGroup.Key!;

                // Skip generic types with unresolved type parameters
                if (HasUnresolvedTypeParameters(typeSymbol))
                {
                    writer.AppendLine($"// Skipping type registration for generic type: {typeSymbol.ToDisplayString()}");
                    continue;
                }

                var typeDisplay = GetTypeDisplayString(typeSymbol);

                // Group by static vs instance
                var instanceHooks = typeGroup.Where(h => !h.MethodSymbol.IsStatic &&
                    ((h.HookKind == "Before" || h.HookKind == "After") && h.HookType == "Test")).ToList();
                var staticHooks = typeGroup.Where(h => !instanceHooks.Contains(h)).ToList();

                if (instanceHooks.Any())
                {
                    writer.AppendLine($"if (!_instanceHooksByType.ContainsKey(typeof({typeDisplay})))");
                    writer.AppendLine($"    _instanceHooksByType[typeof({typeDisplay})] = new global::System.Collections.Generic.Dictionary<global::TUnit.Core.HookType, global::System.Collections.Generic.List<global::TUnit.Core.Hooks.InstanceHookMethod>>();");

                    var hooksByHookType = instanceHooks.GroupBy(h => h.HookType);
                    foreach (var htGroup in hooksByHookType)
                    {
                        writer.AppendLine($"_instanceHooksByType[typeof({typeDisplay})][global::TUnit.Core.HookType.{htGroup.Key}] = new global::System.Collections.Generic.List<global::TUnit.Core.Hooks.InstanceHookMethod>();");
                    }
                }

                if (staticHooks.Any())
                {
                    writer.AppendLine($"if (!_staticHooksByType.ContainsKey(typeof({typeDisplay})))");
                    writer.AppendLine($"    _staticHooksByType[typeof({typeDisplay})] = new global::System.Collections.Generic.Dictionary<global::TUnit.Core.HookType, global::System.Collections.Generic.List<global::TUnit.Core.Hooks.StaticHookMethod>>();");

                    var hooksByHookType = staticHooks.GroupBy(h => h.HookType);
                    foreach (var htGroup in hooksByHookType)
                    {
                        writer.AppendLine($"_staticHooksByType[typeof({typeDisplay})][global::TUnit.Core.HookType.{htGroup.Key}] = new global::System.Collections.Generic.List<global::TUnit.Core.Hooks.StaticHookMethod>();");
                    }
                }
            }
        }
        writer.AppendLine();
    }

    private static void GenerateHookLookupMethods(CodeWriter writer)
    {
        // Generate method to get hooks by type - this handles inheritance
        using (writer.BeginBlock("private static IEnumerable<T> GetHooksForType<T>(Type type, HookType hookType) where T : class"))
        {
            writer.AppendLine("var results = new global::System.Collections.Generic.List<T>();");
            writer.AppendLine();

            writer.AppendLine("// Walk up the inheritance chain");
            writer.AppendLine("var currentType = type;");
            writer.AppendLine("while (currentType != null && currentType != typeof(object))");
            writer.AppendLine("{");
            writer.Indent();

            writer.AppendLine("// Check instance hooks");
            writer.AppendLine("if (typeof(T) == typeof(InstanceHookMethod) && _instanceHooksByType.TryGetValue(currentType, out var instanceDict))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("if (instanceDict.TryGetValue(hookType, out var instanceHooks))");
            writer.AppendLine("    results.AddRange(instanceHooks.Cast<T>());");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();

            writer.AppendLine("// Check static hooks");
            writer.AppendLine("if (_staticHooksByType.TryGetValue(currentType, out var staticDict))");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("if (staticDict.TryGetValue(hookType, out var staticHooks))");
            writer.AppendLine("    results.AddRange(staticHooks.Cast<T>());");
            writer.Unindent();
            writer.AppendLine("}");

            writer.AppendLine("currentType = currentType.BaseType;");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine();

            writer.AppendLine("// Sort by order and return");
            writer.AppendLine("return results.OrderBy(h => (h as dynamic)?.Order ?? 0);");
        }
        writer.AppendLine();

        // Generate convenience method for test hooks that need a specific class type
        using (writer.BeginBlock("public IReadOnlyList<InstanceHookMethod> GetInstanceHooksForClass(Type classType, HookType hookType)"))
        {
            writer.AppendLine("return GetHooksForType<InstanceHookMethod>(classType, hookType).ToList();");
        }
        writer.AppendLine();
    }

    private static void GenerateHookInitialization(CodeWriter writer, HookMethodMetadata hook, string fieldName)
    {
        // Skip hooks from generic types with unresolved type parameters
        if (HasUnresolvedTypeParameters(hook.TypeSymbol))
        {
            writer.AppendLine($"// Skipping hook from generic type: {hook.TypeSymbol.ToDisplayString()}");
            return;
        }

        var isStatic = hook.MethodSymbol.IsStatic;
        var hookClass = GetHookClass(hook.HookType, hook.HookKind, isStatic);
        var delegateKey = GetDelegateKey(hook);

        // For Before/After hooks without "Every", they are instance hooks
        var isInstanceHook = (hook.HookKind == "Before" || hook.HookKind == "After") && hook.HookType == "Test";

        writer.AppendLine($"{fieldName}.Add(new {hookClass}");
        writer.AppendLine("{");
        writer.Indent();

        // Only instance hooks have ClassType
        if (isInstanceHook)
        {
            // For generic types, use the open generic form (e.g., BaseClass<>)
            var typeDisplay = GetTypeDisplayString(hook.TypeSymbol);
            writer.AppendLine($"ClassType = typeof({typeDisplay}),");
        }

        writer.AppendLine("MethodInfo = null!,"); // Will be populated by runtime
        writer.AppendLine("HookExecutor = null!,"); // This will be set by the engine
        writer.AppendLine($"Order = {hook.Order},");
        writer.AppendLine($"Body = {delegateKey}_Body" + (isInstanceHook ? "" : ","));

        // Only static hooks have FilePath and LineNumber
        if (!isInstanceHook)
        {
            writer.AppendLine($"FilePath = @\"{hook.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"LineNumber = {hook.LineNumber}");
        }

        writer.Unindent();
        writer.AppendLine("});");

        // Also add to the type-based dictionary
        var hookTypeDisplay = GetTypeDisplayString(hook.TypeSymbol);

        if (isInstanceHook)
        {
            writer.AppendLine($"_instanceHooksByType[typeof({hookTypeDisplay})][HookType.{hook.HookType}].Add({fieldName}.Last() as InstanceHookMethod);");
        }
        else
        {
            writer.AppendLine($"_staticHooksByType[typeof({hookTypeDisplay})][HookType.{hook.HookType}].Add({fieldName}.Last() as StaticHookMethod);");
        }

        writer.AppendLine();
    }

    private static void GenerateInterfaceImplementations(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        // Group hooks by type
        var hooksByType = hooks.GroupBy(h => h.HookType).ToDictionary(g => g.Key, g => g.ToList());

        // Generate interface methods for each hook type we have
        if (hooksByType.ContainsKey("Test"))
        {
            GenerateTestHookMethods(writer, hooksByType["Test"]);
        }

        if (hooksByType.ContainsKey("Class"))
        {
            GenerateClassHookMethods(writer, hooksByType["Class"]);
        }

        if (hooksByType.ContainsKey("Assembly"))
        {
            GenerateAssemblyHookMethods(writer, hooksByType["Assembly"]);
        }

        if (hooksByType.ContainsKey("TestSession"))
        {
            GenerateTestSessionHookMethods(writer, hooksByType["TestSession"]);
        }

        if (hooksByType.ContainsKey("TestDiscovery"))
        {
            GenerateTestDiscoveryHookMethods(writer, hooksByType["TestDiscovery"]);
        }
    }

    private static void GenerateTestHookMethods(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        var hasBefore = hooks.Any(h => h.HookKind == "Before");
        var hasAfter = hooks.Any(h => h.HookKind == "After");
        var hasBeforeEvery = hooks.Any(h => h.HookKind == "BeforeEvery");
        var hasAfterEvery = hooks.Any(h => h.HookKind == "AfterEvery");

        writer.AppendLine("public IReadOnlyList<InstanceHookMethod> CollectBeforeTestHooks(string sessionId)");
        if (hasBefore)
        {
            writer.AppendLine($"    => {GetFieldName("Test", "Before")}.Cast<InstanceHookMethod>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<InstanceHookMethod>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<InstanceHookMethod> CollectAfterTestHooks(string sessionId)");
        if (hasAfter)
        {
            writer.AppendLine($"    => {GetFieldName("Test", "After")}.Cast<InstanceHookMethod>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<InstanceHookMethod>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<TestContext>> CollectBeforeEveryTestHooks(string sessionId)");
        if (hasBeforeEvery)
        {
            writer.AppendLine($"    => {GetFieldName("Test", "BeforeEvery")}.Cast<StaticHookMethod<TestContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<TestContext>>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<TestContext>> CollectAfterEveryTestHooks(string sessionId)");
        if (hasAfterEvery)
        {
            writer.AppendLine($"    => {GetFieldName("Test", "AfterEvery")}.Cast<StaticHookMethod<TestContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<TestContext>>();");
        }
        writer.AppendLine();
    }

    private static void GenerateClassHookMethods(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        var hasBefore = hooks.Any(h => h.HookKind == "Before");
        var hasAfter = hooks.Any(h => h.HookKind == "After");
        var hasBeforeEvery = hooks.Any(h => h.HookKind == "BeforeEvery");
        var hasAfterEvery = hooks.Any(h => h.HookKind == "AfterEvery");

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectBeforeClassHooks(string sessionId)");
        if (hasBefore)
        {
            writer.AppendLine($"    => {GetFieldName("Class", "Before")}.Cast<StaticHookMethod<ClassHookContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<ClassHookContext>>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectAfterClassHooks(string sessionId)");
        if (hasAfter)
        {
            writer.AppendLine($"    => {GetFieldName("Class", "After")}.Cast<StaticHookMethod<ClassHookContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<ClassHookContext>>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectBeforeEveryClassHooks(string sessionId)");
        if (hasBeforeEvery)
        {
            writer.AppendLine($"    => {GetFieldName("Class", "BeforeEvery")}.Cast<StaticHookMethod<ClassHookContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<ClassHookContext>>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<ClassHookContext>> CollectAfterEveryClassHooks(string sessionId)");
        if (hasAfterEvery)
        {
            writer.AppendLine($"    => {GetFieldName("Class", "AfterEvery")}.Cast<StaticHookMethod<ClassHookContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<ClassHookContext>>();");
        }
        writer.AppendLine();
    }

    private static void GenerateAssemblyHookMethods(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        var hasBefore = hooks.Any(h => h.HookKind == "Before");
        var hasAfter = hooks.Any(h => h.HookKind == "After");
        var hasBeforeEvery = hooks.Any(h => h.HookKind == "BeforeEvery");
        var hasAfterEvery = hooks.Any(h => h.HookKind == "AfterEvery");

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectBeforeAssemblyHooks(string sessionId)");
        if (hasBefore)
        {
            writer.AppendLine($"    => {GetFieldName("Assembly", "Before")}.Cast<StaticHookMethod<AssemblyHookContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<AssemblyHookContext>>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectAfterAssemblyHooks(string sessionId)");
        if (hasAfter)
        {
            writer.AppendLine($"    => {GetFieldName("Assembly", "After")}.Cast<StaticHookMethod<AssemblyHookContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<AssemblyHookContext>>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectBeforeEveryAssemblyHooks(string sessionId)");
        if (hasBeforeEvery)
        {
            writer.AppendLine($"    => {GetFieldName("Assembly", "BeforeEvery")}.Cast<StaticHookMethod<AssemblyHookContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<AssemblyHookContext>>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<AssemblyHookContext>> CollectAfterEveryAssemblyHooks(string sessionId)");
        if (hasAfterEvery)
        {
            writer.AppendLine($"    => {GetFieldName("Assembly", "AfterEvery")}.Cast<StaticHookMethod<AssemblyHookContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<AssemblyHookContext>>();");
        }
        writer.AppendLine();
    }

    private static void GenerateTestSessionHookMethods(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        var hasBefore = hooks.Any(h => h.HookKind == "Before");
        var hasAfter = hooks.Any(h => h.HookKind == "After");

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<TestSessionContext>> CollectBeforeTestSessionHooks(string sessionId)");
        if (hasBefore)
        {
            writer.AppendLine($"    => {GetFieldName("TestSession", "Before")}.Cast<StaticHookMethod<TestSessionContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<TestSessionContext>>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<TestSessionContext>> CollectAfterTestSessionHooks(string sessionId)");
        if (hasAfter)
        {
            writer.AppendLine($"    => {GetFieldName("TestSession", "After")}.Cast<StaticHookMethod<TestSessionContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<TestSessionContext>>();");
        }
        writer.AppendLine();
    }

    private static void GenerateTestDiscoveryHookMethods(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        var hasBefore = hooks.Any(h => h.HookKind == "Before");
        var hasAfter = hooks.Any(h => h.HookKind == "After");

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<BeforeTestDiscoveryContext>> CollectBeforeTestDiscoveryHooks(string sessionId)");
        if (hasBefore)
        {
            writer.AppendLine($"    => {GetFieldName("TestDiscovery", "Before")}.Cast<StaticHookMethod<BeforeTestDiscoveryContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<BeforeTestDiscoveryContext>>();");
        }
        writer.AppendLine();

        writer.AppendLine("public IReadOnlyList<StaticHookMethod<TestDiscoveryContext>> CollectAfterTestDiscoveryHooks(string sessionId)");
        if (hasAfter)
        {
            writer.AppendLine($"    => {GetFieldName("TestDiscovery", "After")}.Cast<StaticHookMethod<TestDiscoveryContext>>().ToList();");
        }
        else
        {
            writer.AppendLine("    => Array.Empty<StaticHookMethod<TestDiscoveryContext>>();");
        }
        writer.AppendLine();
    }

    private static void GenerateHookDelegates(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        writer.AppendLine("// Hook delegates for AOT execution");
        writer.AppendLine();

        // Generate delegates only once per unique method
        var uniqueMethods = hooks
            .GroupBy(h => h.MethodSymbol, SymbolEqualityComparer.Default)
            .Select(g => g.First());

        foreach (var hook in uniqueMethods)
        {
            GenerateHookDelegate(writer, hook);
        }
    }

    private static void GenerateHookDelegate(CodeWriter writer, HookMethodMetadata hook)
    {
        // Skip hooks from generic types with unresolved type parameters
        if (HasUnresolvedTypeParameters(hook.TypeSymbol))
        {
            writer.AppendLine($"// Skipping delegate for hook from generic type: {hook.TypeSymbol.ToDisplayString()}");
            return;
        }

        var delegateKey = GetDelegateKey(hook);

        // For generic types, we need to handle them specially to avoid compilation errors
        var className = GetTypeDisplayString(hook.TypeSymbol);
        var methodName = hook.MethodSymbol.Name;
        var isStatic = hook.MethodSymbol.IsStatic;
        IsAsyncMethod(hook.MethodSymbol);

        // Analyze parameters
        var paramCount = hook.MethodSymbol.Parameters.Length;
        var hasCancellationTokenOnly = false;
        var hasContextOnly = false;
        var hasContextAndCancellationToken = false;

        if (paramCount == 1)
        {
            var paramType = hook.MethodSymbol.Parameters[0].Type;
            if (paramType.Name == "CancellationToken" && paramType.ContainingNamespace?.ToString() == "System.Threading")
            {
                hasCancellationTokenOnly = true;
            }
            else
            {
                hasContextOnly = true;
            }
        }
        else if (paramCount == 2)
        {
            // We already validated in IsValidHookMethod that this is Context + CancellationToken
            hasContextAndCancellationToken = true;
        }

        // Skip delegate generation - we'll use direct context passing
        writer.AppendLine();

        // Generate the Body delegate for hook initialization
        var contextType = GetContextTypeForBody(hook.HookType, hook.HookKind);

        // For Before/After hooks without "Every", they are instance hooks
        var isInstanceHook = (hook.HookKind == "Before" || hook.HookKind == "After") && hook.HookType == "Test";

        if (isInstanceHook)
        {
            // Instance hooks - match InstanceHookMethod.Body signature
            using (writer.BeginBlock($"private static async ValueTask {delegateKey}_Body(object instance, {contextType} context, CancellationToken cancellationToken)"))
            {
                writer.AppendLine($"var typedInstance = ({className})instance;");

                // Build method call
                var methodCall = isStatic
                    ? $"{className}.{methodName}"
                    : $"typedInstance.{methodName}";

                if (hasCancellationTokenOnly)
                {
                    // Pass only the cancellation token
                    methodCall += "(cancellationToken)";
                }
                else if (hasContextOnly)
                {
                    // Pass only the context
                    methodCall += "(context)";
                }
                else if (hasContextAndCancellationToken)
                {
                    // Pass both context and cancellation token
                    methodCall += "(context, cancellationToken)";
                }
                else
                {
                    // No parameters
                    methodCall += "()";
                }

                // Use AsyncConvert to handle all return types
                writer.AppendLine($"await AsyncConvert.Convert(() => {methodCall});");
            }
        }
        else
        {
            // Static hooks - match StaticHookMethod<T>.Body signature
            using (writer.BeginBlock($"private static async ValueTask {delegateKey}_Body({contextType} context, CancellationToken cancellationToken)"))
            {
                // Build method call
                var methodCall = $"{className}.{methodName}";

                if (hasCancellationTokenOnly)
                {
                    // Pass only the cancellation token
                    methodCall += "(cancellationToken)";
                }
                else if (hasContextOnly)
                {
                    // Pass only the context
                    methodCall += "(context)";
                }
                else if (hasContextAndCancellationToken)
                {
                    // Pass both context and cancellation token
                    methodCall += "(context, cancellationToken)";
                }
                else
                {
                    // No parameters
                    methodCall += "()";
                }

                // Use AsyncConvert to handle all return types
                writer.AppendLine($"await AsyncConvert.Convert(() => {methodCall});");
            }
        }

        writer.AppendLine();
    }

    private static string GetFieldName(string hookType, string hookKind)
    {
        return $"_{hookKind.ToLower()}{hookType}Hooks";
    }

    private static string GetDelegateKey(HookMethodMetadata hook)
    {
        // Use the type where the hook is declared for the delegate key
        var declaringType = hook.TypeSymbol;

        // Get the full type name including namespace to ensure uniqueness
        var fullTypeName = declaringType.ToDisplayString();

        // For generic types, remove type parameters to avoid compilation issues
        if (declaringType.IsGenericType)
        {
            var genericIndex = fullTypeName.IndexOf('<');
            if (genericIndex > 0)
            {
                fullTypeName = fullTypeName.Substring(0, genericIndex);
            }
        }

        // Make the name safe for use as an identifier
        var safeClassName = fullTypeName
            .Replace(".", "_")
            .Replace("::", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "")
            .Replace("`", "_")
            .Replace("+", "_"); // Handle nested classes

        // Include parameter count to handle overloads
        var paramCount = hook.MethodSymbol.Parameters.Length;
        return $"{safeClassName}_{hook.MethodSymbol.Name}_{paramCount}Params";
    }

    private static bool IsStaticHook(string hookType, string hookKind)
    {
        // Before/After Test hooks (without "Every") are instance hooks
        // All other hooks are static
        return !((hookKind == "Before" || hookKind == "After") && hookType == "Test");
    }

    private static string GetHookClass(string hookType, string hookKind, bool isStatic)
    {
        // Use concrete hook method classes
        return (hookType, hookKind) switch
        {
            ("Test", "Before") when !isStatic => "InstanceHookMethod",
            ("Test", "After") when !isStatic => "InstanceHookMethod",
            ("Test", "Before") => "BeforeTestHookMethod",
            ("Test", "After") => "AfterTestHookMethod",
            ("Test", "BeforeEvery") => "BeforeTestHookMethod",
            ("Test", "AfterEvery") => "AfterTestHookMethod",
            ("Class", "Before") => "BeforeClassHookMethod",
            ("Class", "After") => "AfterClassHookMethod",
            ("Class", "BeforeEvery") => "BeforeClassHookMethod",
            ("Class", "AfterEvery") => "AfterClassHookMethod",
            ("Assembly", "Before") => "BeforeAssemblyHookMethod",
            ("Assembly", "After") => "AfterAssemblyHookMethod",
            ("Assembly", "BeforeEvery") => "BeforeAssemblyHookMethod",
            ("Assembly", "AfterEvery") => "AfterAssemblyHookMethod",
            ("TestSession", "Before") => "BeforeTestSessionHookMethod",
            ("TestSession", "After") => "AfterTestSessionHookMethod",
            ("TestSession", "BeforeEvery") => "BeforeTestSessionHookMethod",
            ("TestSession", "AfterEvery") => "AfterTestSessionHookMethod",
            ("TestDiscovery", "Before") => "BeforeTestDiscoveryHookMethod",
            ("TestDiscovery", "After") => "AfterTestDiscoveryHookMethod",
            ("TestDiscovery", "BeforeEvery") => "BeforeTestDiscoveryHookMethod",
            ("TestDiscovery", "AfterEvery") => "AfterTestDiscoveryHookMethod",
            _ => "BeforeTestHookMethod"
        };
    }

    private static string GetContextType(string hookType)
    {
        return hookType switch
        {
            "Test" => "TestContext",
            "Class" => "ClassHookContext",
            "Assembly" => "AssemblyHookContext",
            "TestSession" => "TestSessionContext",
            "TestDiscovery" => "BeforeTestDiscoveryContext", // Both Before and After use different contexts
            _ => "TestContext"
        };
    }

    private static string GetContextTypeForBody(string hookType, string hookKind)
    {
        return (hookType, hookKind) switch
        {
            ("Test", _) => "TestContext",
            ("Class", _) => "ClassHookContext",
            ("Assembly", _) => "AssemblyHookContext",
            ("TestSession", _) => "TestSessionContext",
            ("TestDiscovery", "Before") => "BeforeTestDiscoveryContext",
            ("TestDiscovery", "After") => "TestDiscoveryContext",
            ("TestDiscovery", "BeforeEvery") => "BeforeTestDiscoveryContext",
            ("TestDiscovery", "AfterEvery") => "TestDiscoveryContext",
            _ => "TestContext"
        };
    }

    private static bool IsAsyncMethod(IMethodSymbol method)
    {
        var returnType = method.ReturnType;
        return returnType.Name == "Task" || returnType.Name == "ValueTask" ||
               (returnType is INamedTypeSymbol { IsGenericType: true } namedType &&
                   (namedType.ConstructedFrom.Name == "Task" || namedType.ConstructedFrom.Name == "ValueTask"));
    }

    private static string GetTypeDisplayString(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.ToDisplayString();
    }

    private static bool HasUnresolvedTypeParameters(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.IsGenericType && typeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);
    }
}

// Hook metadata model
public class HookMethodMetadata
{
    public required IMethodSymbol MethodSymbol { get; init; }
    public required INamedTypeSymbol TypeSymbol { get; init; } // The type where the hook is declared
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required AttributeData HookAttribute { get; init; }
    public required string HookKind { get; init; } // Before, After, BeforeEvery, AfterEvery
    public required string HookType { get; init; } // Test, Class, Assembly, TestSession, TestDiscovery
    public required int Order { get; init; }
    public GeneratorAttributeSyntaxContext? Context { get; init; }
}
