using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public class TestHooksWriter : BaseHookWriter
{
    public static void Execute(ICodeWriter sourceBuilder, HooksDataModel model)
    {
        if (model.IsEveryHook)
        {
            if (model.HookLocationType == HookLocationType.Before)
            {
                sourceBuilder.Append("new global::TUnit.Core.Hooks.BeforeTestHookMethod");
            }
            else
            {
                sourceBuilder.Append("new global::TUnit.Core.Hooks.AfterTestHookMethod");
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

            return;
        }

        sourceBuilder.Append("new global::TUnit.Core.Hooks.InstanceHookMethod");
        sourceBuilder.Append("{");
        sourceBuilder.Append($"ClassType = typeof({model.FullyQualifiedTypeName}),");
        sourceBuilder.Append("MethodInfo = ");
        SourceInformationWriter.GenerateMethodInformation(sourceBuilder, model.Context.SemanticModel.Compilation, model.ClassType, model.Method, null, ',');


        if (model.ClassType.IsGenericDefinition())
        {
            sourceBuilder.Append(
                $"Body = (classInstance, context, cancellationToken) => AsyncConvert.ConvertObject(() => classInstance.GetType().GetMethod(\"{model.MethodName}\", [{string.Join(", ", model.ParameterTypes.Select(x => $"typeof({x})"))}]).Invoke(classInstance, {GetArgsOrEmptyArray(model)})),");
        }
        else
        {
            sourceBuilder.Append($"Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => (({model.FullyQualifiedTypeName})classInstance).{model.MethodName}({GetArgs(model)})),");
        }

        sourceBuilder.Append($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
        sourceBuilder.Append($"Order = {model.Order},");

        sourceBuilder.Append("},");
    }

    private static string GetArgsOrEmptyArray(HooksDataModel model)
    {
        if (!model.ParameterTypes.Any())
        {
            return "[]";
        }

        return GetArgs(model);
    }
}
