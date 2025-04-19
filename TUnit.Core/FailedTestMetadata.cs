using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record FailedTestMetadata<[DynamicallyAccessedMembers(
    DynamicallyAccessedMemberTypes.PublicConstructors
    | DynamicallyAccessedMemberTypes.PublicMethods
    | DynamicallyAccessedMemberTypes.NonPublicMethods)] TClassType>
    where TClassType : class
{
    public required string TestId { get; init; }
    public required string MethodName { get; init; }
    public required Exception Exception { get; init; }
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }


    public static implicit operator TestMetadata<TClassType>(FailedTestMetadata<TClassType> failedTestMetadata)
    {
        return new TestMetadata<TClassType>
        {
            TestId = failedTestMetadata.TestId,
            RepeatLimit = 0,
            TestMethod = SourceGeneratedMethodInformation.Failure<TClassType>(failedTestMetadata.MethodName),
            CurrentRepeatAttempt = 0,
            ResettableClassFactory = new ResettableLazy<TClassType>(() => null!, "Unknown", new TestBuilderContext()),
            TestMethodFactory = (_, _) => default,
            TestBuilderContext = new TestBuilderContext(),
            TestClassArguments = [],
            TestClassProperties = [],
            TestFilePath = failedTestMetadata.TestFilePath,
            TestLineNumber = failedTestMetadata.TestLineNumber,
            TestMethodArguments = [],
            DiscoveryException = failedTestMetadata.Exception
        };
    }

}