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
