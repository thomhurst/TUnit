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
        sourceBuilder.WriteLine($"""MethodInfo = {SourceInformationWriter.GenerateMethodInformation(model.Context, model.Method)},""");

        if (model.IsVoid)
        {
            sourceBuilder.WriteLine(
                $"Body = (context, cancellationToken) => {model.FullyQualifiedTypeName}.{model.MethodName}({GetArgs(model, model.HookLocationType)}),"); 
        }
        else
        {
            sourceBuilder.WriteLine(
                $"AsyncBody = (context, cancellationToken) => AsyncConvert.Convert(() => {model.FullyQualifiedTypeName}.{model.MethodName}({GetArgs(model, model.HookLocationType)})),");
        }

        sourceBuilder.WriteLine($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
        sourceBuilder.WriteLine($"Order = {model.Order},");
        sourceBuilder.WriteLine($"""FilePath = @"{model.FilePath}",""");
        sourceBuilder.WriteLine($"LineNumber = {model.LineNumber},");
        sourceBuilder.WriteLine(
            $"MethodAttributes = [ {AttributeWriter.WriteAttributes(model.Context, model.Method.GetAttributes().ExcludingSystemAttributes()).ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine(
            $"ClassAttributes = [ {AttributeWriter.WriteAttributes(model.Context, model.Method.ContainingType.GetAttributesIncludingBaseTypes().ExcludingSystemAttributes()).ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine(
            $"AssemblyAttributes = [ {AttributeWriter.WriteAttributes(model.Context, model.Method.ContainingAssembly.GetAttributes().ExcludingSystemAttributes()).ToCommaSeparatedString()} ],");
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