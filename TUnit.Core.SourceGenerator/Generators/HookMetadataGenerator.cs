using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;
using TUnit.Core.SourceGenerator.Models.Extracted;
using TUnit.Core.SourceGenerator.Utilities;

namespace TUnit.Core.SourceGenerator.Generators;

/// <summary>
/// Refactored HookMetadataGenerator that uses primitive-only models for proper incremental caching.
/// All symbol access happens in the transform step; only primitives are stored in the model.
/// </summary>
[Generator]
public class HookMetadataGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var enabledProvider = context.AnalyzerConfigOptionsProvider
            .Select((options, _) =>
            {
                options.GlobalOptions.TryGetValue("build_property.EnableTUnitSourceGeneration", out var value);
                return !string.Equals(value, "false", StringComparison.OrdinalIgnoreCase);
            });

        var beforeHooks = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.BeforeAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => ExtractHookModel(ctx, "Before"))
            .Where(static m => m is not null)
            .Combine(enabledProvider);

        var afterHooks = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.AfterAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => ExtractHookModel(ctx, "After"))
            .Where(static m => m is not null)
            .Combine(enabledProvider);

        var beforeEveryHooks = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.BeforeEveryAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => ExtractHookModel(ctx, "BeforeEvery"))
            .Where(static m => m is not null)
            .Combine(enabledProvider);

        var afterEveryHooks = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.AfterEveryAttribute",
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => ExtractHookModel(ctx, "AfterEvery"))
            .Where(static m => m is not null)
            .Combine(enabledProvider);

        context.RegisterSourceOutput(beforeHooks, GenerateHookFile);
        context.RegisterSourceOutput(afterHooks, GenerateHookFile);
        context.RegisterSourceOutput(beforeEveryHooks, GenerateHookFile);
        context.RegisterSourceOutput(afterEveryHooks, GenerateHookFile);
    }

    private static void GenerateHookFile(SourceProductionContext context, (HookModel? Hook, bool IsEnabled) data)
    {
        var (hook, isEnabled) = data;
        if (!isEnabled || hook == null)
        {
            return;
        }

        try
        {
            GenerateIndividualHookFile(context, hook);
        }
        catch (Exception ex)
        {
            var descriptor = new DiagnosticDescriptor(
                "THG001",
                "Hook metadata generation failed",
                "Failed to generate hook metadata for {0}: {1}",
                "TUnit",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            var hookName = $"{hook.MinimalTypeName}.{hook.MethodName}";
            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, hookName, ex.Message));
        }
    }

    /// <summary>
    /// Extracts all needed data as primitives in the transform step.
    /// This enables proper incremental caching.
    /// </summary>
    private static HookModel? ExtractHookModel(GeneratorAttributeSyntaxContext context, string hookKind)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var typeSymbol = methodSymbol.ContainingType;
        if (typeSymbol is null)
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
        var filePath = location.SourceTree?.FilePath ?? hookAttribute.ConstructorArguments.ElementAtOrDefault(1).Value?.ToString() ?? "";
        var lineNumber = location.GetLineSpan().StartLinePosition.Line + 1;

        var order = GetHookOrder(hookAttribute);
        var hookExecutor = GetHookExecutorType(methodSymbol);

        // Determine parameter patterns
        var paramCount = methodSymbol.Parameters.Length;
        var hasCancellationTokenOnly = false;
        var hasContextOnly = false;
        var hasContextAndCancellationToken = false;
        string? firstParameterTypeName = null;

        if (paramCount >= 1)
        {
            var firstParam = methodSymbol.Parameters[0];
            var firstParamType = firstParam.Type;
            firstParameterTypeName = firstParamType.Name;

            if (firstParamType.Name == "CancellationToken" && firstParamType.ContainingNamespace?.ToString() == "System.Threading")
            {
                hasCancellationTokenOnly = paramCount == 1;
            }
            else
            {
                hasContextOnly = paramCount == 1;
            }

            if (paramCount == 2)
            {
                hasContextAndCancellationToken = true;
            }
        }

        // Check for open generic type
        var isOpenGeneric = typeSymbol.IsGenericType && typeSymbol.TypeArguments.Any(t => t.TypeKind == TypeKind.TypeParameter);

        // Pre-generate method info expression
        var methodInfoExpression = GenerateMethodInfoExpression(context.SemanticModel.Compilation, typeSymbol, methodSymbol);

        // Extract parameters using the existing static method
        var parameters = ParameterModel.ExtractAll(methodSymbol);

        // Extract type parameters
        var typeParams = typeSymbol.TypeParameters.Select(t => t.Name).ToArray();

        return new HookModel
        {
            FullyQualifiedTypeName = typeSymbol.GloballyQualified(),
            MinimalTypeName = typeSymbol.Name,
            Namespace = typeSymbol.ContainingNamespace?.ToDisplayString() ?? string.Empty,
            AssemblyName = typeSymbol.ContainingAssembly.Name,
            MethodName = methodSymbol.Name,
            FilePath = filePath,
            LineNumber = lineNumber,
            HookKind = hookKind,
            HookType = hookType,
            Order = order,
            HookExecutorTypeName = hookExecutor,
            IsStatic = methodSymbol.IsStatic,
            IsAsync = methodSymbol.IsAsync,
            ReturnsVoid = methodSymbol.ReturnsVoid,
            ReturnType = methodSymbol.ReturnType.GloballyQualified(),
            ParameterCount = paramCount,
            HasCancellationTokenOnly = hasCancellationTokenOnly,
            HasContextOnly = hasContextOnly,
            HasContextAndCancellationToken = hasContextAndCancellationToken,
            FirstParameterTypeName = firstParameterTypeName,
            Parameters = new EquatableArray<ParameterModel>(parameters),
            ClassIsGenericType = typeSymbol.IsGenericType,
            ClassIsOpenGeneric = isOpenGeneric,
            ClassTypeParameters = new EquatableArray<string>(typeParams),
            MethodInfoExpression = methodInfoExpression,
            HookAttribute = ExtractedAttribute.Extract(hookAttribute),
            MethodAttributes = new EquatableArray<ExtractedAttribute>(
                methodSymbol.GetAttributes().Select(a => ExtractedAttribute.Extract(a)).ToArray())
        };
    }

    private static string GenerateMethodInfoExpression(Compilation compilation, INamedTypeSymbol typeSymbol, IMethodSymbol methodSymbol)
    {
        // Generate the MethodMetadata expression as a string
        using var writer = new CodeWriter();
        MetadataGenerationHelper.WriteMethodMetadata(writer, methodSymbol, typeSymbol);
        return writer.ToString();
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
                                 a.AttributeClass is { IsGenericType: true, ConstructedFrom.Name: "HookExecutorAttribute" });

        if (hookExecutorAttribute == null)
        {
            return null;
        }

        if (hookExecutorAttribute.AttributeClass?.IsGenericType == true)
        {
            var typeArg = hookExecutorAttribute.AttributeClass.TypeArguments.FirstOrDefault();
            return typeArg?.GloballyQualified();
        }

        var typeArgument = hookExecutorAttribute.ConstructorArguments.FirstOrDefault();
        if (typeArgument.Value is ITypeSymbol typeSymbol)
        {
            return typeSymbol.GloballyQualified();
        }

        return null;
    }

    private static void GenerateIndividualHookFile(SourceProductionContext context, HookModel hook)
    {
        var safeFileName = GetSafeFileName(hook);
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

        writer.AppendLine($"namespace TUnit.Generated.Hooks.{safeFileName};");
        writer.AppendLine();

        using (writer.BeginBlock($"internal static class {safeFileName}Initializer"))
        {
            writer.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            using (writer.BeginBlock("public static void Initialize()"))
            {
                GenerateHookRegistration(writer, hook);
            }

            writer.AppendLine();
            GenerateHookDelegate(writer, hook);
        }

        context.AddSource($"{safeFileName}.Hook.g.cs", writer.ToString());
    }

    private static string GetSafeFileName(HookModel hook)
    {
        // Create deterministic filename from full type name and method name
        // Use fully qualified type name to ensure uniqueness across namespaces
        var safeName = $"{hook.FullyQualifiedTypeName}_{hook.MethodName}_{hook.ParameterCount}_{hook.HookKind}_{hook.HookType}"
            .Replace("global::", "")
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "")
            .Replace("`", "_")
            .Replace("::", "_");
        return safeName;
    }

    private static void GenerateHookRegistration(CodeWriter writer, HookModel hook)
    {
        var typeDisplay = hook.FullyQualifiedTypeName;
        var isInstance = hook.HookKind is "Before" or "After" && hook.HookType == "Test";

        if (hook.HookType == "Test")
        {
            if (hook.HookKind == "Before")
            {
                if (isInstance)
                {
                    GenerateInstanceHookRegistration(writer, "BeforeTestHooks", typeDisplay, hook);
                }
                else
                {
                    GenerateGlobalHookRegistration(writer, "BeforeEveryTestHooks", hook);
                }
            }
            else if (hook.HookKind == "After")
            {
                if (isInstance)
                {
                    GenerateInstanceHookRegistration(writer, "AfterTestHooks", typeDisplay, hook);
                }
                else
                {
                    GenerateGlobalHookRegistration(writer, "AfterEveryTestHooks", hook);
                }
            }
            else if (hook.HookKind == "BeforeEvery")
            {
                GenerateGlobalHookRegistration(writer, "BeforeEveryTestHooks", hook);
            }
            else if (hook.HookKind == "AfterEvery")
            {
                GenerateGlobalHookRegistration(writer, "AfterEveryTestHooks", hook);
            }
        }
        else if (hook.HookType == "Class")
        {
            if (hook.HookKind == "Before")
            {
                GenerateTypeHookRegistration(writer, "BeforeClassHooks", typeDisplay, hook);
            }
            else if (hook.HookKind == "After")
            {
                GenerateTypeHookRegistration(writer, "AfterClassHooks", typeDisplay, hook);
            }
            else if (hook.HookKind == "BeforeEvery")
            {
                GenerateGlobalHookRegistration(writer, "BeforeEveryClassHooks", hook);
            }
            else if (hook.HookKind == "AfterEvery")
            {
                GenerateGlobalHookRegistration(writer, "AfterEveryClassHooks", hook);
            }
        }
        else if (hook.HookType == "Assembly")
        {
            var assemblyName = hook.AssemblyName.Replace(".", "_").Replace("-", "_");
            writer.AppendLine($"var {assemblyName}_assembly = typeof({typeDisplay}).Assembly;");

            if (hook.HookKind == "Before")
            {
                GenerateAssemblyHookRegistration(writer, "BeforeAssemblyHooks", assemblyName, hook);
            }
            else if (hook.HookKind == "After")
            {
                GenerateAssemblyHookRegistration(writer, "AfterAssemblyHooks", assemblyName, hook);
            }
            else if (hook.HookKind == "BeforeEvery")
            {
                GenerateGlobalHookRegistration(writer, "BeforeEveryAssemblyHooks", hook);
            }
            else if (hook.HookKind == "AfterEvery")
            {
                GenerateGlobalHookRegistration(writer, "AfterEveryAssemblyHooks", hook);
            }
        }
        else if (hook.HookType == "TestSession")
        {
            if (hook.HookKind is "Before" or "BeforeEvery")
            {
                GenerateGlobalHookRegistration(writer, "BeforeTestSessionHooks", hook);
            }
            else if (hook.HookKind is "After" or "AfterEvery")
            {
                GenerateGlobalHookRegistration(writer, "AfterTestSessionHooks", hook);
            }
        }
        else if (hook.HookType == "TestDiscovery")
        {
            if (hook.HookKind is "Before" or "BeforeEvery")
            {
                GenerateGlobalHookRegistration(writer, "BeforeTestDiscoveryHooks", hook);
            }
            else if (hook.HookKind is "After" or "AfterEvery")
            {
                GenerateGlobalHookRegistration(writer, "AfterTestDiscoveryHooks", hook);
            }
        }
    }

    private static void GenerateInstanceHookRegistration(CodeWriter writer, string dictionaryName, string typeDisplay, HookModel hook)
    {
        var hookType = GetConcreteHookType(dictionaryName, true);
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}.GetOrAdd(typeof({typeDisplay}), static _ => new global::System.Collections.Concurrent.ConcurrentBag<global::TUnit.Core.Hooks.{hookType}>());");
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}[typeof({typeDisplay})].Add(");
        writer.Indent();
        GenerateHookObject(writer, hook, true);
        writer.Unindent();
        writer.AppendLine(");");
    }

    private static void GenerateTypeHookRegistration(CodeWriter writer, string dictionaryName, string typeDisplay, HookModel hook)
    {
        var hookType = GetConcreteHookType(dictionaryName, false);
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}.GetOrAdd(typeof({typeDisplay}), static _ => new global::System.Collections.Concurrent.ConcurrentBag<global::TUnit.Core.Hooks.{hookType}>());");
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}[typeof({typeDisplay})].Add(");
        writer.Indent();
        GenerateHookObject(writer, hook, false);
        writer.Unindent();
        writer.AppendLine(");");
    }

    private static void GenerateAssemblyHookRegistration(CodeWriter writer, string dictionaryName, string assemblyVarName, HookModel hook)
    {
        var assemblyVar = assemblyVarName + "_assembly";
        var hookType = GetConcreteHookType(dictionaryName, false);
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}.GetOrAdd({assemblyVar}, static _ => new global::System.Collections.Concurrent.ConcurrentBag<global::TUnit.Core.Hooks.{hookType}>());");
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}[{assemblyVar}].Add(");
        writer.Indent();
        GenerateHookObject(writer, hook, false);
        writer.Unindent();
        writer.AppendLine(");");
    }

    private static void GenerateGlobalHookRegistration(CodeWriter writer, string listName, HookModel hook)
    {
        writer.AppendLine($"global::TUnit.Core.Sources.{listName}.Add(");
        writer.Indent();
        GenerateHookObject(writer, hook, false);
        writer.Unindent();
        writer.AppendLine(");");
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

    private static void GenerateHookDelegate(CodeWriter writer, HookModel hook)
    {
        var delegateKey = GetDelegateKey(hook);
        var contextType = GetContextTypeForBody(hook.HookType, hook.HookKind);
        var isInstanceHook = hook.HookKind is "Before" or "After" && hook.HookType == "Test";

        if (isInstanceHook)
        {
            using (writer.BeginBlock($"private static async ValueTask {delegateKey}_Body(object instance, {contextType} context, CancellationToken cancellationToken)"))
            {
                if (hook.ClassIsOpenGeneric)
                {
                    GenerateReflectionBasedInvocation(writer, hook, true);
                }
                else
                {
                    GenerateDirectInvocation(writer, hook, true);
                }
            }
        }
        else
        {
            using (writer.BeginBlock($"private static async ValueTask {delegateKey}_Body({contextType} context, CancellationToken cancellationToken)"))
            {
                if (hook.ClassIsOpenGeneric && hook.IsStatic)
                {
                    GenerateOpenGenericStaticInvocation(writer, hook);
                }
                else
                {
                    GenerateStaticMethodInvocation(writer, hook);
                }
            }
        }

        writer.AppendLine();
    }

    private static void GenerateReflectionBasedInvocation(CodeWriter writer, HookModel hook, bool isInstance)
    {
        writer.AppendLine("var instanceType = instance.GetType();");
        writer.AppendLine($"var method = instanceType.GetMethod(\"{hook.MethodName}\", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance{(hook.IsStatic ? " | global::System.Reflection.BindingFlags.Static" : "")});");
        writer.AppendLine("if (method != null)");
        writer.AppendLine("{");
        writer.Indent();

        writer.AppendLine("object?[] methodArgs;");
        if (hook.HasCancellationTokenOnly)
        {
            writer.AppendLine("methodArgs = new object?[] { cancellationToken };");
        }
        else if (hook.HasContextOnly)
        {
            writer.AppendLine("methodArgs = new object?[] { context };");
        }
        else if (hook.HasContextAndCancellationToken)
        {
            writer.AppendLine("methodArgs = new object?[] { context, cancellationToken };");
        }
        else
        {
            writer.AppendLine("methodArgs = System.Array.Empty<object>();");
        }

        writer.AppendLine($"var result = method.Invoke({(hook.IsStatic ? "null" : "instance")}, methodArgs);");

        if (!hook.ReturnsVoid)
        {
            writer.AppendLine("if (result != null)");
            writer.AppendLine("{");
            writer.Indent();
            writer.AppendLine("await AsyncConvert.ConvertObject(result);");
            writer.Unindent();
            writer.AppendLine("}");
        }

        writer.Unindent();
        writer.AppendLine("}");
    }

    private static void GenerateDirectInvocation(CodeWriter writer, HookModel hook, bool isInstance)
    {
        var className = hook.FullyQualifiedTypeName;
        writer.AppendLine($"var typedInstance = ({className})instance;");

        var methodCall = hook.IsStatic
            ? $"{className}.{hook.MethodName}"
            : $"typedInstance.{hook.MethodName}";

        if (hook.HasCancellationTokenOnly)
        {
            methodCall += "(cancellationToken)";
        }
        else if (hook.HasContextOnly)
        {
            methodCall += "(context)";
        }
        else if (hook.HasContextAndCancellationToken)
        {
            methodCall += "(context, cancellationToken)";
        }
        else
        {
            methodCall += "()";
        }

        writer.AppendLine($"await AsyncConvert.Convert(() => {methodCall});");
    }

    private static void GenerateOpenGenericStaticInvocation(CodeWriter writer, HookModel hook)
    {
        writer.AppendLine($"var openGenericType = typeof({hook.FullyQualifiedTypeName});");
        writer.AppendLine("Type? targetType = context.ClassType;");
        writer.AppendLine("MethodInfo? method = null;");
        writer.AppendLine();
        writer.AppendLine("// Walk up the inheritance chain to find the closed generic type that matches the open generic definition");
        writer.AppendLine("while (targetType != null && method == null)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine("if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == openGenericType)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"method = targetType.GetMethod(\"{hook.MethodName}\", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static | global::System.Reflection.BindingFlags.DeclaredOnly);");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine("targetType = targetType.BaseType;");
        writer.Unindent();
        writer.AppendLine("}");
        writer.AppendLine();
        writer.AppendLine("if (method == null)");
        writer.AppendLine("{");
        writer.Indent();
        writer.AppendLine($"throw new global::System.InvalidOperationException($\"Could not find static method '{hook.MethodName}' on type {{context.ClassType.FullName}} or its base types matching generic definition {{openGenericType.FullName}}\");");
        writer.Unindent();
        writer.AppendLine("}");

        if (hook.HasCancellationTokenOnly)
        {
            writer.AppendLine("var parameters = new object[] { cancellationToken };");
        }
        else if (hook.HasContextOnly)
        {
            writer.AppendLine("var parameters = new object[] { context };");
        }
        else if (hook.HasContextAndCancellationToken)
        {
            writer.AppendLine("var parameters = new object[] { context, cancellationToken };");
        }
        else
        {
            writer.AppendLine("var parameters = new object[0];");
        }

        writer.AppendLine(hook.ReturnsVoid
            ? "method.Invoke(null, parameters);"
            : "await AsyncConvert.ConvertObject(() => method.Invoke(null, parameters));");
    }

    private static void GenerateStaticMethodInvocation(CodeWriter writer, HookModel hook)
    {
        var methodCall = $"{hook.FullyQualifiedTypeName}.{hook.MethodName}";

        if (hook.HasCancellationTokenOnly)
        {
            methodCall += "(cancellationToken)";
        }
        else if (hook.HasContextOnly)
        {
            methodCall += "(context)";
        }
        else if (hook.HasContextAndCancellationToken)
        {
            methodCall += "(context, cancellationToken)";
        }
        else
        {
            methodCall += "()";
        }

        writer.AppendLine($"await AsyncConvert.Convert(() => {methodCall});");
    }

    private static void GenerateHookObject(CodeWriter writer, HookModel hook, bool isInstance)
    {
        var hookClass = GetHookClass(hook.HookType, hook.HookKind, !isInstance);
        var delegateKey = GetDelegateKey(hook);

        writer.AppendLine($"new {hookClass}");
        writer.AppendLine("{");
        writer.Indent();

        if (isInstance)
        {
            writer.AppendLine($"InitClassType = typeof({hook.FullyQualifiedTypeName}),");
        }

        // Use pre-generated method info expression
        writer.Append("MethodInfo = ");
        writer.Append(hook.MethodInfoExpression);
        writer.AppendLine(",");

        writer.AppendLine($"HookExecutor = {HookExecutorHelper.GetHookExecutor(hook.HookExecutorTypeName)},");
        writer.AppendLine($"Order = {hook.Order},");
        writer.AppendLine($"RegistrationIndex = global::TUnit.Core.HookRegistrationIndices.GetNext{GetHookIndexMethodName(hook)},");
        writer.AppendLine($"Body = {delegateKey}_Body" + (isInstance ? "" : ","));

        if (!isInstance)
        {
            writer.AppendLine($"FilePath = @\"{hook.FilePath.Replace("\\", "\\\\").Replace("\"", "\\\"")}\",");
            writer.AppendLine($"LineNumber = {hook.LineNumber}");
        }

        writer.Unindent();
        writer.Append("}");
    }

    private static string GetDelegateKey(HookModel hook)
    {
        var fullTypeName = hook.FullyQualifiedTypeName;

        if (hook.ClassIsGenericType)
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

        return $"{safeClassName}_{hook.MethodName}_{hook.ParameterCount}Params";
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

    private static string GetHookIndexMethodName(HookModel hook)
    {
        var prefix = hook.HookKind is "Before" or "BeforeEvery" ? "Before" : "After";
        var suffix = hook.HookKind.Contains("Every") && hook.HookType != "TestSession" && hook.HookType != "TestDiscovery" ? "Every" : "";
        var hookType = hook.HookType;

        return $"{prefix}{suffix}{hookType}HookIndex()";
    }
}
