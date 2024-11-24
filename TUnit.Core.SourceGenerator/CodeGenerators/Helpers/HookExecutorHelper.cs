namespace TUnit.Core.SourceGenerator.Helpers;

public static class HookExecutorHelper
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