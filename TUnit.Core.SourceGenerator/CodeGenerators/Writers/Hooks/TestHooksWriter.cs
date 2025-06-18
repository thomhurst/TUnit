using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public class TestHooksWriter : BaseHookWriter
{
    public static void Execute(SourceCodeWriter sourceBuilder, HooksDataModel model)
    {
        if (model.IsEveryHook)
        {
            if (model.HookLocationType == HookLocationType.Before)
            {
                sourceBuilder.Write("new global::TUnit.Core.Hooks.BeforeTestHookMethod");
            }
            else
            {
                sourceBuilder.Write("new global::TUnit.Core.Hooks.AfterTestHookMethod");
            }

            sourceBuilder.Write("{");
            sourceBuilder.Write("MethodInfo = ");
            SourceInformationWriter.GenerateMethodInformation(sourceBuilder, model.Context, model.ClassType, model.Method, null, ',');

            sourceBuilder.Write($"Body = (context, cancellationToken) => AsyncConvert.Convert(() => {model.FullyQualifiedTypeName}.{model.MethodName}({GetArgs(model)})),");

            sourceBuilder.Write($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
            sourceBuilder.Write($"Order = {model.Order},");
            sourceBuilder.Write($"""FilePath = @"{model.FilePath}",""");
            sourceBuilder.Write($"LineNumber = {model.LineNumber},");

            sourceBuilder.Write("},");

            return;
        }

        sourceBuilder.Write("new global::TUnit.Core.Hooks.InstanceHookMethod");
        sourceBuilder.Write("{");
        sourceBuilder.Write($"ClassType = typeof({model.FullyQualifiedTypeName}),");
        sourceBuilder.Write("MethodInfo = ");
        SourceInformationWriter.GenerateMethodInformation(sourceBuilder, model.Context, model.ClassType, model.Method, null, ',');


        if (model.ClassType.IsGenericDefinition())
        {
            sourceBuilder.Write(
                $"Body = (classInstance, context, cancellationToken) => AsyncConvert.ConvertObject(() => classInstance.GetType().GetMethod(\"{model.MethodName}\", [{string.Join(", ", model.ParameterTypes.Select(x => $"typeof({x})"))}]).Invoke(classInstance, {GetArgsOrEmptyArray(model)})),");
        }
        else
        {
            sourceBuilder.Write($"Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => (({model.FullyQualifiedTypeName})classInstance).{model.MethodName}({GetArgs(model)})),");
        }

        sourceBuilder.Write($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
        sourceBuilder.Write($"Order = {model.Order},");

        sourceBuilder.Write("},");
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
