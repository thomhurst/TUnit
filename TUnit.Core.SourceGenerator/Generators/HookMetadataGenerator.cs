using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Extensions;

namespace TUnit.Core.SourceGenerator.Generators;

[Generator]
public class HookMetadataGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
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

        var beforeHooksCollected = beforeHooks.Collect();
        var afterHooksCollected = afterHooks.Collect();
        var beforeEveryHooksCollected = beforeEveryHooks.Collect();
        var afterEveryHooksCollected = afterEveryHooks.Collect();

        var allHooks = beforeHooksCollected
            .Combine(afterHooksCollected)
            .Combine(beforeEveryHooksCollected)
            .Combine(afterEveryHooksCollected);

        context.RegisterSourceOutput(allHooks, (sourceProductionContext, data) =>
        {
            var (((beforeHooksList, afterHooksList), beforeEveryHooksList), afterEveryHooksList) = data;
            var directHooks = beforeHooksList
                .Concat(afterHooksList)
                .Concat(beforeEveryHooksList)
                .Concat(afterEveryHooksList)
                .Where(h => h != null)
                .Cast<HookMethodMetadata>()
                .ToList();

            var validHooks = ProcessHooks(directHooks);
            GenerateHookRegistry(sourceProductionContext, validHooks.ToImmutableArray());
        });
    }

    private static List<HookMethodMetadata> ProcessHooks(List<HookMethodMetadata> directHooks)
    {
        var validDirectHooks = directHooks.ToList();

        return validDirectHooks
            .GroupBy(h => h, new HookEqualityComparer())
            .Select(g => g.First())
            .ToList();
    }

    private class HookEqualityComparer : IEqualityComparer<HookMethodMetadata>
    {
        public bool Equals(HookMethodMetadata? x, HookMethodMetadata? y)
        {
            if (x == null || y == null)
            {
                return x == y;
            }

            return SymbolEqualityComparer.Default.Equals(x.TypeSymbol, y.TypeSymbol) &&
                   SymbolEqualityComparer.Default.Equals(x.MethodSymbol, y.MethodSymbol);
        }

        public int GetHashCode(HookMethodMetadata? obj)
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

        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            return null;
        }

        var hookAttribute = context.Attributes[0];
        var hookType = GetHookType(hookAttribute);

        if (!IsValidHookMethod(methodSymbol, hookType))
        {
            return null;
        }

        var location = context.TargetNode.GetLocation();
        var filePath = location.SourceTree?.FilePath ?? "";
        var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;

        var order = GetHookOrder(hookAttribute);
        var hookExecutor = GetHookExecutorType(methodSymbol);

        return new HookMethodMetadata
        {
            MethodSymbol = methodSymbol,
            TypeSymbol = typeSymbol,
            FilePath = filePath,
            LineNumber = lineNumber,
            HookKind = hookKind,
            HookType = hookType,
            Order = order,
            Context = context,
            HookExecutor = hookExecutor
        };
    }

    private static bool IsValidHookMethod(IMethodSymbol method, string hookType)
    {
        var returnType = method.ReturnType;
        if (returnType.SpecialType != SpecialType.System_Void &&
            returnType.Name != "Task" &&
            returnType.Name != "ValueTask")
        {
            return false;
        }

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

            if (firstParamTypeName == "CancellationToken" && firstParamNamespace == "System.Threading")
            {
                return method.Parameters.Length == 1;
            }

            var expectedContextType = hookType switch
            {
                "Test" => "TestContext",
                "Class" => "ClassHookContext",
                "Assembly" => "AssemblyHookContext",
                "TestSession" => "TestSessionContext",
                "TestDiscovery" => "TestDiscoveryContext",
                _ => null
            };

            if (hookType == "TestDiscovery" && method.Name.Contains("Before"))
            {
                expectedContextType = "BeforeTestDiscoveryContext";
            }

            if (expectedContextType == null || firstParamNamespace != "TUnit.Core" || firstParamTypeName != expectedContextType)
            {
                return false;
            }

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

    private static string? GetHookExecutorType(IMethodSymbol methodSymbol)
    {
        var hookExecutorAttribute = methodSymbol.GetAttributes()
            .FirstOrDefault(a => a.AttributeClass?.Name == "HookExecutorAttribute" || 
                                 (a.AttributeClass?.IsGenericType == true && 
                                  a.AttributeClass?.ConstructedFrom?.Name == "HookExecutorAttribute"));

        if (hookExecutorAttribute == null)
        {
            return null;
        }

        // For generic HookExecutorAttribute<T>, get the type argument
        if (hookExecutorAttribute.AttributeClass?.IsGenericType == true)
        {
            var typeArg = hookExecutorAttribute.AttributeClass.TypeArguments.FirstOrDefault();
            return typeArg?.GloballyQualified();
        }

        // For non-generic HookExecutorAttribute(Type type), get the constructor argument
        var typeArgument = hookExecutorAttribute.ConstructorArguments.FirstOrDefault();
        if (typeArgument.Value is ITypeSymbol typeSymbol)
        {
            return typeSymbol.GloballyQualified();
        }

        return null;
    }

    private static string GetConcreteHookType(string dictionaryName, bool isInstance)
    {
        if (isInstance)
        {
            return "InstanceHookMethod";
        }

        return dictionaryName switch
        {
            "BeforeClassHooks" => "BeforeClassHookMethod",
            "AfterClassHooks" => "AfterClassHookMethod",
            "BeforeAssemblyHooks" => "BeforeAssemblyHookMethod",
            "AfterAssemblyHooks" => "AfterAssemblyHookMethod",
            "BeforeTestSessionHooks" => "BeforeTestSessionHookMethod",
            "AfterTestSessionHooks" => "AfterTestSessionHookMethod",
            "BeforeTestDiscoveryHooks" => "BeforeTestDiscoveryHookMethod",
            "AfterTestDiscoveryHooks" => "AfterTestDiscoveryHookMethod",
            "BeforeEveryTestHooks" => "BeforeTestHookMethod",
            "AfterEveryTestHooks" => "AfterTestHookMethod",
            "BeforeEveryClassHooks" => "BeforeClassHookMethod",
            "AfterEveryClassHooks" => "AfterClassHookMethod",
            "BeforeEveryAssemblyHooks" => "BeforeAssemblyHookMethod",
            "AfterEveryAssemblyHooks" => "AfterAssemblyHookMethod",
            _ => throw new ArgumentException($"Unknown dictionary name: {dictionaryName}")
        };
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

            using (writer.BeginBlock("public sealed class GeneratedHookRegistry"))
            {
                GenerateStaticConstructor(writer, validHooks);

                GenerateHookDelegates(writer, validHooks);
            }

            writer.AppendLine();

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

        using (writer.BeginBlock("private static void PopulateSourcesDictionaries()"))
        {
            var hooksByType = hooks.GroupBy(h => h.TypeSymbol, SymbolEqualityComparer.Default);

            foreach (var typeGroup in hooksByType)
            {
                var typeSymbol = (INamedTypeSymbol)typeGroup.Key!;

                var typeDisplay = typeSymbol.GloballyQualified();

                var testHooks = typeGroup.Where(h => h.HookType == "Test").ToList();
                var classHooks = typeGroup.Where(h => h.HookType == "Class").ToList();

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

                }

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

            // Handle global "Every" hooks for tests
            var globalBeforeEveryTestHooks = hooks.Where(h => h.HookType == "Test" && h.HookKind == "BeforeEvery").ToList();
            if (globalBeforeEveryTestHooks.Any())
            {
                GenerateGlobalHookListPopulation(writer, "BeforeEveryTestHooks", globalBeforeEveryTestHooks);
            }

            var globalAfterEveryTestHooks = hooks.Where(h => h.HookType == "Test" && h.HookKind == "AfterEvery").ToList();
            if (globalAfterEveryTestHooks.Any())
            {
                GenerateGlobalHookListPopulation(writer, "AfterEveryTestHooks", globalAfterEveryTestHooks);
            }

            // Handle global "Every" hooks for classes
            var globalBeforeEveryClassHooks = hooks.Where(h => h.HookType == "Class" && h.HookKind == "BeforeEvery").ToList();
            if (globalBeforeEveryClassHooks.Any())
            {
                GenerateGlobalHookListPopulation(writer, "BeforeEveryClassHooks", globalBeforeEveryClassHooks);
            }

            var globalAfterEveryClassHooks = hooks.Where(h => h.HookType == "Class" && h.HookKind == "AfterEvery").ToList();
            if (globalAfterEveryClassHooks.Any())
            {
                GenerateGlobalHookListPopulation(writer, "AfterEveryClassHooks", globalAfterEveryClassHooks);
            }

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

            // Handle global "Every" hooks for assemblies
            var globalBeforeEveryAssemblyHooks = hooks.Where(h => h.HookType == "Assembly" && h.HookKind == "BeforeEvery").ToList();
            if (globalBeforeEveryAssemblyHooks.Any())
            {
                GenerateGlobalHookListPopulation(writer, "BeforeEveryAssemblyHooks", globalBeforeEveryAssemblyHooks);
            }

            var globalAfterEveryAssemblyHooks = hooks.Where(h => h.HookType == "Assembly" && h.HookKind == "AfterEvery").ToList();
            if (globalAfterEveryAssemblyHooks.Any())
            {
                GenerateGlobalHookListPopulation(writer, "AfterEveryAssemblyHooks", globalAfterEveryAssemblyHooks);
            }

            var testSessionHooks = hooks.Where(h => h.HookType == "TestSession").ToList();
            if (testSessionHooks.Any())
            {

                var beforeTestSessionHooks = testSessionHooks.Where(h => h.HookKind is "Before" or "BeforeEvery").ToList();
                if (beforeTestSessionHooks.Any())
                {
                    GenerateGlobalHookListPopulation(writer, "BeforeTestSessionHooks", beforeTestSessionHooks);
                }

                var afterTestSessionHooks = testSessionHooks.Where(h => h.HookKind is "After" or "AfterEvery").ToList();
                if (afterTestSessionHooks.Any())
                {
                    GenerateGlobalHookListPopulation(writer, "AfterTestSessionHooks", afterTestSessionHooks);
                }
            }

            var testDiscoveryHooks = hooks.Where(h => h.HookType == "TestDiscovery").ToList();
            if (testDiscoveryHooks.Any())
            {

                var beforeTestDiscoveryHooks = testDiscoveryHooks.Where(h => h.HookKind is "Before" or "BeforeEvery").ToList();
                if (beforeTestDiscoveryHooks.Any())
                {
                    GenerateGlobalHookListPopulation(writer, "BeforeTestDiscoveryHooks", beforeTestDiscoveryHooks);
                }

                var afterTestDiscoveryHooks = testDiscoveryHooks.Where(h => h.HookKind is "After" or "AfterEvery").ToList();
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

        var className = hook.TypeSymbol.GloballyQualified();
        var methodName = hook.MethodSymbol.Name;
        var isStatic = hook.MethodSymbol.IsStatic;

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
            hasContextAndCancellationToken = true;
        }

        writer.AppendLine();

        var contextType = GetContextTypeForBody(hook.HookType, hook.HookKind);

        var isInstanceHook = hook.HookKind is "Before" or "After" && hook.HookType == "Test";

        if (isInstanceHook)
        {
            using (writer.BeginBlock($"private static async ValueTask {delegateKey}_Body(object instance, {contextType} context, CancellationToken cancellationToken)"))
            {
                var isOpenGeneric = hook.TypeSymbol.IsGenericType && hook.TypeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);

                if (isOpenGeneric)
                {
                    // Use reflection instead of dynamic to avoid AOT issues
                    writer.AppendLine("var instanceType = instance.GetType();");
                    writer.AppendLine($"var method = instanceType.GetMethod(\"{methodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance{(isStatic ? " | System.Reflection.BindingFlags.Static" : "")});");
                    writer.AppendLine("if (method != null)");
                    writer.AppendLine("{");
                    writer.Indent();

                    writer.AppendLine("object?[] methodArgs;");
                    if (hasCancellationTokenOnly)
                    {
                        writer.AppendLine("methodArgs = new object?[] { cancellationToken };");
                    }
                    else if (hasContextOnly)
                    {
                        writer.AppendLine("methodArgs = new object?[] { context };");
                    }
                    else if (hasContextAndCancellationToken)
                    {
                        writer.AppendLine("methodArgs = new object?[] { context, cancellationToken };");
                    }
                    else
                    {
                        writer.AppendLine("methodArgs = System.Array.Empty<object>();");
                    }

                    writer.AppendLine($"var result = method.Invoke({(isStatic ? "null" : "instance")}, methodArgs);");
                    
                    if (!hook.MethodSymbol.ReturnsVoid)
                    {
                        writer.AppendLine("if (result != null)");
                        writer.AppendLine("{");
                        writer.Indent();
                        writer.AppendLine("await AsyncConvert.ConvertObject(() => result);");
                        writer.Unindent();
                        writer.AppendLine("}");
                    }

                    writer.Unindent();
                    writer.AppendLine("}");
                }
                else
                {
                    writer.AppendLine($"var typedInstance = ({className})instance;");

                    var methodCall = isStatic
                        ? $"{className}.{methodName}"
                        : $"typedInstance.{methodName}";

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
            }
        }
        else
        {
            using (writer.BeginBlock($"private static async ValueTask {delegateKey}_Body({contextType} context, CancellationToken cancellationToken)"))
            {
                var isOpenGeneric = hook.TypeSymbol.IsGenericType && hook.TypeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);

                if (isOpenGeneric && isStatic)
                {
                    writer.AppendLine($"var method = typeof({hook.TypeSymbol.GloballyQualifiedNonGeneric()}).GetMethod(\"{methodName}\", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);");

                    if (hasCancellationTokenOnly)
                    {
                        writer.AppendLine("var parameters = new object[] { cancellationToken };");
                    }
                    else if (hasContextOnly)
                    {
                        writer.AppendLine("var parameters = new object[] { context };");
                    }
                    else if (hasContextAndCancellationToken)
                    {
                        writer.AppendLine("var parameters = new object[] { context, cancellationToken };");
                    }
                    else
                    {
                        writer.AppendLine("var parameters = new object[0];");
                    }

                    writer.AppendLine(hook.MethodSymbol.ReturnsVoid
                        ? "method!.Invoke(null, parameters);"
                        : "await AsyncConvert.ConvertObject(() => method!.Invoke(null, parameters));");
                }
                else
                {
                    var methodCall = $"{className}.{methodName}";

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
            }
        }

        writer.AppendLine();
    }

    private static void GenerateHookListPopulation(CodeWriter writer, string dictionaryName, string typeDisplay, List<HookMethodMetadata> hooks, bool isInstance)
    {
        var hookType = GetConcreteHookType(dictionaryName, isInstance);
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}.GetOrAdd(typeof({typeDisplay}), _ => new global::System.Collections.Concurrent.ConcurrentBag<global::TUnit.Core.Hooks.{hookType}>());");

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
        var hookType = GetConcreteHookType(dictionaryName, false);
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}.GetOrAdd({assemblyVar}, _ => new global::System.Collections.Concurrent.ConcurrentBag<global::TUnit.Core.Hooks.{hookType}>());");

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
        writer.AppendLine($"HookExecutor = {HookExecutorHelper.GetHookExecutor(hook.HookExecutor)},");
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

    private static string GetDelegateKey(HookMethodMetadata hook)
    {
        var declaringType = hook.TypeSymbol;

        var fullTypeName = declaringType.GloballyQualified();

        if (declaringType.IsGenericType)
        {
            var genericIndex = fullTypeName.IndexOf('<');
            if (genericIndex > 0)
            {
                fullTypeName = fullTypeName.Substring(0, genericIndex);
            }
        }

        var safeClassName = fullTypeName
            .Replace(".", "_")
            .Replace("::", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "")
            .Replace("`", "_")
            .Replace("+", "_");

        var paramCount = hook.MethodSymbol.Parameters.Length;
        return $"{safeClassName}_{hook.MethodSymbol.Name}_{paramCount}Params";
    }

    private static string GetHookClass(string hookType, string hookKind, bool isStatic)
    {
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
            "TestDiscovery" => "BeforeTestDiscoveryContext",
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
}

public class HookMethodMetadata
{
    public required IMethodSymbol MethodSymbol { get; init; }
    public required INamedTypeSymbol TypeSymbol { get; init; }
    public required string FilePath { get; init; }
    public required int LineNumber { get; init; }
    public required string HookKind { get; init; }
    public required string HookType { get; init; }
    public required int Order { get; init; }
    public required GeneratorAttributeSyntaxContext Context { get; init; }
    public string? HookExecutor { get; init; }
}
