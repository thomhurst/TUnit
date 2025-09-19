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

        // Generate individual files for each hook instead of collecting them
        context.RegisterSourceOutput(beforeHooks, (sourceProductionContext, hook) =>
        {
            if (hook != null)
            {
                GenerateIndividualHookFile(sourceProductionContext, hook);
            }
        });

        context.RegisterSourceOutput(afterHooks, (sourceProductionContext, hook) =>
        {
            if (hook != null)
            {
                GenerateIndividualHookFile(sourceProductionContext, hook);
            }
        });

        context.RegisterSourceOutput(beforeEveryHooks, (sourceProductionContext, hook) =>
        {
            if (hook != null)
            {
                GenerateIndividualHookFile(sourceProductionContext, hook);
            }
        });

        context.RegisterSourceOutput(afterEveryHooks, (sourceProductionContext, hook) =>
        {
            if (hook != null)
            {
                GenerateIndividualHookFile(sourceProductionContext, hook);
            }
        });
    }

    private static void GenerateIndividualHookFile(SourceProductionContext context, HookMethodMetadata hook)
    {
        try
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
        catch (Exception ex)
        {
            var descriptor = new DiagnosticDescriptor(
                "THG001",
                "Hook metadata generation failed",
                "Failed to generate hook metadata for {0}: {1}",
                "TUnit",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);

            var hookName = $"{hook.TypeSymbol.Name}.{hook.MethodSymbol.Name}";
            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, hookName, ex.Message));
        }
    }

    private static string GetSafeFileName(HookMethodMetadata hook)
    {
        var typeName = hook.TypeSymbol.Name;
        var methodName = hook.MethodSymbol.Name;
        
        // Remove generic type parameters from type name for file safety
        if (hook.TypeSymbol.IsGenericType)
        {
            var genericIndex = typeName.IndexOf('`');
            if (genericIndex > 0)
            {
                typeName = typeName.Substring(0, genericIndex);
            }
        }

        var safeTypeName = typeName
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "")
            .Replace("`", "_")
            .Replace("+", "_");

        var safeMethodName = methodName
            .Replace(".", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace(",", "_")
            .Replace(" ", "");

        var guid = System.Guid.NewGuid().ToString("N");
        
        return $"{safeTypeName}_{safeMethodName}_{hook.HookKind}_{hook.HookType}_{guid}";
    }

    private static void GenerateHookRegistration(CodeWriter writer, HookMethodMetadata hook)
    {
        var typeDisplay = hook.TypeSymbol.GloballyQualified();
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
            var assemblyName = hook.TypeSymbol.ContainingAssembly.Name.Replace(".", "_").Replace("-", "_");
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

    private static void GenerateInstanceHookRegistration(CodeWriter writer, string dictionaryName, string typeDisplay, HookMethodMetadata hook)
    {
        var hookType = GetConcreteHookType(dictionaryName, true);
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}.GetOrAdd(typeof({typeDisplay}), _ => new global::System.Collections.Concurrent.ConcurrentBag<global::TUnit.Core.Hooks.{hookType}>());");
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}[typeof({typeDisplay})].Add(");
        writer.Indent();
        GenerateHookObject(writer, hook, true);
        writer.Unindent();
        writer.AppendLine(");");
    }

    private static void GenerateTypeHookRegistration(CodeWriter writer, string dictionaryName, string typeDisplay, HookMethodMetadata hook)
    {
        var hookType = GetConcreteHookType(dictionaryName, false);
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}.GetOrAdd(typeof({typeDisplay}), _ => new global::System.Collections.Concurrent.ConcurrentBag<global::TUnit.Core.Hooks.{hookType}>());");
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}[typeof({typeDisplay})].Add(");
        writer.Indent();
        GenerateHookObject(writer, hook, false);
        writer.Unindent();
        writer.AppendLine(");");
    }

    private static void GenerateAssemblyHookRegistration(CodeWriter writer, string dictionaryName, string assemblyVarName, HookMethodMetadata hook)
    {
        var assemblyVar = assemblyVarName + "_assembly";
        var hookType = GetConcreteHookType(dictionaryName, false);
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}.GetOrAdd({assemblyVar}, _ => new global::System.Collections.Concurrent.ConcurrentBag<global::TUnit.Core.Hooks.{hookType}>());");
        writer.AppendLine($"global::TUnit.Core.Sources.{dictionaryName}[{assemblyVar}].Add(");
        writer.Indent();
        GenerateHookObject(writer, hook, false);
        writer.Unindent();
        writer.AppendLine(");");
    }

    private static void GenerateGlobalHookRegistration(CodeWriter writer, string listName, HookMethodMetadata hook)
    {
        writer.AppendLine($"global::TUnit.Core.Sources.{listName}.Add(");
        writer.Indent();
        GenerateHookObject(writer, hook, false);
        writer.Unindent();
        writer.AppendLine(");");
    }


    private static HookMethodMetadata? GetHookMethodMetadata(GeneratorAttributeSyntaxContext context, string hookKind)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            return null;
        }

        var typeSymbol = methodSymbol.ContainingType;
        if (typeSymbol is not { })
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
                                 a.AttributeClass is { IsGenericType: true, ConstructedFrom.Name: "HookExecutorAttribute" });

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
                    writer.AppendLine($"var method = instanceType.GetMethod(\"{methodName}\", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.NonPublic | global::System.Reflection.BindingFlags.Instance{(isStatic ? " | global::System.Reflection.BindingFlags.Static" : "")});");
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
                        writer.AppendLine("await AsyncConvert.ConvertObject(result);");
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
                    // For open generic types, we need to find the closed generic base type that matches
                    // the open generic definition where the hook was defined
                    writer.AppendLine($"var openGenericType = typeof({hook.TypeSymbol.GloballyQualified()});");
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
                    writer.AppendLine($"method = targetType.GetMethod(\"{methodName}\", global::System.Reflection.BindingFlags.Public | global::System.Reflection.BindingFlags.Static | global::System.Reflection.BindingFlags.DeclaredOnly);");
                    writer.Unindent();
                    writer.AppendLine("}");
                    writer.AppendLine("targetType = targetType.BaseType;");
                    writer.Unindent();
                    writer.AppendLine("}");
                    writer.AppendLine();
                    writer.AppendLine("if (method == null)");
                    writer.AppendLine("{");
                    writer.Indent();
                    writer.AppendLine($"throw new global::System.InvalidOperationException($\"Could not find static method '{methodName}' on type {{context.ClassType.FullName}} or its base types matching generic definition {{openGenericType.FullName}}\");");
                    writer.Unindent();
                    writer.AppendLine("}");

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
                        ? "method.Invoke(null, parameters);"
                        : "await AsyncConvert.ConvertObject(() => method.Invoke(null, parameters));");
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


    private static void GenerateHookObject(CodeWriter writer, HookMethodMetadata hook, bool isInstance)
    {
        var hookClass = GetHookClass(hook.HookType, hook.HookKind, !isInstance);
        var delegateKey = GetDelegateKey(hook);

        writer.AppendLine($"new {hookClass}");
        writer.AppendLine("{");
        writer.Indent();

        if (isInstance)
        {
            writer.AppendLine($"InitClassType = typeof({hook.TypeSymbol.GloballyQualified()}),");
        }

        writer.Append("MethodInfo = ");
        SourceInformationWriter.GenerateMethodInformation(writer, hook.Context.SemanticModel.Compilation, hook.TypeSymbol, hook.MethodSymbol, null, ',');
        writer.AppendLine();
        writer.AppendLine($"HookExecutor = {HookExecutorHelper.GetHookExecutor(hook.HookExecutor)},");
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

    private static string GetHookIndexMethodName(HookMethodMetadata hook)
    {
        var prefix = hook.HookKind == "Before" || hook.HookKind == "BeforeEvery" ? "Before" : "After";
        var suffix = hook.HookKind.Contains("Every") && hook.HookType != "TestSession" && hook.HookType != "TestDiscovery" ? "Every" : "";
        var hookType = hook.HookType;

        return $"{prefix}{suffix}{hookType}HookIndex()";
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
