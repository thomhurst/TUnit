using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public static class GlobalTestHooksWriter
{
    public static void Execute(SourceCodeWriter sourceBuilder, HooksDataModel model)
    { 
        sourceBuilder.WriteLine($"new {GetClassType(model.HookLevel, model.HookLocationType)}");
        sourceBuilder.WriteLine("{");
        
        sourceBuilder.WriteTabs();
        sourceBuilder.Write("MethodInfo = ");
        SourceInformationWriter.GenerateMethodInformation(sourceBuilder, model.Context, model.ClassType, model.Method, null, ',');
        
        sourceBuilder.WriteLine($"Body = (context, cancellationToken) => AsyncConvert.Convert(() => {model.FullyQualifiedTypeName}.{model.MethodName}({GetArgs(model, model.HookLocationType)})),");

        sourceBuilder.WriteLine($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
        sourceBuilder.WriteLine($"Order = {model.Order},");
        sourceBuilder.WriteLine($"""FilePath = @"{model.FilePath}",""");
        sourceBuilder.WriteLine($"LineNumber = {model.LineNumber},");

        sourceBuilder.WriteLine("},");
    }

    private static string GetClassType(string hookType, HookLocationType hookLocationType)
    {
        if (hookLocationType == HookLocationType.Before)
        {
            return hookType switch
            {
                "TUnit.Core.HookType.Test" => "global::TUnit.Core.Hooks.BeforeTestHookMethod",
                "TUnit.Core.HookType.Class" => "global::TUnit.Core.Hooks.BeforeClassHookMethod",
                "TUnit.Core.HookType.Assembly" => "global::TUnit.Core.Hooks.BeforeAssemblyHookMethod",
                "TUnit.Core.HookType.TestSession" => "global::TUnit.Core.Hooks.BeforeTestSessionHookMethod",
                "TUnit.Core.HookType.TestDiscovery" => "global::TUnit.Core.Hooks.BeforeTestDiscoveryHookMethod",
                _ => throw new ArgumentOutOfRangeException(nameof(hookType), hookType, null)
            };
        }
        
        return hookType switch
        {
            "TUnit.Core.HookType.Test" => "global::TUnit.Core.Hooks.AfterTestHookMethod",
            "TUnit.Core.HookType.Class" => "global::TUnit.Core.Hooks.AfterClassHookMethod",
            "TUnit.Core.HookType.Assembly" => "global::TUnit.Core.Hooks.AfterAssemblyHookMethod",
            "TUnit.Core.HookType.TestSession" => "global::TUnit.Core.Hooks.AfterTestSessionHookMethod",
            "TUnit.Core.HookType.TestDiscovery" => "global::TUnit.Core.Hooks.AfterTestDiscoveryHookMethod",
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