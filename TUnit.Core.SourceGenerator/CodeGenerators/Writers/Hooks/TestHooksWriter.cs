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
                sourceBuilder.WriteLine("new global::TUnit.Core.Hooks.BeforeTestHookMethod");
            }
            else
            {
                sourceBuilder.WriteLine("new global::TUnit.Core.Hooks.AfterTestHookMethod");
            }
            
            sourceBuilder.WriteLine("{ ");
            sourceBuilder.WriteTabs();
            sourceBuilder.Write("MethodInfo = ");
            SourceInformationWriter.GenerateMethodInformation(sourceBuilder, model.Context, model.ClassType, model.Method, null, ',');
            
            if (model.IsVoid)
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
            
            sourceBuilder.WriteTabs();
            sourceBuilder.Write("MethodAttributes = ");
            AttributeWriter.WriteAttributes(sourceBuilder, model.Context, model.Method.GetAttributes().ExcludingSystemAttributes());
            
            sourceBuilder.WriteTabs();
            sourceBuilder.Write("ClassAttributes = ");
            AttributeWriter.WriteAttributes(sourceBuilder, model.Context,
                    model.Method.ContainingType.GetAttributesIncludingBaseTypes().ExcludingSystemAttributes());
            
            sourceBuilder.WriteTabs();
            sourceBuilder.Write("AssemblyAttributes = ");
            AttributeWriter.WriteAttributes(sourceBuilder, model.Context, model.Method.ContainingAssembly.GetAttributes().ExcludingSystemAttributes());
            
            sourceBuilder.WriteLine("},");

            return;
        }

        sourceBuilder.WriteLine("new global::TUnit.Core.Hooks.InstanceHookMethod");
        sourceBuilder.WriteLine("{");
        sourceBuilder.WriteLine($"ClassType = typeof({model.FullyQualifiedTypeName}),");
        sourceBuilder.WriteTabs();
        sourceBuilder.Write("MethodInfo = ");
        SourceInformationWriter.GenerateMethodInformation(sourceBuilder, model.Context, model.ClassType, model.Method, null, ',');
        
        if (model.IsVoid)
        {
            if (model.ClassType.IsGenericDefinition())
            {
                sourceBuilder.WriteLine($"Body = (classInstance, context, cancellationToken) => typeof({model.FullyQualifiedTypeName}).GetMethod(\"{model.MethodName}\", [{string.Join(", ", model.ParameterTypes.Select(x => $"typeof({x})"))}]).Invoke(classInstance, {GetArgsOrEmptyArray(model)}),");
            }
            else
            {
                sourceBuilder.WriteLine($"Body = (classInstance, context, cancellationToken) => ((typeof({model.FullyQualifiedTypeName}))classInstance).{model.MethodName}({GetArgs(model)}),");
            }
        }
        else
        {
            if (model.ClassType.IsGenericDefinition())
            {
                sourceBuilder.WriteLine($"AsyncBody = (classInstance, context, cancellationToken) => await ((global::System.Threading.Tasks.Task) typeof({model.FullyQualifiedTypeName}).GetMethod(\"{model.MethodName}\", [{string.Join(", ", model.ParameterTypes.Select(x => $"typeof({x})"))}]).Invoke(classInstance, {GetArgsOrEmptyArray(model)})),");
            }
            else
            {
                sourceBuilder.WriteLine($"AsyncBody = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => ((typeof({model.FullyQualifiedTypeName}))classInstance).{model.MethodName}({GetArgs(model)})),");
            }
        }

        sourceBuilder.WriteLine($"HookExecutor = {HookExecutorHelper.GetHookExecutor(model.HookExecutor)},");
        sourceBuilder.WriteLine($"Order = {model.Order},");
        
        sourceBuilder.WriteTabs();
        sourceBuilder.Write("MethodAttributes = ");
        AttributeWriter.WriteAttributes(sourceBuilder, model.Context, model.Method.GetAttributes().ExcludingSystemAttributes());
        
        sourceBuilder.WriteTabs();
        sourceBuilder.Write("ClassAttributes = ");
        AttributeWriter.WriteAttributes(sourceBuilder, model.Context, model.Method.ContainingType.GetAttributesIncludingBaseTypes().ExcludingSystemAttributes());
        
        sourceBuilder.WriteTabs();
        sourceBuilder.WriteLine("AssemblyAttributes = ");
        AttributeWriter.WriteAttributes(sourceBuilder, model.Context, model.Method.ContainingAssembly.GetAttributes().ExcludingSystemAttributes());
        
        sourceBuilder.WriteLine("},");
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