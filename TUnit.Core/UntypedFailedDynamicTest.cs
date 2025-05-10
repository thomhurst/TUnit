using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

[RequiresUnreferencedCode("Reflection")]
public record UntypedFailedDynamicTest
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
        | DynamicallyAccessedMemberTypes.PublicMethods 
        | DynamicallyAccessedMemberTypes.NonPublicMethods
        | DynamicallyAccessedMemberTypes.PublicProperties)]
    public required Type TestClassType { get; init; }
    public required string MethodName { get; init; }
    public required Exception Exception { get; init; }
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }

    public static implicit operator DynamicTest(UntypedFailedDynamicTest failedTestMetadata)
    {
        return new UntypedDynamicTest(failedTestMetadata.TestClassType.GetMethod(failedTestMetadata.MethodName)!)
        {
            TestClassArguments = [],
            Properties = [],
            TestFilePath = failedTestMetadata.TestFilePath,
            TestLineNumber = failedTestMetadata.TestLineNumber,
            TestMethodArguments = [],
            Exception = failedTestMetadata.Exception
        };
    }
}