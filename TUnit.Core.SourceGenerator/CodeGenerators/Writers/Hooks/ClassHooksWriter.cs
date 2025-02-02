using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Enums;
using TUnit.Core.SourceGenerator.Extensions;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public static class ClassHooksWriter
{
    public static void Execute(SourceCodeWriter sourceBuilder, HooksDataModel model)
    { 
        if (model.HookLocationType == HookLocationType.Before)
        {
            sourceBuilder.WriteLine("new BeforeClassHookMethod");
        }
        else
        {
            sourceBuilder.WriteLine("new AfterClassHookMethod");
        }

        sourceBuilder.WriteLine("{ ");
        sourceBuilder.WriteLine($"""MethodInfo = {SourceInformationWriter.GenerateMethodInformation(model.Context, model.Method)},""");

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
        sourceBuilder.WriteLine(
            $"MethodAttributes = [ {AttributeWriter.WriteAttributes(model.Context, model.Method.GetAttributes().ExcludingSystemAttributes()).ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine(
            $"ClassAttributes = [ {AttributeWriter.WriteAttributes(model.Context, model.Method.ContainingType.GetAttributesIncludingBaseTypes().ExcludingSystemAttributes()).ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine(
            $"AssemblyAttributes = [ {AttributeWriter.WriteAttributes(model.Context, model.Method.ContainingAssembly.GetAttributes().ExcludingSystemAttributes()).ToCommaSeparatedString()} ],");
        sourceBuilder.WriteLine("},");
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