using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public static class GlobalTestHooksWriter
{
    public static void Execute(SourceCodeWriter sourceBuilder, HooksDataModel model)
    { 
        sourceBuilder.WriteLine(
                $$"""
                   new StaticHookMethod<{{GetClassType(model.HookLevel, model.HookLocationType)}}>
                           { 
                              MethodInfo = typeof({{model.FullyQualifiedTypeName}}).GetMethod("{{model.MethodName}}", 0, [{{string.Join(", ", model.ParameterTypes.Select(x => $"typeof({x})"))}}]),
                              Body = (context, cancellationToken) => AsyncConvert.Convert(() => {{model.FullyQualifiedTypeName}}.{{model.MethodName}}({{GetArgs(model, model.HookLocationType)}})),
                              HookExecutor = {{HookExecutorHelper.GetHookExecutor(model.HookExecutor)}},
                              Order = {{model.Order}},
                              FilePath = @"{{model.FilePath}}",
                              LineNumber = {{model.LineNumber}},
                           },
                   """);
    }

    private static string GetClassType(string hookType, HookLocationType hookLocationType)
    {
        if (hookType == "TUnit.Core.HookType.TestDiscovery" && hookLocationType == HookLocationType.Before)
        {
            return "global::TUnit.Core.BeforeTestDiscoveryContext";
        }
        
        return hookType switch
        {
            "TUnit.Core.HookType.Test" => "global::TUnit.Core.TestContext",
            "TUnit.Core.HookType.Class" => "global::TUnit.Core.ClassHookContext",
            "TUnit.Core.HookType.Assembly" => "global::TUnit.Core.AssemblyHookContext",
            "TUnit.Core.HookType.TestSession" => "global::TUnit.Core.TestSessionContext",
            "TUnit.Core.HookType.TestDiscovery" => "global::TUnit.Core.TestDiscoveryContext",
            _ => throw new ArgumentOutOfRangeException(nameof(hookType), hookType, null)
        };
    }

    private static string GetArgs(HooksDataModel model, HookLocationType hookLocationType)
    {
        List<string> args = [];

        var expectedType = model.HookLevel switch
        {
            "TUnit.Core.HookType.Test" => WellKnownFullyQualifiedClassNames.TestContext,
            "TUnit.Core.HookType.Class" => WellKnownFullyQualifiedClassNames.ClassHookContext,
            "TUnit.Core.HookType.Assembly" => WellKnownFullyQualifiedClassNames.AssemblyHookContext,
            "TUnit.Core.HookType.TestSession" => WellKnownFullyQualifiedClassNames.TestSessionContext,
            "TUnit.Core.HookType.TestDiscovery" when hookLocationType == HookLocationType.Before => WellKnownFullyQualifiedClassNames.BeforeTestDiscoveryContext,
            "TUnit.Core.HookType.TestDiscovery" when hookLocationType == HookLocationType.After => WellKnownFullyQualifiedClassNames.TestDiscoveryContext,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        foreach (var type in model.ParameterTypes)
        {
            if (type == expectedType.WithGlobalPrefix)
            {
                args.Add("context");
            }

            if (type == WellKnownFullyQualifiedClassNames.CancellationToken.WithGlobalPrefix)
            {
                args.Add("cancellationToken");
            }
        }

        return string.Join(", ", args);
    }
}