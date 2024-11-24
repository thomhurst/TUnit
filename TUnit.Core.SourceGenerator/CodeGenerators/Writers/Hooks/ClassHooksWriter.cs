using TUnit.Core.SourceGenerator.Helpers;

namespace TUnit.Core.SourceGenerator.Writers.Hooks;

public static class ClassHooksWriter
{
    public static void Execute(SourceCodeWriter sourceBuilder, HooksDataModel model)
    { 
        if(model.HookLocationType == HookLocationType.Before)
        {
            sourceBuilder.WriteLine("new BeforeClassHookMethod");
        }
        else
        {
            sourceBuilder.WriteLine("new AfterClassHookMethod");
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