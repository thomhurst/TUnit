using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Source generator that emits unified hook metadata for AOT support
/// </summary>
[Generator]
public class UnifiedHookMetadataGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

        // Combine all collected hooks
        var allHooks = beforeHooksCollected
            .Combine(afterHooksCollected)
            .Combine(beforeEveryHooksCollected)
            .Combine(afterEveryHooksCollected);

        // Generate the hook registry
        context.RegisterSourceOutput(allHooks, (context, data) =>
        {
            var (((beforeHooksList, afterHooksList), beforeEveryHooksList), afterEveryHooksList) = data;
            var directHooks = beforeHooksList
                .Concat(afterHooksList)
                .Concat(beforeEveryHooksList)
                .Concat(afterEveryHooksList)
                .Where(h => h != null)
                .Cast<HookMethodMetadata>()
                .ToList();

            // Process and deduplicate hooks
            var validHooks = ProcessHooks(directHooks);
            GenerateHookRegistry(context, validHooks.ToImmutableArray());
        });
    }

    private static List<HookMethodMetadata> ProcessHooks(List<HookMethodMetadata> directHooks)
    {
        // Include all hooks - the engine will handle inheritance properly
        // We should not filter out abstract generic classes as they can define valid hooks for derived classes
        var validDirectHooks = directHooks.ToList();

        // Deduplicate hooks
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

            // Generate the hook registry implementation
            using (writer.BeginBlock("public sealed class GeneratedHookRegistry"))
            {
                // Generate storage fields
                GenerateStorageFields(writer, validHooks);

                // Generate static constructor
                GenerateStaticConstructor(writer, validHooks);

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
                    writer.AppendLine("_ = new GeneratedHookRegistry();");
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
    }

    private static void GenerateStaticConstructor(CodeWriter writer, List<HookMethodMetadata> hooks)
    {
        using (writer.BeginBlock("static GeneratedHookRegistry()"))
        {
            writer.AppendLine("try");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("PopulateSourcesDictionaries();");
            writer.Unindent();
            writer.AppendLine("}");
            writer.AppendLine("catch (Exception ex)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("throw new InvalidOperationException($\"Failed to initialize hook registry: {ex.Message}\", ex);");
            writer.Unindent();
            writer.AppendLine("}");
        }

        writer.AppendLine();

        // Generate hook initialization method
        using (writer.BeginBlock("private static void PopulateSourcesDictionaries()"))
        {
            // Group hooks by type for efficient dictionary population
            var hooksByType = hooks.GroupBy(h => h.TypeSymbol, SymbolEqualityComparer.Default);

            foreach (var typeGroup in hooksByType)
            {
                var typeSymbol = (INamedTypeSymbol)typeGroup.Key!;

                var typeDisplay = typeSymbol.GloballyQualified();

                // Group by hook kind and type
                var testHooks = typeGroup.Where(h => h.HookType == "Test").ToList();
                var classHooks = typeGroup.Where(h => h.HookType == "Class").ToList();

                // Generate test hook registrations
                if (testHooks.Any())
                {

                    var beforeTestHooks = testHooks.Where(h => h.HookKind == "Before").ToList();
                    if (beforeTestHooks.Any())
                    {
                        GenerateHookListPopulation(writer, "BeforeTestHooks", typeDisplay, beforeTestHooks, isInstance: true);
                    }

                    var afterTestHooks = testHooks.Where(h => h.HookKind == "After").ToList();
                    if (afterTestHooks.Any())
                    {
                        GenerateHookListPopulation(writer, "AfterTestHooks", typeDisplay, afterTestHooks, isInstance: true);
                    }

                    var beforeEveryTestHooks = testHooks.Where(h => h.HookKind == "BeforeEvery").ToList();
                    if (beforeEveryTestHooks.Any())
                    {
                        GenerateHookListPopulation(writer, "BeforeEveryTestHooks", typeDisplay, beforeEveryTestHooks, isInstance: false);
                    }

                    var afterEveryTestHooks = testHooks.Where(h => h.HookKind == "AfterEvery").ToList();
                    if (afterEveryTestHooks.Any())
                    {
                        GenerateHookListPopulation(writer, "AfterEveryTestHooks", typeDisplay, afterEveryTestHooks, isInstance: false);
                    }
                }

                // Generate class hook registrations
                if (classHooks.Any())
                {

                    var beforeClassHooks = classHooks.Where(h => h.HookKind == "Before").ToList();
                    if (beforeClassHooks.Any())
                    {
                        GenerateHookListPopulation(writer, "BeforeClassHooks", typeDisplay, beforeClassHooks, isInstance: false);
                    }

                    var afterClassHooks = classHooks.Where(h => h.HookKind == "After").ToList();
                    if (afterClassHooks.Any())
                    {
                        GenerateHookListPopulation(writer, "AfterClassHooks", typeDisplay, afterClassHooks, isInstance: false);
                    }
                }
            }

            // Handle assembly hooks separately since they're keyed by Assembly
            var assemblyHookGroups = hooks.Where(h => h.HookType == "Assembly")
                .GroupBy(h => h.TypeSymbol.ContainingAssembly, SymbolEqualityComparer.Default);

            foreach (var assemblyGroup in assemblyHookGroups)
            {
                var assembly = (IAssemblySymbol)assemblyGroup.Key!;
                var assemblyName = assembly.Name;

                writer.AppendLine($"var {assemblyName.Replace(".", "_")}_assembly = typeof({assemblyGroup.First().TypeSymbol.GloballyQualified()}).Assembly;");

                var beforeAssemblyHooks = assemblyGroup.Where(h => h.HookKind == "Before").ToList();
                if (beforeAssemblyHooks.Any())
                {
                    GenerateAssemblyHookListPopulation(writer, "BeforeAssemblyHooks", assemblyName, beforeAssemblyHooks);
                }

                var afterAssemblyHooks = assemblyGroup.Where(h => h.HookKind == "After").ToList();
                if (afterAssemblyHooks.Any())
                {
                    GenerateAssemblyHookListPopulation(writer, "AfterAssemblyHooks", assemblyName, afterAssemblyHooks);
                }
            }

            // Handle test session and test discovery hooks (not type-indexed)
            var testSessionHooks = hooks.Where(h => h.HookType == "TestSession").ToList();
            if (testSessionHooks.Any())
            {

                var beforeTestSessionHooks = testSessionHooks.Where(h => h.HookKind == "Before").ToList();
                if (beforeTestSessionHooks.Any())
                {
                    GenerateGlobalHookListPopulation(writer, "BeforeTestSessionHooks", beforeTestSessionHooks);
                }

                var afterTestSessionHooks = testSessionHooks.Where(h => h.HookKind == "After").ToList();
                if (afterTestSessionHooks.Any())
                {
                    GenerateGlobalHookListPopulation(writer, "AfterTestSessionHooks", afterTestSessionHooks);
                }
            }

            var testDiscoveryHooks = hooks.Where(h => h.HookType == "TestDiscovery").ToList();
            if (testDiscoveryHooks.Any())
            {

                var beforeTestDiscoveryHooks = testDiscoveryHooks.Where(h => h.HookKind == "Before").ToList();
                if (beforeTestDiscoveryHooks.Any())
                {
                    GenerateGlobalHookListPopulation(writer, "BeforeTestDiscoveryHooks", beforeTestDiscoveryHooks);
                }

                var afterTestDiscoveryHooks = testDiscoveryHooks.Where(h => h.HookKind == "After").ToList();
                if (afterTestDiscoveryHooks.Any())
                {
                    GenerateGlobalHookListPopulation(writer, "AfterTestDiscoveryHooks", afterTestDiscoveryHooks);
                }
            }
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
        var delegateKey = GetDelegateKey(hook);

        // For generic types, we need to handle them specially to avoid compilation errors
        var className = hook.TypeSymbol.GloballyQualified();
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
                // Check if we're dealing with an open generic type
                var isOpenGeneric = hook.TypeSymbol.IsGenericType && hook.TypeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);

                if (isOpenGeneric)
                {
                    // For open generic types, use dynamic to avoid invalid cast
                    writer.AppendLine($"// Instance method on open generic type - using dynamic");
                    writer.AppendLine($"dynamic dynamicInstance = instance;");
                    
                    // Build method call
                    var methodCall = isStatic
                        ? $"{className}.{methodName}"
                        : $"dynamicInstance.{methodName}";

                    if (hasCancellationTokenOnly)
                    {
                        methodCall += "(cancellationToken)";
                    }
                    else if (hasContextOnly)
                    {
                        methodCall += "(context)";
                    }
                    else if (hasContextAndCancellationToken)
                    {
                        methodCall += "(context, cancellationToken)";
                    }
                    else
                    {
                        methodCall += "()";
                    }

                    writer.AppendLine($"await AsyncConvert.Convert(() => {methodCall});");
                }
                else
                {
                    // For concrete types, use normal cast
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
        }
        else
        {
            // Static hooks - match StaticHookMethod<T>.Body signature
            using (writer.BeginBlock($"private static async ValueTask {delegateKey}_Body({contextType} context, CancellationToken cancellationToken)"))
            {
                // Check if we're dealing with an open generic type
                var isOpenGeneric = hook.TypeSymbol.IsGenericType && hook.TypeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);

                if (isOpenGeneric && isStatic)
                {
                    // For static methods on open generic types, we need to use reflection
                    writer.AppendLine($"// Static method on open generic type - using reflection");
                    writer.AppendLine($"var method = typeof({hook.TypeSymbol.GloballyQualifiedNonGeneric()}).GetMethod(\"{methodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);");
                    
                    // Build parameters array based on what the method expects
                    if (hasCancellationTokenOnly)
                    {
                        writer.AppendLine($"var parameters = new object[] {{ cancellationToken }};");
                    }
                    else if (hasContextOnly)
                    {
                        writer.AppendLine($"var parameters = new object[] {{ context }};");
                    }
                    else if (hasContextAndCancellationToken)
                    {
                        writer.AppendLine($"var parameters = new object[] {{ context, cancellationToken }};");
                    }
                    else
                    {
                        writer.AppendLine($"var parameters = new object[0];");
                    }
                    
                    writer.AppendLine($"await AsyncConvert.Convert(() => method!.Invoke(null, parameters));");
                }
                else
                {
                    // For non-generic types or instance methods, use direct invocation
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
        }

        writer.AppendLine();
    }

    private static void GenerateHookListPopulation(CodeWriter writer, string dictionaryName, string typeDisplay, List<HookMethodMetadata> hooks, bool isInstance)
    {
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}.GetOrAdd(typeof({typeDisplay}), _ => new global::System.Collections.Concurrent.ConcurrentBag<global::TUnit.Core.Hooks.{(isInstance ? "InstanceHookMethod" : $"StaticHookMethod<{GetContextType(hooks.First().HookType)}>")}>());");

        foreach (var hook in hooks.OrderBy(h => h.Order))
        {
            writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}[typeof({typeDisplay})].Add(");
            writer.Indent();
            GenerateHookObject(writer, hook, isInstance);
            writer.Unindent();
            writer.AppendLine(");");
        }
        writer.AppendLine();
    }

    private static void GenerateAssemblyHookListPopulation(CodeWriter writer, string dictionaryName, string assemblyVarName, List<HookMethodMetadata> hooks)
    {
        var assemblyVar = assemblyVarName.Replace(".", "_") + "_assembly";
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}.GetOrAdd({assemblyVar}, _ => new global::System.Collections.Concurrent.ConcurrentBag<global::TUnit.Core.Hooks.StaticHookMethod<AssemblyHookContext>>());");

        foreach (var hook in hooks.OrderBy(h => h.Order))
        {
            writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}[{assemblyVar}].Add(");
            writer.Indent();
            GenerateHookObject(writer, hook, false);
            writer.Unindent();
            writer.AppendLine(");");
        }
        writer.AppendLine();
    }

    private static void GenerateGlobalHookListPopulation(CodeWriter writer, string listName, List<HookMethodMetadata> hooks)
    {
        foreach (var hook in hooks.OrderBy(h => h.Order))
        {
            writer.AppendLine($"global::TUnit.Core.Sources.{listName}.Add(");
            writer.Indent();
            GenerateHookObject(writer, hook, false);
            writer.Unindent();
            writer.AppendLine(");");
        }
        writer.AppendLine();
    }

    private static void GenerateHookObject(CodeWriter writer, HookMethodMetadata hook, bool isInstance)
    {
        var hookClass = GetHookClass(hook.HookType, hook.HookKind, !isInstance);
        var delegateKey = GetDelegateKey(hook);

        writer.AppendLine($"new {hookClass}");
        writer.AppendLine("{");
        writer.Indent();

        if (isInstance)
        {
            writer.AppendLine($"ClassType = typeof({hook.TypeSymbol.GloballyQualified()}),");
        }

        writer.Append("MethodInfo = ");
        SourceInformationWriter.GenerateMethodInformation(writer, hook.Context.SemanticModel.Compilation, hook.TypeSymbol, hook.MethodSymbol, null, ',');
        writer.AppendLine();
        writer.AppendLine("HookExecutor = null!,");
        writer.AppendLine($"Order = {hook.Order},");
        writer.AppendLine($"Body = {delegateKey}_Body" + (isInstance ? "" : ","));

        if (!isInstance)
        {
            writer.AppendLine($"FilePath = @\"{hook.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"LineNumber = {hook.LineNumber}");
        }

        writer.Unindent();
        writer.Append("}");
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
        var fullTypeName = declaringType.GloballyQualified();

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
    public required GeneratorAttributeSyntaxContext Context { get; init; }
}
