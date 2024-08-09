namespace TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;

internal static class HookExecutorHelper
{
    public static string GetHookExecutor(string? hookExecutor)
    {
        if (string.IsNullOrEmpty(hookExecutor))
        {
            return "DefaultExecutor.Instance";
        }

        return $"new {hookExecutor}()";
    }
}