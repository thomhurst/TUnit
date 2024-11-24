﻿using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Enums;
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
        
        if(model.HookLocationType == HookLocationType.Before)
        {
            sourceBuilder.WriteLine("new BeforeAssemblyHookMethod");
        }
        else
        {
            sourceBuilder.WriteLine("new AfterAssemblyHookMethod");
        }

        sourceBuilder.WriteLine("{ ");
        sourceBuilder.WriteLine($"""MethodInfo = typeof({model.FullyQualifiedTypeName}).GetMethod("{model.MethodName}", 0, [{string.Join(", ", model.ParameterTypes.Select(x => $"typeof({x})"))}]),""");
        
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