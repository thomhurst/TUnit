namespace TUnit.Core.Exceptions;

public class TimeoutException : TUnitException
{
    internal TimeoutException(TestInformation testInformation) : base(GetMessage(testInformation))
    {
    }

    private static string GetMessage(TestInformation testInformation)
    {
        return $"The test timed out after {testInformation.Timeout!.Value.TotalMilliseconds} milliseconds";
    }
}