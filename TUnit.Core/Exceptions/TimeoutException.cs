using System.Runtime.Serialization;

namespace TUnit.Core.Exceptions;

public class TimeoutException : TUnitException
{
    public TimeoutException(TestDetails testDetails) : base(GetMessage(testDetails))
    {
        
    }

    private static string GetMessage(TestDetails testDetails)
    {
        return $"The test timed out after {testDetails.Timeout.Milliseconds} milliseconds";
    }
}