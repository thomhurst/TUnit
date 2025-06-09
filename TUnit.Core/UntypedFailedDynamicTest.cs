using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace TUnit.Core;

[RequiresDynamicCode("Reflection")]
[RequiresUnreferencedCode("Reflection")]
public record UntypedFailedDynamicTest(MethodInfo TestMethod)
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
        return new UntypedDynamicTest(failedTestMetadata.TestMethod)
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
