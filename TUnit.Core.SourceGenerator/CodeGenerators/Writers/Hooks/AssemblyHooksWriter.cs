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
            sourceBuilder.WriteLine("new global::TUnit.Core.Hooks.BeforeAssemblyHookMethod");
        }
        else
        {
            sourceBuilder.WriteLine("new global::TUnit.Core.Hooks.AfterAssemblyHookMethod");
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
        AttributeWriter.WriteAttributes(sourceBuilder, model.Context, model.Method.ContainingType.GetAttributesIncludingBaseTypes().ExcludingSystemAttributes());
        
        sourceBuilder.WriteTabs();
        sourceBuilder.Write("AssemblyAttributes = ");
        AttributeWriter.WriteAttributes(sourceBuilder, model.Context, model.Method.ContainingAssembly.GetAttributes().ExcludingSystemAttributes());
        
        sourceBuilder.WriteLine();
        sourceBuilder.WriteLine("},");
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