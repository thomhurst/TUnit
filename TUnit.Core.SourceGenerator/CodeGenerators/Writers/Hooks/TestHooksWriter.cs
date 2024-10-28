using TUnit.Core.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Core.SourceGenerator.Models;

namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers.Hooks;

public class TestHooksWriter : BaseHookWriter
{
    public static void Execute(SourceCodeWriter sourceBuilder, HooksDataModel model)
    {
        if (model.IsEveryHook)
        {
            sourceBuilder.WriteLine(
                $$"""
                  new StaticHookMethod<{{WellKnownFullyQualifiedClassNames.TestContext.WithGlobalPrefix}}>
                          { 
                             MethodInfo = typeof({{model.FullyQualifiedTypeName}}).GetMethod("{{model.MethodName}}", 0, [{{string.Join(", ", model.ParameterTypes.Select(x => $"typeof({x})"))}}]),
                             Body = (context, cancellationToken) => AsyncConvert.Convert(() => {{model.FullyQualifiedTypeName}}.{{model.MethodName}}({{GetArgs(model)}})),
                             HookExecutor = {{HookExecutorHelper.GetHookExecutor(model.HookExecutor)}},
                             Order = {{model.Order}},
                             FilePath = @"{{model.FilePath}}",
                             LineNumber = {{model.LineNumber}},
                          },
                  """);
            
            return;
        }
        
        sourceBuilder.WriteLine(
                $$"""
                  new InstanceHookMethod<{{model.FullyQualifiedTypeName}}>
                          {
                               MethodInfo = typeof({{model.FullyQualifiedTypeName}}).GetMethod("{{model.MethodName}}", 0, [{{string.Join(", ", model.ParameterTypes.Select(x => $"typeof({x})"))}}]),
                               Body = (classInstance, context, cancellationToken) => AsyncConvert.Convert(() => classInstance.{{model.MethodName}}({{GetArgs(model)}})),
                               HookExecutor = {{HookExecutorHelper.GetHookExecutor(model.HookExecutor)}},
                               Order = {{model.Order}},
                          },
                  """);
    }
}