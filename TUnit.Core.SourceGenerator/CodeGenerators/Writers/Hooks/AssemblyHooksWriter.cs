using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public static class AssemblyHooksWriter
{
    public static void Execute(ICodeWriter sourceBuilder, HooksDataModel? model)
    {
        if (model is null)
        {
            return;
        }

        if (model.HookLocationType == HookLocationType.Before)
        {
            sourceBuilder.Append("new global::TUnit.Core.Hooks.BeforeAssemblyHookMethod");
        }
        else
        {
            sourceBuilder.Append("new global::TUnit.Core.Hooks.AfterAssemblyHookMethod");
        }

        sourceBuilder.Append("{");
        sourceBuilder.Append("MethodInfo = ");
        SourceInformationWriter.GenerateMethodInformation(sourceBuilder, model.Context.SemanticModel.Compilation, model.ClassType, model.Method, null, ',');

        sourceBuilder.Append($"Body = (context, cancellationToken) => AsyncConvert.Convert(() => {model.FullyQualifiedTypeName}.{model.MethodName}({GetArgs(model)})),");

        sourceBuilder.Append($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
        sourceBuilder.Append($"Order = {model.Order},");
        sourceBuilder.Append($"""FilePath = @"{model.FilePath}",""");
        sourceBuilder.Append($"LineNumber = {model.LineNumber},");

        sourceBuilder.Append("},");
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
