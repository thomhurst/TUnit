namespace TUnit.Core.Exceptions;

public class TestNotExecutedException : TUnitException
{
    internal TestNotExecutedException(TestDetails testDetails) : base(GetMessage(testDetails))
    {
    }

    private static string GetMessage(TestDetails testDetails)
    {
        return $"The test {testDetails.TestName} was not run.";
    }
}
