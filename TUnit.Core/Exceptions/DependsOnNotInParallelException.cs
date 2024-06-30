namespace TUnit.Core.Exceptions;

public class DependsOnNotInParallelException : TUnitException
{
    internal DependsOnNotInParallelException(string testName) : base($"{testName} cannot use DependsOn with a test that has a NotInParallel attribute")
    {
    }
}