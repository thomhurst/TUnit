﻿using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators;

[Generator]
internal class TestHooksGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var setUpMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.BeforeAttribute",
                predicate: static (_, _) => true,
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx, HookLocationType.Before, false))
            .Where(static m => m is not null)
            .SelectMany((x, _) => x)
            .Collect();

        var cleanUpMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.AfterAttribute",
                predicate: static (_, _) => true,
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx, HookLocationType.After, false))
            .Where(static m => m is not null)
            .SelectMany((x, _) => x)
            .Collect();
        
        var beforeEveryMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.BeforeEveryAttribute",
                predicate: static (s, _) => true,
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx, HookLocationType.Before, true))
            .Where(static m => m is not null)
            .Collect();
        
        var afterEveryMethods = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                "TUnit.Core.AfterEveryAttribute",
                predicate: static (s, _) => true,
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx, HookLocationType.After, true))
            .Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(
            setUpMethods
                .Combine(beforeEveryMethods)
                .Combine(cleanUpMethods)
                .Combine(afterEveryMethods)
                .SelectMany((x, _) =>
                {
                    IEnumerable<HooksDataModel> model =
                    [
                        ..x.Left.Left.Left,
                        ..x.Left.Left.Right.SelectMany(h => h),
                        ..x.Left.Right,
                        ..x.Right.SelectMany(h => h)
                    ];

                    return model;
                })
                .Collect(), 
            Generate);
    }

    static IEnumerable<HooksDataModel> GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context,
        HookLocationType hookLocationType, bool isEveryHook)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            yield break;
        }

        foreach (var contextAttribute in context.Attributes)
        {
            var hookLevel = contextAttribute.ConstructorArguments[0].ToCSharpString();

            yield return new HooksDataModel
            {
                MethodName = methodSymbol.Name,
                HookLocationType = hookLocationType,
                IsEveryHook = isEveryHook && hookLevel is not "TUnit.Core.HookType.TestDiscovery" and not "TUnit.Core.HookType.TestSession",
                HookLevel = hookLevel,
                FullyQualifiedTypeName =
                    methodSymbol.ContainingType.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                MinimalTypeName = methodSymbol.ContainingType.Name,
                ParameterTypes = methodSymbol.Parameters
                    .Select(x => x.Type.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix))
                    .ToArray(),
                HasTimeoutAttribute = methodSymbol.HasTimeoutAttribute(),
                HookExecutor = methodSymbol.GetAttributes().FirstOrDefault(x => x.AttributeClass?.IsOrInherits("global::TUnit.Core.Executors.HookExecutorAttribute") == true)?.AttributeClass?.TypeArguments.FirstOrDefault()?.ToDisplayString(DisplayFormats.FullyQualifiedGenericWithGlobalPrefix),
                Order = contextAttribute.NamedArguments.FirstOrDefault(x => x.Key == "Order").Value.Value as int? ?? 0,
                FilePath = contextAttribute.ConstructorArguments[1].Value?.ToString() ?? string.Empty,
                LineNumber = contextAttribute.ConstructorArguments[2].Value as int? ?? 0,
            };
        }
    }

    private void Generate(SourceProductionContext productionContext,
        ImmutableArray<HooksDataModel> hooks)
    {
        foreach (var hooksGroupedByLevel in hooks.GroupBy(x => x.HookLevel))
        {
            var interfaceType = GetInterfaceType(hooksGroupedByLevel);

            foreach (var groupedByTypeName in hooksGroupedByLevel.GroupBy(x => x.FullyQualifiedTypeName))
            {
                var className =
                    $"{hooksGroupedByLevel.Key.Split('.').Last()}Hooks_{groupedByTypeName.Key
                        .Replace("global::", string.Empty)
                        .Replace('.', '_')}";
                
                using var sourceBuilder = new SourceCodeWriter();

                sourceBuilder.WriteLine("// <auto-generated/>");
                sourceBuilder.WriteLine("#pragma warning disable");
                sourceBuilder.WriteLine("using global::System.Linq;");
                sourceBuilder.WriteLine("using global::System.Reflection;");
                sourceBuilder.WriteLine("using global::System.Runtime.CompilerServices;");
                sourceBuilder.WriteLine("using global::TUnit.Core;");
                sourceBuilder.WriteLine("using global::TUnit.Core.Interfaces;");
                sourceBuilder.WriteLine();
                sourceBuilder.WriteLine("namespace TUnit.SourceGenerated;");
                sourceBuilder.WriteLine();
                sourceBuilder.WriteLine("[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]");
                sourceBuilder.WriteLine($"file partial class {className} : {interfaceType}");
                sourceBuilder.WriteLine("{");
                sourceBuilder.WriteLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
                sourceBuilder.WriteLine("public static void Initialise()");
                sourceBuilder.WriteLine("{");
                sourceBuilder.WriteLine($"SourceRegistrar.Register(new {className}());");
                sourceBuilder.WriteLine("}");

                sourceBuilder.WriteLine($"public IReadOnlyList<{GetReturnType(hooksGroupedByLevel, HookLocationType.Before, false)}> CollectBeforeHooks()");
                sourceBuilder.WriteLine("{");
                sourceBuilder.WriteLine("return");
                sourceBuilder.WriteLine("[");
                
                foreach (var model in groupedByTypeName.Where(x => x.HookLocationType == HookLocationType.Before && !x.IsEveryHook))
                {
                    if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Test")
                    {
                        TestHooksWriter.Execute(sourceBuilder, model);
                    }
                    else if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Class")
                    {
                        ClassHooksWriter.Execute(sourceBuilder, model);
                    }
                    else if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Assembly")
                    {
                        AssemblyHooksWriter.Execute(sourceBuilder, model);
                    }
                    else if (hooksGroupedByLevel.Key is "TUnit.Core.HookType.TestDiscovery"
                             or "TUnit.Core.HookType.TestSession")
                    {
                        GlobalTestHooksWriter.Execute(sourceBuilder, model, model.HookLocationType);
                    }
                }

                sourceBuilder.WriteLine("];");
                sourceBuilder.WriteLine("}");
                sourceBuilder.WriteLine();
                
                sourceBuilder.WriteLine($"public IReadOnlyList<{GetReturnType(hooksGroupedByLevel, HookLocationType.After, false)}> CollectAfterHooks()");
                sourceBuilder.WriteLine("{");
                sourceBuilder.WriteLine("return");
                sourceBuilder.WriteLine("[");
                
                foreach (var model in groupedByTypeName.Where(x => x.HookLocationType == HookLocationType.After && !x.IsEveryHook))
                {
                    if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Test")
                    {
                        TestHooksWriter.Execute(sourceBuilder, model);
                    }
                    else if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Class")
                    {
                        ClassHooksWriter.Execute(sourceBuilder, model);
                    }
                    else if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Assembly")
                    {
                        AssemblyHooksWriter.Execute(sourceBuilder, model);
                    }
                    else if (hooksGroupedByLevel.Key is "TUnit.Core.HookType.TestDiscovery"
                             or "TUnit.Core.HookType.TestSession")
                    {
                        GlobalTestHooksWriter.Execute(sourceBuilder, model, model.HookLocationType);
                    }
                }

                sourceBuilder.WriteLine("];");
                sourceBuilder.WriteLine("}");
                
                sourceBuilder.WriteLine($"public IReadOnlyList<{GetReturnType(hooksGroupedByLevel, HookLocationType.Before, true)}> CollectBeforeEveryHooks()");
                sourceBuilder.WriteLine("{");
                sourceBuilder.WriteLine("return");
                sourceBuilder.WriteLine("[");
                
                foreach (var model in groupedByTypeName.Where(x => x.HookLocationType == HookLocationType.Before && x.IsEveryHook))
                {
                    if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Test")
                    {
                        TestHooksWriter.Execute(sourceBuilder, model);
                    }
                    else if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Class")
                    {
                        ClassHooksWriter.Execute(sourceBuilder, model);
                    }
                    else if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Assembly")
                    {
                        AssemblyHooksWriter.Execute(sourceBuilder, model);
                    }
                }

                sourceBuilder.WriteLine("];");
                sourceBuilder.WriteLine("}");
                sourceBuilder.WriteLine();
                
                sourceBuilder.WriteLine($"public IReadOnlyList<{GetReturnType(hooksGroupedByLevel, HookLocationType.After, true)}> CollectAfterEveryHooks()");
                sourceBuilder.WriteLine("{");
                sourceBuilder.WriteLine("return");
                sourceBuilder.WriteLine("[");
                
                foreach (var model in groupedByTypeName.Where(x => x.HookLocationType == HookLocationType.After && x.IsEveryHook))
                {
                    if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Test")
                    {
                        TestHooksWriter.Execute(sourceBuilder, model);
                    }
                    else if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Class")
                    {
                        ClassHooksWriter.Execute(sourceBuilder, model);
                    }
                    else if (hooksGroupedByLevel.Key == "TUnit.Core.HookType.Assembly")
                    {
                        AssemblyHooksWriter.Execute(sourceBuilder, model);
                    }
                }

                sourceBuilder.WriteLine("];");
                sourceBuilder.WriteLine("}");                
                
                sourceBuilder.WriteLine("}");
                
                productionContext.AddSource($"{className}.Generated.cs", sourceBuilder.ToString());
            }
        }
    }

    private static string GetInterfaceType(IGrouping<string, HooksDataModel> hooksGroupedByLevel)
    {
        return hooksGroupedByLevel.Key switch
        {
            "TUnit.Core.HookType.TestDiscovery" => "TUnit.Core.Interfaces.SourceGenerator.ITestDiscoveryHookSource",
            "TUnit.Core.HookType.TestSession" => "TUnit.Core.Interfaces.SourceGenerator.ITestSessionHookSource", 
            "TUnit.Core.HookType.Assembly" => "TUnit.Core.Interfaces.SourceGenerator.IAssemblyHookSource", 
            "TUnit.Core.HookType.Class" => "TUnit.Core.Interfaces.SourceGenerator.IClassHookSource", 
            "TUnit.Core.HookType.Test" => "TUnit.Core.Interfaces.SourceGenerator.ITestHookSource", 
            _ => throw new ArgumentOutOfRangeException()
        };
    }
    
    private static string GetReturnType(IGrouping<string, HooksDataModel> hooksGroupedByLevel, HookLocationType hookLocationType, bool isEvery)
    {
        return hooksGroupedByLevel.Key switch
        {
            "TUnit.Core.HookType.TestDiscovery" 
                when hookLocationType == HookLocationType.Before => "StaticHookMethod<BeforeTestDiscoveryContext>",
            "TUnit.Core.HookType.TestDiscovery" => "StaticHookMethod<TestDiscoveryContext>",
            "TUnit.Core.HookType.TestSession" => "StaticHookMethod<TestSessionContext>",
            "TUnit.Core.HookType.Assembly" => "StaticHookMethod<AssemblyHookContext>",
            "TUnit.Core.HookType.Class" => "StaticHookMethod<ClassHookContext>",
            "TUnit.Core.HookType.Test" when isEvery => "StaticHookMethod<TestContext>",
            "TUnit.Core.HookType.Test" => "InstanceHookMethod",
            _ => throw new ArgumentOutOfRangeException()
        };
    }
}