using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public static class ClassHooksWriter
{
    public static void Execute(ICodeWriter sourceBuilder, HooksDataModel model)
    {
        if (model.HookLocationType == HookLocationType.Before)
        {
            sourceBuilder.Append("new global::TUnit.Core.Hooks.BeforeClassHookMethod");
        }
        else
        {
            sourceBuilder.Append("new global::TUnit.Core.Hooks.AfterClassHookMethod");
        }

        sourceBuilder.Append("{");
        sourceBuilder.Append("MethodInfo = ");
        SourceInformationWriter.GenerateMethodInformation(sourceBuilder, model.Context.SemanticModel.Compilation, model.ClassType, model.Method, null, ',');

        sourceBuilder.Append($"Body = (context, cancellationToken) => AsyncConvert.Convert(() => {model.FullyQualifiedTypeName}.{model.MethodName}({GetArgs(model)})),");

        sourceBuilder.Append($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
        sourceBuilder.Append($"Order = {model.Order},");
        sourceBuilder.Append($"RegistrationIndex = global::TUnit.Core.HookRegistrationIndices.GetNext{(model.HookLocationType == HookLocationType.Before ? "Before" : "After")}{(model.IsEveryHook ? "Every" : "")}ClassHookIndex(),");
        sourceBuilder.Append($"""FilePath = @"{model.FilePath}",""");
        sourceBuilder.Append($"LineNumber = {model.LineNumber},");

        sourceBuilder.Append("},");
    }

    private static string GetArgs(HooksDataModel model)
    {
        List<string> args = [];

        foreach (var type in model.ParameterTypes)
        {
            if (type == WellKnownFullyQualifiedClassNames.ClassHookContext.WithGlobalPrefix)
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
