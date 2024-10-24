using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

internal static class TestHooksWriter
{
    public static void Execute(SourceCodeWriter sourceBuilder, HooksDataModel model)
    {
        sourceBuilder.WriteLine(
                $$"""
                  new InstanceHookMethod<{{model.FullyQualifiedTypeName}}>
                          {
                               MethodInfo = typeof({{model.FullyQualifiedTypeName}}).GetMethod("{{model.MethodName}}", 0, [{{string.Join(", ", model.ParameterTypes.Select(x => $"typeof({x})"))}}]),
                               Body = (classInstance, testContext, cancellationToken) => AsyncConvert.Convert(() => classInstance.{{model.MethodName}}({{GenerateContextObject(model)}})),
                               HookExecutor = {{HookExecutorHelper.GetHookExecutor(model.HookExecutor)}},
                               Order = {{model.Order}},
                          },
                  """);
    }

    private static string GenerateContextObject(HooksDataModel model)
    {
        List<string> args = [];
        
        foreach (var type in model.ParameterTypes)
        {
            if (type == WellKnownFullyQualifiedClassNames.TestContext.WithGlobalPrefix)
            {
                args.Add("testContext");
            }
            
            if (type == WellKnownFullyQualifiedClassNames.CancellationToken.WithGlobalPrefix)
            {
                args.Add("cancellationToken");
            }
        }

        return string.Join(", ", args);
    }
}