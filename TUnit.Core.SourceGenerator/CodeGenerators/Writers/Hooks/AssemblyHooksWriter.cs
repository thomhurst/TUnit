using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public static class AssemblyHooksWriter
{
    public static void Execute(SourceCodeWriter sourceBuilder, HooksDataModel? model)
    {
        if (model is null)
        {
            return;
        }
        
        if (model.HookLocationType == HookLocationType.Before)
        {
            sourceBuilder.Write("new global::TUnit.Core.Hooks.BeforeAssemblyHookMethod");
        }
        else
        {
            sourceBuilder.Write("new global::TUnit.Core.Hooks.AfterAssemblyHookMethod");
        }

        sourceBuilder.Write("{ ");
        sourceBuilder.WriteTabs();
        sourceBuilder.Write("MethodInfo = ");
        SourceInformationWriter.GenerateMethodInformation(sourceBuilder, model.Context, model.ClassType, model.Method, null, ',');
        
        sourceBuilder.Write($"Body = (context, cancellationToken) => AsyncConvert.Convert(() => {model.FullyQualifiedTypeName}.{model.MethodName}({GetArgs(model)})),");
        
        sourceBuilder.Write($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
        sourceBuilder.Write($"Order = {model.Order},");
        sourceBuilder.Write($"""FilePath = @"{model.FilePath}",""");
        sourceBuilder.Write($"LineNumber = {model.LineNumber},");
        
        sourceBuilder.Write("},");
    }
    
    private static string GetArgs(HooksDataModel model)
    {
        List<string> args = [];
        
        foreach (var type in model.ParameterTypes)
        {
            if (type == WellKnownFullyQualifiedClassNames.AssemblyHookContext.WithGlobalPrefix)
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