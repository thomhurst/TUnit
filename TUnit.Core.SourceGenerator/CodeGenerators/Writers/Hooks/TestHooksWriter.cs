using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public class TestHooksWriter : BaseHookWriter
{
    public static void Execute(SourceCodeWriter sourceBuilder, HooksDataModel model)
    {
        if (model.IsEveryHook)
        {
            if(model.HookLocationType == HookLocationType.Before)
            {
                sourceBuilder.WriteLine("new BeforeTestHookMethod");
            }
            else
            {
                sourceBuilder.WriteLine("new AfterTestHookMethod");
            }
            
            sourceBuilder.WriteLine("{ ");
            sourceBuilder.WriteLine($"""MethodInfo = {MethodInfoWriter.Write(model.FullyQualifiedTypeName, model.MethodName, model.ParameterTypes, false)},""");
            
            if(model.IsVoid)
            {
                sourceBuilder.WriteLine($"Body = (context, cancellationToken) => {model.FullyQualifiedTypeName}.{model.MethodName}({GetArgs(model)}),");
            }
            else
            {
                sourceBuilder.WriteLine($"AsyncBody = (context, cancellationToken) => AsyncConvert.Convert(() => {model.FullyQualifiedTypeName}.{model.MethodName}({GetArgs(model)})),");
            }
            
            sourceBuilder.WriteLine($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
            sourceBuilder.WriteLine($"Order = {model.Order},");
            sourceBuilder.WriteLine($"""FilePath = @"{model.FilePath}",""");
            sourceBuilder.WriteLine($"LineNumber = {model.LineNumber},");
            sourceBuilder.WriteLine("},");

            return;
        }

        sourceBuilder.WriteLine($"new InstanceHookMethod<{model.FullyQualifiedTypeName}>");
        sourceBuilder.WriteLine("{");
        sourceBuilder.WriteLine($"""MethodInfo = {MethodInfoWriter.Write(model.FullyQualifiedTypeName, model.MethodName, model.ParameterTypes, false)},""");
        
        if(model.IsVoid)
        {
            sourceBuilder.WriteLine($"Body = (classInstance, context, cancellationToken) => classInstance.{model.MethodName}({GetArgs(model)}),");
        }
        else
        {
            sourceBuilder.WriteLine($"AsyncBody = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.{model.MethodName}({GetArgs(model)})),");
        }

        sourceBuilder.WriteLine($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
        sourceBuilder.WriteLine($"Order = {model.Order},");
        sourceBuilder.WriteLine("},");
    }
}