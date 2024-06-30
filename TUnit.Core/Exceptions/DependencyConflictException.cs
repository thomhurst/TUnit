namespace TUnit.Core.Exceptions;

public class DependencyConflictException : TUnitException
{
    internal DependencyConflictException(TestInformation test1, TestInformation test2) : base(GetMessage(test1, test2))
    {
    }

    private static string GetMessage(TestInformation test1, TestInformation test2)
    {
        return $"{test1.TestName} and {test2.TestName} cannot depend on each other.";
    }
}