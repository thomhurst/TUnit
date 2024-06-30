namespace TUnit.Core.Exceptions;

public class DependsOnNotInParallelException : TUnitException
{
    internal DependsOnNotInParallelException(string testName) : base($"{testName} cannot have both DependsOn and NotInParallel attributes")
    {
    }
}