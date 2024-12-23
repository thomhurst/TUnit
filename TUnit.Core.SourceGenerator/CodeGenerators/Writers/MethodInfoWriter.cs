namespace TUnit.Core.SourceGenerator.CodeGenerators.Writers;

public static class MethodInfoWriter
{
    public static string Write(string type, string methodName, string[] parameters, bool isStatic)
    {
        return isStatic ? WriteStatic(type, methodName, parameters) : WriteInstance(type, methodName, parameters);
    }

    private static string WriteInstance(string type, string methodName, string[] parameters)
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
    
    private static string WriteStatic(string type, string methodName, string[] parameters)
    {
        var actionType = parameters.Length == 0
            ? "Action"
            : $"Action<{string.Join(", ", parameters)}>";

        var actionLambdaParameters = parameters.Select((x, i) => $"{x} a{i}");
        
        return
            $"(({actionType})(({string.Join(", ", actionLambdaParameters)}) => {type}.{methodName}({string.Join(", ", parameters.Select((x, i) => $"a{i}"))}))).Method";
    }
}