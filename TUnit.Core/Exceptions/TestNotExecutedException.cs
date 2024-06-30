namespace TUnit.Core.Exceptions;

public class TestNotExecutedException : TUnitException
{
    internal TestNotExecutedException(TestInformation testInformation) : base(GetMessage(testInformation))
    {
    }

    private static string GetMessage(TestInformation testInformation)
    {
        return $"The test {testInformation.TestName} was not run.";
    }
}