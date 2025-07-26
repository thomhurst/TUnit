using TUnit.Core.Enums;

namespace TUnit.Core;

internal static class DataGeneratorMetadataCreator
{
    public static DataGeneratorMetadata CreateDataGeneratorMetadata(
        TestMetadata testMetadata,
        string testSessionId,
        DataGeneratorType generatorType,
        object? testClassInstance,
        object?[]? classInstanceArguments,
        TestBuilderContextAccessor contextAccessor)
    {
        // Determine which parameters we're generating for
        var parametersToGenerate = generatorType == DataGeneratorType.ClassParameters
            ? testMetadata.MethodMetadata.Class.Parameters
            : testMetadata.MethodMetadata.Parameters;

        // Filter out CancellationToken if it's the last parameter (handled by the engine)
        if (generatorType == DataGeneratorType.TestParameters && parametersToGenerate.Length > 0)
        {
            var lastParam = parametersToGenerate[^1];
            if (lastParam.Type == typeof(System.Threading.CancellationToken))
            {
                parametersToGenerate = parametersToGenerate[..^1];
            }
        }

        return new DataGeneratorMetadata
        {
            TestBuilderContext = contextAccessor,
            MembersToGenerate = [..parametersToGenerate],
            TestInformation = testMetadata.MethodMetadata,
            Type = generatorType,
            TestSessionId = testSessionId,
            TestClassInstance = testClassInstance,
            ClassInstanceArguments = classInstanceArguments
        };
    }
}
