using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators.Writers;

internal static class DisplayNameWriter
{
    public static string GetDisplayName(TestSourceDataModel testSourceDataModel)
    {
        var customDisplayName = GetCustomDisplayName(testSourceDataModel);

        if (!string.IsNullOrEmpty(customDisplayName))
        {
            return customDisplayName!;
        }

        return $"{testSourceDataModel.MethodName}{GetMethodArgs(testSourceDataModel)}";
    }

    private static string? GetCustomDisplayName(TestSourceDataModel testSourceDataModel)
    {
        var displayName = testSourceDataModel.CustomDisplayName;

        if (string.IsNullOrEmpty(displayName))
        {
            return null;
        }
        
        var args = testSourceDataModel.MethodArguments.DataVariables.Select(x => x.Name);

        for (var index = 0; index < testSourceDataModel.MethodParameterNames.Length; index++)
        {
            var methodParameterName = testSourceDataModel.MethodParameterNames.ElementAtOrDefault(index);
            displayName = displayName!.Replace($"${methodParameterName}", $"{{{args.ElementAtOrDefault(index)}}}");
        }

        return displayName;
    }

    private static string GetMethodArgs(TestSourceDataModel testSourceDataModel)
    {
        var variableNames = testSourceDataModel.MethodArguments.DataVariables.Select(x => x.Name).ToArray();
        
        if (!variableNames.Any())
        {
            return string.Empty;
        }

        var args = variableNames.Select(x => $"{{{x}}}");
        
        return $"({string.Join(", ", args)})";
    }
}