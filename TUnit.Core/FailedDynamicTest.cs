using System.Diagnostics.CodeAnalysis;

namespace TUnit.Core;

public record FailedDynamicTest<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors 
                                | DynamicallyAccessedMemberTypes.PublicMethods 
                                | DynamicallyAccessedMemberTypes.PublicProperties)]
    TClassType>
    where TClassType : class
{
    public required string TestId { get; init; }
    public required string MethodName { get; init; }
    public required Exception Exception { get; init; }
    public required string TestFilePath { get; init; }
    public required int TestLineNumber { get; init; }
    
    public static implicit operator DynamicTest<
        TClassType>(FailedDynamicTest<TClassType> failedTestMetadata)
    {
        return new DynamicTest<TClassType>
        {
            TestMethod = @class => @class.GetType(),
            TestClassArguments = [],
            Properties = [],
            TestFilePath = failedTestMetadata.TestFilePath,
            TestLineNumber = failedTestMetadata.TestLineNumber,
            TestMethodArguments = [],
            Exception = failedTestMetadata.Exception
        };
    }

}