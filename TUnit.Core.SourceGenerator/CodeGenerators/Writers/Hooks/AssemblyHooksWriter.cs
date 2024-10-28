using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
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

        sourceBuilder.WriteLine(
            $$$"""
               new StaticHookMethod<global::TUnit.Core.AssemblyHookContext>
                       { 
                          MethodInfo = typeof({{{model.FullyQualifiedTypeName}}}).GetMethod("{{{model.MethodName}}}", 0, [{{{string.Join(", ", model.ParameterTypes.Select(x => $"typeof({x})"))}}}]),
                          Body = (context, cancellationToken) => AsyncConvert.Convert(() => {{{model.FullyQualifiedTypeName}}}.{{{model.MethodName}}}({{{GetArgs(model)}}})),
                          HookExecutor = {{{HookExecutorHelper.GetHookExecutor(model.HookExecutor)}}},
                          Order = {{{model.Order}}},
                          FilePath = @"{{{model.FilePath}}}",
                          LineNumber = {{{model.LineNumber}}},
                       },
               """);
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