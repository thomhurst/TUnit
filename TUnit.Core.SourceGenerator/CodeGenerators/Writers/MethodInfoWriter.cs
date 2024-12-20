namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class MethodInfoWriter
{
    public static string Write(string type, string methodName, string[] parameters)
    {
        string[] actionTypes =
        [
            type,
            ..parameters
        ];

        var actionLambdaParameters = actionTypes.Select((x, i) => $"{x} a{i}");
        
        return
            $"((Action<{string.Join(", ", actionTypes)}>)(({string.Join(", ", actionLambdaParameters)}) => a0.{methodName}({string.Join(", ", parameters.Select((x, i) => $"a{i+1}"))}))).Method";
    }
}